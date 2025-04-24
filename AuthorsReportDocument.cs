using System;
using System.Collections.Generic;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DepartmentLibrary.Services;

namespace DepartmentLibrary.Services
{
    public class AuthorsReportDocument : IDocument
    {
        private readonly List<AuthorReportData> _reportData;

        public AuthorsReportDocument(List<AuthorReportData> reportData)
        {
            _reportData = reportData;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(50);

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().AlignCenter().Text("Department Library Report")
                        .FontSize(20).Bold();

                    column.Item().AlignCenter().Text($"Generated on {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(12);

                    column.Item().AlignCenter().Text($"Publications from {new DateTime(2015, 1, 1):dd/MM/yyyy} to {DateTime.Now:dd/MM/yyyy}")
                        .FontSize(12);

                    column.Item().Height(30);
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                foreach (var authorData in _reportData)
                {
                    column.Item().Element(x => ComposeAuthorSection(x, authorData));
                    column.Item().Height(20);
                }
            });
        }

        private void ComposeAuthorSection(IContainer container, AuthorReportData authorData)
        {
            container.Column(column =>
            {
                // Author name
                column.Item().BorderBottom(1).Padding(5).Element(x => x
                    .Text(authorData.AuthorName)
                    .FontSize(14)
                    .Bold()
                );

                column.Item().Height(10);

                // Works before thesis
                if (authorData.WorksBeforeThesis != null && authorData.WorksBeforeThesis.Count > 0)
                {
                    column.Item().Element(x => x.Text("Works Before Thesis Defense").FontSize(12).Bold());
                    column.Item().Element(x => ComposeWorksTable(x, authorData.WorksBeforeThesis));
                    column.Item().Height(10);
                }

                // Works after thesis
                if (authorData.WorksAfterThesis != null && authorData.WorksAfterThesis.Count > 0)
                {
                    column.Item().Element(x => x.Text("Works After Thesis Defense").FontSize(12).Bold());
                    column.Item().Element(x => ComposeWorksTable(x, authorData.WorksAfterThesis));
                }
            });
        }

        private void ComposeWorksTable(IContainer container, List<WorkInfo> works)
        {
            container.Table(table =>
            {
                // Define columns
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(160);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(40);
                    columns.RelativeColumn();
                });

                // Add header row
                table.Header(header =>
                {
                    header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Title").Bold();
                    header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Publication Date").Bold();
                    header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Category").Bold();
                    header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Journal").Bold();
                    header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Pages").Bold();
                    header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(5).Text("Digital Reference").Bold();
                });

                // Add data rows
                foreach (var work in works)
                {
                    table.Cell().Border(1).Padding(5).Text(work.WorkTitle);
                    table.Cell().Border(1).Padding(5).Text(work.PublicationDate.ToString("dd/MM/yyyy"));
                    table.Cell().Border(1).Padding(5).Text(work.Category);
                    table.Cell().Border(1).Padding(5).Text(work.Journal);
                    table.Cell().Border(1).Padding(5).Text(work.Pages.ToString());
                    table.Cell().Border(1).Padding(5).Text(work.DigitalReference);
                }
            });
        }
    }
}