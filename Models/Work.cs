using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DepartmentLibrary.Models;

public class Work
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("title")]
    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; }

    [BsonElement("annotation")]
    [Required(ErrorMessage = "Annotation is required")]
    public string Annotation { get; set; }

    [BsonElement("category")]
    [BsonRepresentation(BsonType.ObjectId)]
    [Required(ErrorMessage = "Category is required")]
    public string CategoryId { get; set; }

    [BsonElement("source_references")]
    [Required(ErrorMessage = "Source_References is required")]
    public string SourceReferences { get; set; }

    [BsonElement("pages_num")]
    [Required(ErrorMessage = "Pages_Num is required")]
    public int PagesNum { get; set; }

    [BsonElement("authors")]
    [BsonRepresentation(BsonType.ObjectId)]
    [Required(ErrorMessage = "Authors is required")]
    public List<string> AuthorIds { get; set; } = new();

    [BsonElement("digital_reference")]
    [Required(ErrorMessage = "Digital_Reference is required")]
    public string DigitalReference { get; set; }

    [BsonElement("publish_date")]
    [BsonRepresentation(BsonType.DateTime)]
    [Required(ErrorMessage = "Publish_Date is required")]
    public DateTime PublishDate { get; set; }

    [BsonElement("journal")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? JournalId { get; set; }

    [BsonElement("city")]
    public string City { get; set; }
}