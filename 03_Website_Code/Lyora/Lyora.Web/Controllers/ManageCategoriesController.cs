using Lyora.BLL.Services;
using Lyora.DAL.Entities;
using Lyora.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Manage Categories (perfume families) use case.</summary>
public class ManageCategoriesController : AdminControllerBase
{
    private readonly ProductService _productService;

    public ManageCategoriesController(ProductService productService)
    {
        _productService = productService;
    }

    public IActionResult Index()
    {
        ViewBag.Categories = _productService.GetCategories();
        return View(new CategoryFormViewModel());
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var category = _productService.GetCategories().FirstOrDefault(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound();
        }
        return View(new CategoryFormViewModel { CategoryId = category.CategoryId, Name = category.Name, Description = category.Description });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(CategoryFormViewModel model)
    {
        var viewName = model.CategoryId == 0 ? nameof(Index) : nameof(Edit);
        if (model.CategoryId != 0 && _productService.GetCategories().All(c => c.CategoryId != model.CategoryId))
        {
            return NotFound();
        }
        if (ModelState.IsValid)
        {
            var result = _productService.SaveCategory(new Category
            {
                CategoryId = model.CategoryId,
                Name = model.Name,
                Description = model.Description
            });
            if (result.Succeeded)
            {
                Success(model.CategoryId == 0 ? "Family added." : "Family saved.");
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }

        ViewBag.Categories = _productService.GetCategories();
        return View(viewName, model);
    }
}
