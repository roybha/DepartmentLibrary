﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace YourApp.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("email")]
    public string Email { get; set; }

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; }

    [BsonElement("role")]
    public string Role { get; set; }
}