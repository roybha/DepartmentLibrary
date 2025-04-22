using DepartmentLibrary.Models;
using DepartmentLibrary.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DepartmentLibrary.Controllers;

[Authorize(Roles = "admin")]
public class AuthorsController : Controller
{
    private readonly ILogger<AuthorsController> _logger;
    private readonly IMongoRepository<Author> _repository;

    public AuthorsController(ILogger<AuthorsController> logger, IMongoRepository<Author> repository)
    {
        _logger = logger;
        _repository = repository;
    }
    
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Get all Authors");
        var authors = await _repository.GetAllAsync();
        return View(authors);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Author author)
    {
        if (!ModelState.IsValid)
        {
            foreach (var entry in ModelState)
            {
                if (entry.Value.Errors.Count > 0)
                {
                    string errorMessages = string.Join(", ", entry.Value.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Validation error for field '{entry.Key}': {errorMessages}");
                }
            }
            _logger.LogWarning("Invalid data provided while creating author");
            return View(author);
        }

        await _repository.CreateAsync(author);
        _logger.LogInformation("New author created: {AuthorId}", author.Id);
        return RedirectToAction("Index");
    }
    
    // GET: Authors/Edit/{id}
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var author = await _repository.GetByIdAsync(id);
        if (author == null)
        {
            return NotFound();
        }
        return View(author);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Author author)
    {
        if (id != author.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid data provided while editing: {AuthorId}", id);
            return View(author);
        }

        await _repository.UpdateAsync(id, author);
        _logger.LogInformation("Author {AuthorId} updated", id);
        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var author = await _repository.GetByIdAsync(id);
        return author == null ? NotFound() : View(author);
    }
    
    [HttpPost]
    [ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Author {AuthorId} deleted", id);
        return RedirectToAction(nameof(Index));
    }
}