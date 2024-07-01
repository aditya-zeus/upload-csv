using System.Text;
using MySqlConnector;

namespace Task1.Services
{
    public interface IFileOperation
    {
        public int BATCH_SIZE { get; set; }
        public Task<(string, string)> SaveFile(IFormFile file);
        public List<string> GetSqlStatement(string filePath);
        public void RemoveFile(string filePath)
        {
            File.Delete(filePath);
        }

        protected static string CreateSqlStatement(int fieldLength, int iFrom, int iTo)
        {
            StringBuilder SqlCommand = new StringBuilder(@"INSERT IGNORE INTO Details(Email, Name, Country, State, City, Telephone, AddressLine1, AddressLine2, DoB, GrossSalaryFY2019_20, GrossSalaryFY2020_21, GrossSalaryFY2021_22, GrossSalaryFY2022_23, GrossSalaryFY2023_24) VALUES");

            List<string> rowParam = new List<string>();
            for (int i = iFrom; i < iTo; i++)
            {
                List<string> col = new List<string>();
                for (int j = 0; j < fieldLength; j++)
                {
                    col.Add($"@r{i}c{j}");
                }
                string singleRow = "(" + string.Join(",", col) + ")";
                rowParam.Add(singleRow);
            }
            SqlCommand.Append(string.Join(",", rowParam));

            return SqlCommand.ToString();
        }

        protected static async void UpdateParamsOfCommandUsersAndSaveToDb(string connectionString, string sCommand, List<string[]> rows, int iFrom, int iTo, int cols)
        {
            // TODO: Handle Deadlock for sql connections
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            // MySqlCommand command = new MySqlCommand(sCommand, connection);
            MySqlCommand command = connection.CreateCommand();
            MySqlTransaction transaction;
            // Start a local transaction
            transaction = connection.BeginTransaction();
            try {
                command.Connection = connection;
                command.Transaction = transaction;
                command.CommandText = sCommand;
                for (int i = iFrom; i < iTo; ++i)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        command.Parameters.AddWithValue($"@r{i}c{j}", rows[i][j]);
                    }
                }
                await command.PrepareAsync();
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            } catch(Exception e) {
                Console.WriteLine($"[-] Unable to commit: --- {e}");
                await transaction.RollbackAsync();
            } finally {
                connection.Close();
            }
        }

        protected static void ProcessInBatches(string connectionString, List<string[]> rows, int iFrom, int iTo, int cols)
        {
            string sCommand = CreateSqlStatement(cols, iFrom, iTo);
            UpdateParamsOfCommandUsersAndSaveToDb(connectionString, sCommand, rows, iFrom, iTo, cols);
        }

        protected static async Task<int> DeleteSingleUser(string connectionString, string Email)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string sCommand = $"DELETE FROM Details WHERE Email = @Email";
            MySqlCommand command = new MySqlCommand(sCommand, connection);
            command.Parameters.AddWithValue("@Email", Email);
            int res = await command.ExecuteNonQueryAsync();

            await connection.CloseAsync();
            return res;
        }

        protected static async Task<List<Dictionary<string, object>>> GetData(string connectionString, bool multipleRows, string? Email = null, int iFrom = 0, int iTo = 0)
        {
            string sCommand;
            if (multipleRows)
            {
                if (iFrom <= 0 && iTo <= 0)
                    sCommand = $"SELECT * FROM Details";
                else if (iTo <= 0)
                    sCommand = $"SELECT * FROM Details WHERE id >= {iFrom}";
                else if (iFrom <= 0)
                    sCommand = $"SELECT * FROM Details WHERE id <= {iTo}";
                else
                    sCommand = $"SELECT * FROM Details WHERE Id >= {iFrom} and Id < {iTo}";
            }
            else
            {
                sCommand = $"SELECT * FROM Details WHERE Email LIKE @Email";
            }

            List<Dictionary<string, object>> res = [];

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sCommand, connection);
            if (!multipleRows) command.Parameters.AddWithValue("@Email", Email);

            // Execute the query asynchronously and get the data reader
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var data = new Dictionary<string, object>();
                // Get the number of columns in the result set
                int numColumns = reader.FieldCount;

                // Iterate through each column and add its name and value to the dictionary
                for (int i = 0; i < numColumns; i++)
                {
                    string columnName = reader.GetName(i);
                    object? columnValue = reader.IsDBNull(i) ? null : reader.GetValue(i);

                    data.Add(columnName, columnValue ?? string.Empty);
                }
                res.Add(data);
            }
            await connection.CloseAsync();
            await reader.CloseAsync();

            return res;
        }

        public static string GetParamaterizedQuery(List<string[]> rows, int iFrom, int iTo, int cols) {
            StringBuilder SqlCommand = new StringBuilder(@"INSERT IGNORE INTO Details(Email, Name, Country, State, City, Telephone, AddressLine1, AddressLine2, DoB, GrossSalaryFY2019_20, GrossSalaryFY2020_21, GrossSalaryFY2021_22, GrossSalaryFY2022_23, GrossSalaryFY2023_24) VALUES");

            for(int i = iFrom; i < iTo; i++)
            {
                // SqlCommand.Append("(" + string.Join(", ", rows[i]) + "), ");
                SqlCommand.Append('(');
                for(int j = 0; j < cols; j++) {
                    SqlCommand.Append("\"" + rows[i][j] + "\", ");
                }
                SqlCommand.Remove(SqlCommand.Length - 2, 2);
                SqlCommand.Append("), ");
            }
            SqlCommand.Remove(SqlCommand.Length - 2, 2);

            return SqlCommand.ToString();
        }

        public static async void SaveToDb(string connectionString, string query) {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            MySqlCommand command = connection.CreateCommand();
            MySqlTransaction transaction  = connection.BeginTransaction();

            try {
                command.Connection = connection;
                command.Transaction = transaction;
                command.CommandText = query;

                await command.PrepareAsync();
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            } catch(Exception e) {
                Console.WriteLine($"[-] Unable to commit: --- {e.Message} | Republishing...");
                await transaction.RollbackAsync();
                var rc = new RabbitConnection("saveToDb");
                rc.BasicPublish(query);
            } finally {
                await connection.CloseAsync();
                await connection.CloseAsync();
            }
        }
    }
}