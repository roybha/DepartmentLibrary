using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using DepartmentLibrary.Services;
using Microsoft.AspNetCore.Authorization;

namespace DepartmentLibrary.Controllers
{
    [Authorize] // Ensure only authenticated users can access
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

        [HttpGet]
        public async Task<IActionResult> Generate(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Extract user claims from HttpContext
                var userEmail = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown@example.com";
                var userRole = HttpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? "UnknownRole";
                var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "UnknownUserId";

                // Debug logging
                System.Diagnostics.Debug.WriteLine($"Generating report for user: {userEmail}, Role: {userRole}, ID: {userId}");

                // Generate filename
                var currentDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"AuthorsReport_{currentDate}.pdf";

                // Generate the PDF report
                var reportStream = await _reportService.GenerateAuthorsReportAsync(
                    startDate, endDate, userId, userRole, HttpContext.User
                );

                // Return the PDF file
                return File(reportStream, "application/pdf", filename);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating report: {ex.Message}");
                TempData["ErrorMessage"] = $"Error generating report: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}

