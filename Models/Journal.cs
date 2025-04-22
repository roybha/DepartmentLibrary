using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DepartmentLibrary.Models;

public class Journal
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("title")]
    public string Title { get; set; }

    [BsonElement("number")]
    public int Number { get; set; }

    [BsonElement("volume")]
    public int Volume { get; set; }

    [BsonElement("pages")]
    public int Pages { get; set; }

    [BsonElement("reference")]
    public string Reference { get; set; }

    [BsonElement("edition")]
    public string Edition { get; set; }
}