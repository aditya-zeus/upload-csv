using Microsoft.AspNetCore.Mvc;
using Task1.Services;

namespace Task1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadFile : ControllerBase
    {
        FileOperationStorage fos = new FileOperationStorage();
        
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

        [HttpGet("email")]
        public async Task<IActionResult> GetSingleRow(string email) {
            if(email == null) {
                return BadRequest("Email required");
            }
            var res = await fos.GetData(false, email);
            return Ok(res);
        }

        [HttpPost]
        public IActionResult PostAsync(IFormFile file) {

            // Upload file
            var response = fos.SaveFile(file);
            if(response.Result.Item1 == "BadRequest") {
                return BadRequest(response.Result.Item2);
            }

            string filePath = response.Result.Item2;
            RabbitConnection rc = new RabbitConnection("process");
            rc.BasicPublish(filePath);
            rc.Dispose();

            List<string> sqlStatementToStore = fos.GetSqlStatement(filePath);
            // string sqlStatementToStore = fos.SaveToDB(filepath);
            rc = new RabbitConnection("saveToDb");


            foreach (var statement in sqlStatementToStore)
            {
                rc.BasicPublish(statement);
            }

            fos.RemoveFile(filePath);
            return Ok(filePath);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSingle(string Email) {
            int res = await fos.DeleteSingleUser(Email);
            if(res == 1) {
                return Ok($"Deleted User with email - {Email}");
            } else if(res > 1) {
                return BadRequest($"Something is wrong, more than 1 row deleted");
            } else {
                return NotFound($"User with email {Email} not found");
            }
        }

        // TODO: Implement HTTP PATCH method to update a single row
        // TODO: Take email id along with updated data and update the values accordingly
    }
}