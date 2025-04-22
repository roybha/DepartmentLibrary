using DepartmentLibrary.Models;
using DepartmentLibrary.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DepartmentLibrary.Controllers;

[Authorize(Roles = "admin")]
public class JournalsController : Controller
{
    private readonly IMongoRepository<Journal> _repository;
    private readonly ILogger<JournalsController> _logger;

    public JournalsController(
        IMongoRepository<Journal> repository,
        ILogger<JournalsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // GET: Journals
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Запит на отримання всіх журналів");
        var journals = await _repository.GetAllAsync();
        return View(journals);
    }

    // GET: Journals/Create
    public IActionResult Create() => View();

    // POST: Journals/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Journal journal)
    {
        if (journal.Pages <= 0)
            ModelState.AddModelError("Pages", "Кількість сторінок має бути більше 0");

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
            _logger.LogWarning("Invalid data");
            return View(journal);
        }

        await _repository.CreateAsync(journal);
        _logger.LogInformation("Journal {JournalId} created", journal.Id);
        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var journal = await _repository.GetByIdAsync(id);
        if (journal == null)
        {
            return NotFound();
        }
        return View(journal);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Journal journal)
    {
        if (id != journal.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid editing data {JournalId}", id);
            return View(journal);
        }

        await _repository.UpdateAsync(id, journal);
        _logger.LogInformation("Journal {JournalId} edited", id);
        return RedirectToAction(nameof(Index));
    }
    
    public async Task<IActionResult> Delete(string id)
    {
        var journal = await _repository.GetByIdAsync(id);
        return journal == null ? NotFound() : View(journal);
    }
    
    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Journal {JournalId} deleted", id);
        return RedirectToAction(nameof(Index));
    }
}