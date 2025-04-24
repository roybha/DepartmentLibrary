using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DepartmentLibrary.Services;

namespace DepartmentLibrary.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ReportService _reportService;

        public ReportsController(ReportService reportService)
        {
            _reportService = reportService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Generate()
        {
            try
            {
                // Get the current date for the report filename
                var currentDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"AuthorsReport_{currentDate}.pdf";

                // Generate the PDF report
                var reportStream = await _reportService.GenerateAuthorsReportAsync();

                // Return the PDF as a file download
                return File(reportStream, "application/pdf", filename);
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error generating report: {ex.Message}");

                // Redirect to error page or show error message
                TempData["ErrorMessage"] = $"Error generating report: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}