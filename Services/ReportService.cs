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
using MongoDB.Bson.Serialization;

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

        // Updated method signature to include userId and userRole
        public async Task<byte[]> GenerateAuthorsReportAsync(DateTime startDate, DateTime endDate, string userId, string userRole)
        {
            Console.WriteLine("[ReportService] Starting report generation...");
            try
            {
                Console.WriteLine($"[ReportService] Fetching report data from MongoDB between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}...");
                var reportData = await GetReportDataAsync(startDate, endDate, userId, userRole);
                Console.WriteLine($"[ReportService] Retrieved data for {reportData.Count} authors");

                Console.WriteLine("[ReportService] Generating PDF document...");
                var document = new AuthorsReportDocument(reportData, startDate, endDate);
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

        // Updated method signature and pipeline to incorporate role-based filtering
        private async Task<List<AuthorReportData>> GetReportDataAsync(DateTime startDate, DateTime endDate, string userId, string userRole)
        {
            var database = _mongoClient.GetDatabase(_databaseName);
            var worksCollection = database.GetCollection<BsonDocument>("works");

            // Base match stage for date range and null publish_date checks
            var dateMatch = new BsonDocument("$or", new BsonArray
                {
                    new BsonDocument
                    {
                        { "publish_date", new BsonDocument
                            {
                                { "$gte", startDate },
                                { "$lte", endDate }
                            }
                        }
                    },
                    new BsonDocument
                    {
                        { "publish_date", BsonNull.Value }
                    }
                });

            var matchConditions = new List<BsonDocument>
            {
                dateMatch
            };

            // If role is author, add filter for works authored by this user only
            if (userRole == "author")
            {
                // Assumes authors field is an array of ObjectIds
                var authorFilter = new BsonDocument("authors", new ObjectId(userId));
                matchConditions.Add(authorFilter);
            }

            var matchStage = new BsonDocument("$match", new BsonDocument("$and", new BsonArray(matchConditions)));

            var pipeline = new BsonDocument[]
            {
                matchStage,

                // Lookup authors first
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "authors" },
                    { "localField", "authors" },
                    { "foreignField", "_id" },
                    { "as", "author_details" }
                }),

                // Lookup categories
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "categories" },
                    { "localField", "category" },
                    { "foreignField", "_id" },
                    { "as", "category_details" }
                }),

                // Lookup journals
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "journals" },
                    { "localField", "journal" },
                    { "foreignField", "_id" },
                    { "as", "journal_details" }
                }),

                // Make sure we have at least one author
                new BsonDocument("$match", new BsonDocument
                {
                    { "author_details", new BsonDocument("$ne", new BsonArray()) }
                }),

                // Unwind authors
                new BsonDocument("$unwind", "$author_details"),

                // Add debugging field for thesis defense date and handle null publish dates
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "debug_thesis_date", "$author_details.thesis_defense_date" },

                    // Improved date handling - use a conditional to check if publish_date exists
                    { "publish_date_safe", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$eq", new BsonArray { "$publish_date", BsonNull.Value }) },
                            // For null publish dates, use middle of the specified date range
                            { "then", startDate.AddDays((endDate - startDate).TotalDays / 2) },
                            { "else", "$publish_date" }
                        })
                    }
                }),

                // Now create separate documents for before and after thesis
                new BsonDocument("$facet", new BsonDocument
                {
                    { "before_thesis", new BsonArray
                        {
                            // Match works before thesis defense or with null defense date
                            new BsonDocument("$match", new BsonDocument("$or", new BsonArray
                                {
                                    new BsonDocument("$expr", new BsonDocument("$eq", new BsonArray { "$author_details.thesis_defense_date", BsonNull.Value })),
                                    new BsonDocument("$expr", new BsonDocument("$lt", new BsonArray { "$publish_date_safe", "$author_details.thesis_defense_date" }))
                                }))
                            ,
                            // Project necessary fields
                            new BsonDocument("$project", new BsonDocument
                            {
                                { "_id", 0 },
                                { "author_id", "$author_details._id" },
                                { "author_name", "$author_details.name" },
                                { "work", new BsonDocument
                                    {
                                        { "Work Title", new BsonDocument("$ifNull", new BsonArray { "$title", "Untitled" }) },
                                        { "Publication Date", "$publish_date_safe" },
                                        { "Category", new BsonDocument("$cond", new BsonDocument
                                            {
                                                { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$category_details"), 0 }) },
                                                { "then", new BsonDocument("$arrayElemAt", new BsonArray { "$category_details.title", 0 }) },
                                                { "else", "Uncategorized" }
                                            })
                                        },
                                        { "Journal", new BsonDocument("$cond", new BsonDocument
                                            {
                                                { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$journal_details"), 0 }) },
                                                { "then", new BsonDocument("$arrayElemAt", new BsonArray { "$journal_details.title", 0 }) },
                                                { "else", "No Journal" }
                                            })
                                        },
                                        { "Pages", new BsonDocument("$ifNull", new BsonArray { "$pages_num", 0 }) },
                                        { "Digital Reference", new BsonDocument("$ifNull", new BsonArray { "$digital_reference", "N/A" }) }
                                    }
                                },
                                { "debug_publish_date", "$publish_date_safe" },
                                { "debug_thesis_date", "$debug_thesis_date" }
                            })
                        }
                    },
                    { "after_thesis", new BsonArray
                        {
                            // Match works after thesis defense with non-null defense date
                            new BsonDocument("$match", new BsonDocument("$and", new BsonArray
                                {
                                    new BsonDocument("$expr", new BsonDocument("$ne", new BsonArray { "$author_details.thesis_defense_date", BsonNull.Value })),
                                    new BsonDocument("$expr", new BsonDocument("$gte", new BsonArray { "$publish_date_safe", "$author_details.thesis_defense_date" }))
                                }))
                            ,
                            // Project necessary fields
                            new BsonDocument("$project", new BsonDocument
                            {
                                { "_id", 0 },
                                { "author_id", "$author_details._id" },
                                { "author_name", "$author_details.name" },
                                { "work", new BsonDocument
                                    {
                                        { "Work Title", new BsonDocument("$ifNull", new BsonArray { "$title", "Untitled" }) },
                                        { "Publication Date", "$publish_date_safe" },
                                        { "Category", new BsonDocument("$cond", new BsonDocument
                                            {
                                                { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$category_details"), 0 }) },
                                                { "then", new BsonDocument("$arrayElemAt", new BsonArray { "$category_details.title", 0 }) },
                                                { "else", "Uncategorized" }
                                            })
                                        },
                                        { "Journal", new BsonDocument("$cond", new BsonDocument
                                            {
                                                { "if", new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$journal_details"), 0 }) },
                                                { "then", new BsonDocument("$arrayElemAt", new BsonArray { "$journal_details.title", 0 }) },
                                                { "else", "No Journal" }
                                            })
                                        },
                                        { "Pages", new BsonDocument("$ifNull", new BsonArray { "$pages_num", 0 }) },
                                        { "Digital Reference", new BsonDocument("$ifNull", new BsonArray { "$digital_reference", "N/A" }) }
                                    }
                                },
                                { "debug_publish_date", "$publish_date_safe" },
                                { "debug_thesis_date", "$debug_thesis_date" }
                            })
                        }
                    }
                }),

                // Unwind both result sets
                new BsonDocument("$project", new BsonDocument
                {
                    { "all_data", new BsonDocument("$concatArrays", new BsonArray { "$before_thesis", "$after_thesis" }) }
                }),
                new BsonDocument("$unwind", "$all_data"),

                // Group by author to separate works before and after thesis
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$all_data.author_id" },
                    { "Author Name", new BsonDocument("$first", "$all_data.author_name") },
                    { "works_before_thesis", new BsonDocument("$push", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$or", new BsonArray
                                {
                                    new BsonDocument("$eq", new BsonArray { "$all_data.debug_thesis_date", BsonNull.Value }),
                                    new BsonDocument("$lt", new BsonArray { "$all_data.debug_publish_date", "$all_data.debug_thesis_date" })
                                })
                            },
                            { "then", "$all_data.work" },
                            { "else", BsonNull.Value }
                        }))
                    },
                    { "works_after_thesis", new BsonDocument("$push", new BsonDocument("$cond", new BsonDocument
                        {
                            { "if", new BsonDocument("$and", new BsonArray
                                {
                                    new BsonDocument("$ne", new BsonArray { "$all_data.debug_thesis_date", BsonNull.Value }),
                                    new BsonDocument("$gte", new BsonArray { "$all_data.debug_publish_date", "$all_data.debug_thesis_date" })
                                })
                            },
                            { "then", "$all_data.work" },
                            { "else", BsonNull.Value }
                        }))
                    },
                    { "debug_dates", new BsonDocument("$push", new BsonDocument
                        {
                            { "publish_date", "$all_data.debug_publish_date" },
                            { "thesis_date", "$all_data.debug_thesis_date" }
                        })
                    }
                }),

                // Clean up the null values
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "Author Name", 1 },
                    { "works_before_thesis", new BsonDocument("$filter", new BsonDocument
                        {
                            { "input", "$works_before_thesis" },
                            { "as", "work" },
                            { "cond", new BsonDocument("$ne", new BsonArray { "$$work", BsonNull.Value }) }
                        })
                    },
                    { "works_after_thesis", new BsonDocument("$filter", new BsonDocument
                        {
                            { "input", "$works_after_thesis" },
                            { "as", "work" },
                            { "cond", new BsonDocument("$ne", new BsonArray { "$$work", BsonNull.Value }) }
                        })
                    },
                    { "debug_dates", 1 }
                })
            };

            try
            {
                // Debug the date range
                System.Diagnostics.Debug.WriteLine($"Filtering works between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}");

                // Use a raw result class to include debugging information
                var aggregationResult = await worksCollection.AggregateAsync<BsonDocument>(pipeline);
                var rawResults = await aggregationResult.ToListAsync();

                // Convert to your AuthorReportData model and debug
                var results = new List<AuthorReportData>();
                int beforeThesisCount = 0;
                int afterThesisCount = 0;

                foreach (var doc in rawResults)
                {
                    // Extract data from BsonDocument
                    var authorData = new AuthorReportData
                    {
                        AuthorName = doc["Author Name"].AsString,
                        WorksBeforeThesis = BsonSerializer.Deserialize<List<WorkInfo>>(doc["works_before_thesis"].ToJson()),
                        WorksAfterThesis = BsonSerializer.Deserialize<List<WorkInfo>>(doc["works_after_thesis"].ToJson())
                    };

                    // Debug information
                    beforeThesisCount += authorData.WorksBeforeThesis?.Count ?? 0;
                    afterThesisCount += authorData.WorksAfterThesis?.Count ?? 0;

                    // Debug the date comparisons
                    if (doc.Contains("debug_dates"))
                    {
                        var debugDates = doc["debug_dates"].AsBsonArray;
                        System.Diagnostics.Debug.WriteLine($"Author: {authorData.AuthorName}");
                        foreach (var dateInfo in debugDates)
                        {
                            var dateDoc = dateInfo.AsBsonDocument;
                            if (dateDoc.Contains("publish_date") && dateDoc.Contains("thesis_date"))
                            {
                                var publishDate = dateDoc["publish_date"].IsBsonNull ? "NULL" : dateDoc["publish_date"].ToUniversalTime().ToString("yyyy-MM-dd");
                                var thesisDate = dateDoc["thesis_date"].IsBsonNull ? "NULL" : dateDoc["thesis_date"].ToUniversalTime().ToString("yyyy-MM-dd");
                                System.Diagnostics.Debug.WriteLine($"  Publish date: {publishDate}, Thesis date: {thesisDate}");
                            }
                        }
                    }

                    results.Add(authorData);
                }

                System.Diagnostics.Debug.WriteLine($"Total works before thesis: {beforeThesisCount}");
                System.Diagnostics.Debug.WriteLine($"Total works after thesis: {afterThesisCount}");
                System.Diagnostics.Debug.WriteLine($"Total authors in report: {results.Count}");

                return results;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving report data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to let the caller handle it
            }
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

