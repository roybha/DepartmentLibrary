using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace DepartmentLibrary.Models
{
    public class AuthorReportData
    {
        [BsonElement("Author Name")]
        public string AuthorName { get; set; }

        [BsonElement("works_before_thesis")]
        public List<WorkInfo> WorksBeforeThesis { get; set; }

        [BsonElement("works_after_thesis")]
        public List<WorkInfo> WorksAfterThesis { get; set; }
    }

    public class WorkInfo
    {
        [BsonElement("Work Title")]
        public string WorkTitle { get; set; }

        [BsonElement("Publication Date")]
        public DateTime PublicationDate { get; set; }

        [BsonElement("Category")]
        public string Category { get; set; }

        [BsonElement("Journal")]
        public string Journal { get; set; }

        [BsonElement("Pages")]
        public int Pages { get; set; }

        [BsonElement("Digital Reference")]
        public string DigitalReference { get; set; }
    }
}
