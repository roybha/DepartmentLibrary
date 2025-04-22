using DepartmentLibrary.Models;
using DepartmentLibrary.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace DepartmentLibrary.Controllers;

[Authorize]
public class WorksController : Controller
{
    private readonly IMongoRepository<Work> _repository;
    private readonly IMongoRepository<Author> _authorRepository;
    private readonly IMongoRepository<Category> _categoryRepository;
    private readonly IMongoRepository<Journal> _journalRepository;
    private readonly ILogger<WorksController> _logger;

    public WorksController(
        IMongoRepository<Work> repository,
        IMongoRepository<Author> authorRepository,
        ILogger<WorksController> logger,
        IMongoRepository<Category> categoryRepository,
        IMongoRepository<Journal> journalRepository)
    {
        _repository = repository;
        _authorRepository = authorRepository;
        _logger = logger;
        _categoryRepository = categoryRepository;
        _journalRepository = journalRepository;
    }

    // GET: Works
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Get all works");
        var works = await _repository.GetAllAsync();
        return View(works);
    }

    // GET: Works/Create
    public async Task<IActionResult> Create()
    {
        try
        {
            ViewBag.Authors = await _authorRepository.GetAllAsync() ?? new List<Author>();
            ViewBag.Categories = await _categoryRepository.GetAllAsync() ?? new List<Category>();
            ViewBag.Journals = await _journalRepository.GetAllAsync() ?? new List<Journal>();
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при завантаженні даних для створення роботи");
            return RedirectToAction("Error", "Home");
        }
    }

    // POST: Works/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Work work)
    {
        // Custom validation: At least one author must be selected
        if (work.AuthorIds == null || work.AuthorIds.Count == 0)
        {
            ModelState.AddModelError("AuthorIds", "Please select at least one author.");
        }

        // Check if model state is valid
        if (!ModelState.IsValid)
        {
            // Log all validation errors
            foreach (var entry in ModelState)
            {
                if (entry.Value.Errors.Count > 0)
                {
                    string errorMessages = string.Join(", ", entry.Value.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Validation error for field '{entry.Key}': {errorMessages}");
                }
            }

            // Reload dropdown data for the form
            ViewBag.Authors = await _authorRepository.GetAllAsync();
            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            ViewBag.Journals = await _journalRepository.GetAllAsync();

            return View(work);
        }

        try
        {
            // Save the work to the database
            await _repository.CreateAsync(work);
            _logger.LogInformation($"Work {work.Id} created successfully.");
            return RedirectToAction(nameof(Index));
        }
        catch (MongoWriteException ex)
        {
            if (ex.WriteError?.Category == ServerErrorCategory.Uncategorized)
            {
                _logger.LogError($"Database validation error: {ex.WriteError.Message}");
                ModelState.AddModelError(string.Empty, "Validation failed due to database rules. Check your input.");
            }
            else
            {
                _logger.LogError(ex, "General MongoDB write error");
                ModelState.AddModelError(string.Empty, "Failed to save data. Please try again.");
            }
    
            // Reload dropdown data
            ViewBag.Authors = await _authorRepository.GetAllAsync() ?? new List<Author>();
            ViewBag.Categories = await _categoryRepository.GetAllAsync() ?? new List<Category>();
            ViewBag.Journals = await _journalRepository.GetAllAsync() ?? new List<Journal>();
    
            return View(work);
        }
    }
    
    //TODO GenerateReport
}