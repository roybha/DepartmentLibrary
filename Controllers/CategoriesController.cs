using DepartmentLibrary.Models;
using DepartmentLibrary.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DepartmentLibrary.Controllers;

[Authorize(Roles = "admin")]
public class CategoriesController : Controller
{
    private readonly IMongoRepository<Category> _repository;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        IMongoRepository<Category> repository,
        ILogger<CategoriesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // GET: Categories
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Запит на отримання всіх категорій");
        return View(await _repository.GetAllAsync());
    }

    // GET: Categories/Create
    public IActionResult Create() => View();

    // POST: Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid data entered");
            return View(category);
        }

        await _repository.CreateAsync(category);
        _logger.LogInformation("Categories {CategoryId} created", category.Id);
        return RedirectToAction(nameof(Index));
    }
    
    // GET: Authors/Edit/{id}
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Category category)
    {
        if (id != category.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid data entered for editing: {CategoryId}", id);
            return View(category);
        }

        await _repository.UpdateAsync(id, category);
        _logger.LogInformation("Category {CategoryId} edited", id);
        return RedirectToAction(nameof(Index));
    }
    
    public async Task<IActionResult> Delete(string id)
    {
        var category = await _repository.GetByIdAsync(id);
        return category == null ? NotFound() : View(category);
    }
    
    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Category {CategoryId} deleted", id);
        return RedirectToAction(nameof(Index));
    }
}