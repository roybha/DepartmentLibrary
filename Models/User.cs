using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DepartmentLibrary.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("email")]
    public string Email { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }
    [BsonElement("phone")]
    public string Phone { get; set; }

    [BsonElement("password")]
    public string Password { get; set; }

    [BsonElement("position")]
    public string Position { get; set; }

    [BsonElement("thesis_defense_date")]
    public DateTime? ThesisDefenseDate { get; set; }

    [BsonElement("role")]
    public string Role { get; set; }
}