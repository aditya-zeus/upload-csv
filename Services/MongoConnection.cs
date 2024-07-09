using MongoDB.Driver;
using MongoDB.Bson;
using Task1.Models;

namespace Task1.Services
{
    public class MongoConnection
    {
        private readonly string connectionString;
        private readonly string databaseName;
        private readonly string collectionName;

        private readonly MongoClient client;
        private readonly IMongoDatabase database;
        private readonly IMongoCollection<StateObject> collection;

        public MongoConnection(string _connectionString = "mongodb://localhost:27017/", string _databaseName = "StateObject", string _collectionName = "State") {
            connectionString = _connectionString;
            databaseName = _databaseName;
            collectionName = _collectionName;

            client = new MongoClient(connectionString);
            database = client.GetDatabase(databaseName);
            collection = database.GetCollection<StateObject>(collectionName);
        }

        public async Task<string> GetOne(string id) {
            var filter = Builders<StateObject>.Filter.Eq("_id", ObjectId.Parse(id));
            var result = await collection.Find(filter).FirstOrDefaultAsync();

            if (result != null)
            {
                return result.State!;
            }
            else
            {
                Console.WriteLine($"Document with id {id} not found.");
                return null!;
            }
        }

        public async Task<string> InsertOne(StateObject newObject) {
            await collection.InsertOneAsync(newObject);
            return newObject.Id.ToString();
        }

        public async Task InsertMany(StateObject[] newObject) {
            await collection.InsertManyAsync(newObject);
        }

        public async Task<bool> UpdateOneState(string id, string newState) {
            var filter = Builders<StateObject>.Filter.Eq("_id", ObjectId.Parse(id));

            // Define the update definition
            var update = Builders<StateObject>.Update
                .Set("State", newState);

            var result = await collection.UpdateOneAsync(filter, update);

            if(result.IsAcknowledged) {
                if(result.ModifiedCount == 1) {
                    return true;
                } else if(result.ModifiedCount > 1) {
                    Console.WriteLine("Multiple records updated");
                    return false;
                }
            }
            return false;
        }

        public async Task<bool> DeleteOne(string id) {
            var filter = Builders<StateObject>.Filter.Eq("_id", ObjectId.Parse(id));
            var result = await collection.DeleteOneAsync(filter);

            var deleted = result.DeletedCount;

            if(deleted == 1) {
                return true;
            } else if (deleted > 1) {
                Console.WriteLine($"Multiple records deleted: {deleted}");
                return false;
            }
            return false; 
        }
    }
}