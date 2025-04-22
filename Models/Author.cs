using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DepartmentLibrary.Models;

public class Author
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("phone")]
    public string Phone { get; set; }

    [BsonElement("works")]
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> Works { get; set; } = new(); // Зберігає ObjectId як string

    [BsonElement("position")]
    public string Position { get; set; }

    [BsonElement("thesis_defense_date")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime? ThesisDefenseDate { get; set; }
}