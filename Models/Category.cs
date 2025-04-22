using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DepartmentLibrary.Models;

public class Category
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("title")]
    public string Title { get; set; }
}