using Task1.Services;
using Microsoft.AspNetCore.Mvc;
using Task1.Models;

namespace Task1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadFileController : ControllerBase
    {
        private readonly FileOperationStorage fos = new();
        private readonly MongoConnection mongo = new();

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var res = await fos.GetData(true, null, 1, -1);
            return Ok(res);
        }

        [HttpGet("range")]
        public async Task<IActionResult> Get(int iFrom, int iTo)
        {
            var res = await fos.GetData(true, null, iFrom, iTo);
            return Ok(res);
        }

        [HttpGet("/db/id")]
        public async Task<IActionResult> GetDataAndDelete(string id)
        {
            var res = await mongo.GetOne(id);

            if(res == "Executing 1000/1000") {
                await mongo.DeleteOne(id);
            }
            return Ok(res);
        }

        [HttpGet("email")]
        public async Task<IActionResult> GetSingleRow(string email)
        {
            if (email == null)
            {
                return BadRequest("Email required");
            }
            var res = await fos.GetData(false, email);
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(IFormFile file)
        {
            // Upload file
            var response = fos.SaveFile(file);
            if (response.Result.Item1 == "BadRequest")
            {
                return BadRequest(response.Result.Item2);
            }
            string filePath = response.Result.Item2;
            var id = PostHelper(filePath);
            return Ok(id);
        }

        private async Task<string> PostHelper(string filePath)
        {
            StateObject so = new StateObject
            {
                State = "Queued"
            };


            // process == useless queue
            // string filePath = response.Result.Item2;
            RabbitConnection rc;
            await RetryPolicies.GetWaitAndRetryPolicy().ExecuteAsync(async () =>
            {
                mongo.InsertOne(so);
                rc = new RabbitConnection("process");
                rc.BasicPublish($"{filePath}|{so.Id}|");
                rc.Dispose();
                await Task.CompletedTask;
            });

            so.State = $"Preparing Query";

            List<string> sqlStatementToStore = fos.GetSqlStatement(filePath);
            // string sqlStatementToStore = fos.SaveToDB(filepath);

            await RetryPolicies.GetWaitAndRetryPolicy().ExecuteAsync(async () =>
            {
                int len = sqlStatementToStore.Count;
                int current = 1;
                rc = new RabbitConnection("saveToDb");
                foreach (var statement in sqlStatementToStore)
                {
                    rc.BasicPublish($"{statement}|{so.Id}|{current}/{len}");
                    mongo.UpdateOneState(so.Id.ToString(), $"Preparing {current++}/{len}");
                }
                rc.Dispose();
                // so.State = $"Preparing {current-1}/{len}";
                await Task.CompletedTask;
                mongo.UpdateOneState(so.Id.ToString(), so.State);
            });

            fos.RemoveFile(filePath);
            return so.Id.ToString();
        }

        [HttpPut]
        public async Task<IActionResult> PutAsync(string email, string name, string country, string state, string city, string telephone, string addressLine1, string addressLine2, string dob, int? grossSalaryFY2019_20, int? grossSalaryFY2020_21, int? grossSalaryFY2021_22, int? grossSalaryFY2022_23, int? grossSalaryFY2023_24)
        {
            // TODO: Some issues while receiving DateOnly

            string[] dates = dob.Split("/");
            if (dates[0].Length != 2 || dates[1].Length != 2 || dates[2].Length != 4)
            {
                return BadRequest("Invalid date format");
            }

            // Convert dd/MM/yyyy to DateTime using DateTimeOffset
            DateTimeOffset dateTimeOffset = new DateTimeOffset(int.Parse(dates[2]), int.Parse(dates[1]), int.Parse(dates[0]), 0, 0, 0, TimeSpan.Zero);

            // Convert the DateTimeOffset object to the desired output format
            var outputDate = dateTimeOffset.ToString("yyyy/MM/dd");


            var statement = $"INSERT IGNORE INTO Details(Email, Name, Country, State, City, Telephone, AddressLine1, AddressLine2, DoB, grossSalaryFY2019_20, grossSalaryFY2020_21, grossSalaryFY2021_22, grossSalaryFY2022_23, grossSalaryFY2023_24) VALUES('{email}', '{name}', '{country}', '{state}', '{city}', '{telephone}', '{addressLine1}', '{addressLine2}', '{outputDate}', {grossSalaryFY2019_20}, {grossSalaryFY2020_21}, {grossSalaryFY2021_22}, {grossSalaryFY2022_23}, {grossSalaryFY2023_24}) ON DUPLICATE KEY UPDATE Name = VALUES(Name), Country = VALUES(Country), State = VALUES(State), City = VALUES(City), Telephone = VALUES(Telephone), AddressLine1 = VALUES(AddressLine1), AddressLine2 = VALUES(AddressLine2), DoB = VALUES(DoB), grossSalaryFY2019_20 = VALUES(grossSalaryFY2019_20), grossSalaryFY2020_21 = VALUES(grossSalaryFY2020_21), grossSalaryFY2021_22 = VALUES(grossSalaryFY2021_22), grossSalaryFY2022_23 = VALUES(grossSalaryFY2022_23), grossSalaryFY2023_24 = VALUES(grossSalaryFY2023_24);";
            await RetryPolicies.GetWaitAndRetryPolicy().ExecuteAsync(async () =>
           {
               RabbitConnection rc = new("saveToDb");
               rc.BasicPublish($"{statement}||");
               rc.Dispose();
               await Task.CompletedTask;
           });
            return Ok("Request received!");
        }

        [HttpPut("json")]
        public IActionResult PutJson(UploadFileModel data)
        {
            // TODO: Implement PUT using JSON
            return Ok($"Data received: {data}");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSingle(string Email)
        {
            int res = await fos.DeleteSingleUser(Email);
            if (res == 1)
            {
                return Ok($"Deleted User with email - {Email}");
            }
            else if (res > 1)
            {
                return BadRequest($"Something is wrong, more than 1 row deleted");
            }
            else
            {
                return NotFound($"User with email {Email} not found");
            }
        }

        // TODO: Implement HTTP PATCH method to update a single row
        // TODO: Take email id along with updated data and update the values accordingly
    }
}