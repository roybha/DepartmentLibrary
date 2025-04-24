using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DepartmentLibrary.Models;

namespace DepartmentLibrary.Services
{
    public class ReportService
    {
        private readonly IMongoClient _mongoClient;
        private readonly string _databaseName;

        public ReportService(IMongoClient mongoClient, string databaseName)
        {
            _mongoClient = mongoClient;
            _databaseName = databaseName;
            QuestPDF.Settings.License = LicenseType.Community;
            Console.WriteLine("[ReportService] Initialized with database: " + databaseName);
        }

        public async Task<byte[]> GenerateAuthorsReportAsync()
        {
            Console.WriteLine("[ReportService] Starting report generation...");
            try
            {
                Console.WriteLine("[ReportService] Fetching report data from MongoDB...");
                var reportData = await GetReportDataAsync();
                Console.WriteLine($"[ReportService] Retrieved data for {reportData.Count} authors");

                Console.WriteLine("[ReportService] Generating PDF document...");
                var document = new AuthorsReportDocument(reportData);
                var pdfBytes = document.GeneratePdf();

                Console.WriteLine("[ReportService] PDF generation completed successfully");
                return pdfBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportService] ERROR during report generation: {ex}");
                throw;
            }
        }

        private async Task<List<AuthorReportData>> GetReportDataAsync()
        {
            Console.WriteLine("[MongoDB] Initializing database connection...");
            var database = _mongoClient.GetDatabase(_databaseName);
            var worksCollection = database.GetCollection<BsonDocument>("works");
            Console.WriteLine($"[MongoDB] Connected to collection: {worksCollection.CollectionNamespace}");

            Console.WriteLine("[MongoDB] Building aggregation pipeline...");
            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "authors" },
                    { "localField", "authors" },
                    { "foreignField", "_id" },
                    { "as", "author_details" }
                }),
                new BsonDocument("$unwind", "$author_details"),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "categories" },
                    { "localField", "category" },
                    { "foreignField", "_id" },
                    { "as", "category_details" }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "journals" },
                    { "localField", "journal" },
                    { "foreignField", "_id" },
                    { "as", "journal_details" }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "publish_date", new BsonDocument("$ifNull", new BsonArray { "$publish_date", new DateTime(2023, 1, 1) }) },
                    { "category_name", new BsonDocument("$ifNull", new BsonArray
                        {
                            new BsonDocument("$arrayElemAt", new BsonArray { "$category_details.name", 0 }),
                            "Uncategorized"
                        })
                    },
                    { "journal_name", new BsonDocument("$ifNull", new BsonArray
                        {
                            new BsonDocument("$arrayElemAt", new BsonArray { "$journal_details.name", 0 }),
                            "No Journal"
                        })
                    },
                    { "pages", new BsonDocument("$ifNull", new BsonArray { "$pages", 0 }) },
                    { "digital_reference", new BsonDocument("$ifNull", new BsonArray { "$digital_reference", "N/A" }) }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "isBeforeThesis", new BsonDocument("$lt", new BsonArray {
                        "$publish_date", "$author_details.thesis_defense_date"
                    })}
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "author_id", "$author_details._id" },
                    { "author_name", "$author_details.name" },
                    { "isBeforeThesis", 1 },
                    { "Work Title", "$title" },
                    { "Publication Date", "$publish_date" },
                    { "Category", "$category_name" },
                    { "Journal", "$journal_name" },
                    { "Pages", "$pages" },
                    { "Digital Reference", "$digital_reference" }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$author_id" },
                    { "Author Name", new BsonDocument("$first", "$author_name") },
                    { "works_before_thesis", new BsonDocument("$push", new BsonDocument("$cond", new BsonArray {
                        "$isBeforeThesis",
                        new BsonDocument
                        {
                            { "Work Title", "$Work Title" },
                            { "Publication Date", "$Publication Date" },
                            { "Category", "$Category" },
                            { "Journal", "$Journal" },
                            { "Pages", "$Pages" },
                            { "Digital Reference", "$Digital Reference" }
                        },
                        BsonNull.Value
                    })) },
                    { "works_after_thesis", new BsonDocument("$push", new BsonDocument("$cond", new BsonArray {
                        new BsonDocument("$not", new BsonArray { "$isBeforeThesis" }),
                        new BsonDocument
                        {
                            { "Work Title", "$Work Title" },
                            { "Publication Date", "$Publication Date" },
                            { "Category", "$Category" },
                            { "Journal", "$Journal" },
                            { "Pages", "$Pages" },
                            { "Digital Reference", "$Digital Reference" }
                        },
                        BsonNull.Value
                    })) }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "works_before_thesis", new BsonDocument("$filter", new BsonDocument
                        {
                            { "input", "$works_before_thesis" },
                            { "as", "item" },
                            { "cond", new BsonDocument("$ne", new BsonArray { "$$item", BsonNull.Value }) }
                        })
                    },
                    { "works_after_thesis", new BsonDocument("$filter", new BsonDocument
                        {
                            { "input", "$works_after_thesis" },
                            { "as", "item" },
                            { "cond", new BsonDocument("$ne", new BsonArray { "$$item", BsonNull.Value }) }
                        })
                    }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "Author Name", 1 },
                    { "works_before_thesis", 1 },
                    { "works_after_thesis", 1 }
                })
            };

            Console.WriteLine("[MongoDB] Executing aggregation pipeline...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var aggregation = await worksCollection.AggregateAsync<BsonDocument>(pipeline);
            var bsonResults = await aggregation.ToListAsync();
            stopwatch.Stop();

            Console.WriteLine($"[MongoDB] Pipeline executed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"[MongoDB] Retrieved {bsonResults.Count} author documents");

            Console.WriteLine("[Data Processing] Parsing MongoDB results...");
            var results = new List<AuthorReportData>();
            int totalWorks = 0;
            int totalPages = 0;
            var categories = new HashSet<string>();
            var journals = new HashSet<string>();

            foreach (var doc in bsonResults)
            {
                Console.WriteLine($"[Data Processing] Processing author: {doc.GetValue("Author Name", "Unknown")}");

                var author = new AuthorReportData
                {
                    AuthorName = doc.GetValue("Author Name", "").AsString,
                    WorksBeforeThesis = new List<WorkInfo>(),
                    WorksAfterThesis = new List<WorkInfo>()
                };

                if (doc.Contains("works_before_thesis"))
                {
                    var works = doc["works_before_thesis"].AsBsonArray;
                    Console.WriteLine($"[Data Processing] Found {works.Count} works before thesis");
                    foreach (var workDoc in works)
                    {
                        if (workDoc.IsBsonDocument)
                        {
                            var work = ParseWorkInfo(workDoc.AsBsonDocument);
                            author.WorksBeforeThesis.Add(work);
                            totalWorks++;
                            totalPages += work.Pages;
                            categories.Add(work.Category);
                            journals.Add(work.Journal);
                        }
                    }
                }

                if (doc.Contains("works_after_thesis"))
                {
                    var works = doc["works_after_thesis"].AsBsonArray;
                    Console.WriteLine($"[Data Processing] Found {works.Count} works after thesis");
                    foreach (var workDoc in works)
                    {
                        if (workDoc.IsBsonDocument)
                        {
                            var work = ParseWorkInfo(workDoc.AsBsonDocument);
                            author.WorksAfterThesis.Add(work);
                            totalWorks++;
                            totalPages += work.Pages;
                            categories.Add(work.Category);
                            journals.Add(work.Journal);
                        }
                    }
                }

                results.Add(author);
            }

            Console.WriteLine($"[Data Processing] Completed processing:");
            Console.WriteLine($"- Total authors: {results.Count}");
            Console.WriteLine($"- Total works: {totalWorks}");
            Console.WriteLine($"- Total pages: {totalPages}");
            Console.WriteLine($"- Unique categories: {categories.Count}");
            Console.WriteLine($"- Unique journals: {journals.Count}");

            return results;
        }

        private WorkInfo ParseWorkInfo(BsonDocument workDoc)
        {
            var workTitle = workDoc.GetValue("Work Title", "Untitled").AsString;
            var category = workDoc.GetValue("Category", "Uncategorized").AsString;
            var journal = workDoc.GetValue("Journal", "No Journal").AsString;
            var pages = workDoc.GetValue("Pages", 0).AsInt32;

            Console.WriteLine($"[Data Processing] Parsing work:");
            Console.WriteLine($"- Title: {workTitle}");
            Console.WriteLine($"- Category: {category}");
            Console.WriteLine($"- Journal: {journal}");
            Console.WriteLine($"- Pages: {pages}");

            if (pages <= 0)
            {
                Console.WriteLine("[WARNING] Invalid page count detected!");
            }

            return new WorkInfo
            {
                WorkTitle = workTitle,
                PublicationDate = workDoc.GetValue("Publication Date", new DateTime(2023, 1, 1)).ToUniversalTime(),
                Category = category,
                Journal = journal,
                Pages = pages,
                DigitalReference = workDoc.GetValue("Digital Reference", "N/A").AsString
            };
        }
    }

    public class AuthorReportData
    {
        [MongoDB.Bson.Serialization.Attributes.BsonElement("Author Name")]
        public string AuthorName { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("works_before_thesis")]
        public List<WorkInfo> WorksBeforeThesis { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("works_after_thesis")]
        public List<WorkInfo> WorksAfterThesis { get; set; }
    }

    public class WorkInfo
    {
        [MongoDB.Bson.Serialization.Attributes.BsonElement("Work Title")]
        public string WorkTitle { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("Publication Date")]
        public DateTime PublicationDate { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("Category")]
        public string Category { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("Journal")]
        public string Journal { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("Pages")]
        public int Pages { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("Digital Reference")]
        public string DigitalReference { get; set; }
    }
}