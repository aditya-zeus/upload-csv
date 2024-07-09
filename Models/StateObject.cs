using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Task1.Models
{
    public class StateObject
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string? State { get; set; }
    }
}