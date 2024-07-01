using Microsoft.VisualBasic.FileIO;

namespace Task1.Services
{
    public class FileOperationStorage : IFileOperation
    {
        // CREATE TABLE Details(Id INT PRIMARY KEY AUTO_INCREMENT, Email VARCHAR(255) UNIQUE NOT NULL, Name VARCHAR(50) NOT NULL, Country VARCHAR(15), State VARCHAR(30), City VARCHAR(30), Telephone VARCHAR(10), AddressLine1 VARCHAR(150), AddressLine2 VARCHAR(150), DoB DATE, GrossSalaryFY2019_20 INT, GrossSalaryFY2020_21 INT, GrossSalaryFY2021_22 INT, GrossSalaryFY2022_23 INT, GrossSalaryFY2023_24 INT);
        // temp_dir = 'c/Users/aditya.jha/AppData/Local/Temp/FileUploads'
        private readonly static string connectionString = "Server=localhost;User ID=root;Password=;Database=task1;";
        private readonly static int fieldLength = 14;
        private readonly static string[] fieldType = ["System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.Int32", "System.Int32", "System.Int32", "System.Int32", "System.Int32"];

        public int BATCH_SIZE { get; set; } = 1000;

        public async Task<(string, string)> SaveFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return ("BadRequest", "File required!");
            }

            string contentType = file.ContentType;
            if (!contentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase))
            {
                return ("BadRequest", "Only CSV files are allowed.");
            }

            string tempFilePath = Path.GetTempPath();
            Directory.CreateDirectory(tempFilePath + "FileUploads");

            string filePath = Path.Combine(tempFilePath, "FileUploads", Path.GetRandomFileName());
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return ("Ok", filePath);
        }

        public List<string> GetSqlStatement(string filePath)
        {
            int malformedPackets = 0;
            bool firstFlag = true;
            List<string[]> rows = [];
            List<string[]> malformedRows = [];
             
            using (TextFieldParser csvParser = new(filePath))
            {
                csvParser.TextFieldType = FieldType.Delimited;
                csvParser.CommentTokens = ["#"];
                csvParser.SetDelimiters([","]);
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the row with the column names
                // csvParser.ReadLine();

                while (!csvParser.EndOfData)
                {
                    bool unsupportedDataType = false;
                    string[] fields = new string[fieldLength];
                    try {
                        fields = csvParser.ReadFields() ?? [];
                    } catch(MalformedLineException) {
                        malformedPackets++;
                        continue;
                    }

                    // Handle incomplete or unexpected data
                    if(fields.Length != fieldLength) {
                        malformedPackets++;
                        continue; // Do nothing for now & Skip this iteration
                    }


                    // Skip first row if it is column name
                    if(firstFlag) {
                        firstFlag = false;
                        if(fields[0].Equals("email", StringComparison.CurrentCultureIgnoreCase)) continue;
                    }

                    for(int i = 0; i < fieldLength; i++) {
                        // Console.WriteLine($"Datatype of {fields[i]} -> {fields[i].GetType().FullName} | {fieldType[i]}");
                        if(fields[i].GetType().FullName != fieldType[i]) {
                            if(fieldType[i] == "System.Int32") {
                                if(int.TryParse(fields[i], out int value)) {} else {
                                    // return "Bad Request | Unsupported data type";
                                    unsupportedDataType = true;
                                    break;
                                }
                            } else {
                                // return "BadRequest";
                                unsupportedDataType = true;
                                break;
                            }
                        }
                    }
                    if(unsupportedDataType) {
                        malformedRows.Add(fields);
                        malformedPackets++;
                        continue;
                    }

                    // =======================================
                    // Preprocessing of data can be done here
                    // =======================================
                    
                    // Data is in fields[]
                    rows.Add(fields);
                }
            }

            List<string> res = new List<string>();
            int iFrom = 0,
                iTo = BATCH_SIZE;
        
            while(iFrom < (rows.Count - (rows.Count % BATCH_SIZE))) {
                // IFileOperation.ProcessInBatches(connectionString, rows, iFrom, iTo, fieldLength);
                // var statement = IFileOperation.CreateSqlStatement(fieldLength, iFrom, iTo);
                var statement = IFileOperation.GetParamaterizedQuery(rows, iFrom, iTo, fieldLength);
                res.Add(statement);

                iFrom += BATCH_SIZE;
                iTo += BATCH_SIZE;
            }

            if((rows.Count % BATCH_SIZE) != 0) {
                iTo = rows.Count;

                // IFileOperation.ProcessInBatches(connectionString, rows, iFrom, iTo, fieldLength);
                var statement = IFileOperation.GetParamaterizedQuery(rows, iFrom, iTo, fieldLength);
                res.Add(statement);
            }
            
            // return $"Successfully inserted data | Unable to insert {malformedPackets} malformed packets | Malformed Rows: {string.Join(", ", malformedRows.Select(row => row[0]).ToList())}";
            return res;
        }

        public void RemoveFile(string filePath)
        {
            File.Delete(filePath);
        }

        public async Task<int> DeleteSingleUser(string Email) {
            int res = await IFileOperation.DeleteSingleUser(connectionString, Email);
            return res;
        }

        public async Task<List<Dictionary<string, object>>> GetData(bool multipleRows, string? Email=null, int iFrom=0, int iTo=0) {
            List<Dictionary<string, object>> res = [];
            if(multipleRows) {
                res = await IFileOperation.GetData(connectionString, true, null, iFrom, iTo);
            } else {
                res = await IFileOperation.GetData(connectionString, false, Email);
            }
            return res;
        }

        public void SaveToDb(string query) {
            IFileOperation.SaveToDb(connectionString, query);
        }
    }
}