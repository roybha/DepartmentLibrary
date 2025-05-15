using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DepartmentLibrary.Services;
using Microsoft.AspNetCore.Http; // For accessing HttpContext

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

        // Change to HttpGet to allow direct URL access
        [HttpGet]
        public async Task<IActionResult> Generate(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get the current date for the report filename
                var currentDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"AuthorsReport_{currentDate}.pdf";

                // Retrieve userId and userRole from the current user context
                var userId = HttpContext.User.FindFirst("UserId")?.Value; // Adjust based on your authentication setup
                var userRole = HttpContext.User.FindFirst("UserRole")?.Value; // Adjust based on your authentication setup

                // Debugging output
                System.Diagnostics.Debug.WriteLine($"User  ID: {userId}, User Role: {userRole}");

                // Generate the PDF report with date filter
                var reportStream = await _reportService.GenerateAuthorsReportAsync(startDate, endDate, userId, userRole);

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

