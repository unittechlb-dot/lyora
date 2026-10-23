using Lyora.BLL.Services;
using Lyora.DAL.Entities;
using Lyora.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Manage Products and Manage BOM (perfume formulas) use cases.</summary>
public class ManagePerfumesController : AdminControllerBase
{
    private static readonly string[] ImageExtensions = { ".svg", ".png", ".jpg", ".jpeg", ".webp" };

    private readonly ProductService _productService;
    private readonly InventoryService _inventoryService;
    private readonly IWebHostEnvironment _environment;

    public ManagePerfumesController(ProductService productService, InventoryService inventoryService, IWebHostEnvironment environment)
    {
        _productService = productService;
        _inventoryService = inventoryService;
        _environment = environment;
    }

    public IActionResult Index() => View(_productService.GetPerfumes());

    [HttpGet]
    public IActionResult Create() => View("Form", Fill(new PerfumeFormViewModel()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PerfumeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", Fill(model));
        }

        var perfume = ToPerfume(model);
        var result = _productService.SavePerfume(perfume);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", Fill(model));
        }

        Success($"{perfume.Name} was created. Now add its formula (BOM).");
        return RedirectToAction(nameof(Edit), new { id = perfume.PerfumeId });
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var perfume = _productService.GetPerfume(id);
        if (perfume == null)
        {
            return NotFound();
        }

        return View("Form", Fill(new PerfumeFormViewModel
        {
            PerfumeId = perfume.PerfumeId,
            Name = perfume.Name,
            CategoryId = perfume.CategoryId,
            Description = perfume.Description,
            Price = perfume.Price,
            SizeMl = perfume.SizeMl,
            Intensity = perfume.Intensity,
            Style = perfume.Style,
            ImageUrl = perfume.ImageUrl,
            IsActive = perfume.IsActive
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, PerfumeFormViewModel model)
    {
        model.PerfumeId = id;
        if (_productService.GetPerfume(id) == null)
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            return View("Form", Fill(model));
        }

        var result = _productService.SavePerfume(ToPerfume(model));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", Fill(model));
        }

        Success("Perfume saved.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ---------- BOM (formula) ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveBomLine(int id, int materialId, decimal quantityPerBottle)
    {
        var result = _productService.SaveBomLine(id, materialId, quantityPerBottle);
        if (result.Succeeded)
        {
            Success("Formula updated.");
        }
        else
        {
            Error(result.Error!);
        }
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveBomLine(int id, int bomItemId)
    {
        _productService.RemoveBomLine(id, bomItemId);
        Success("Raw material removed from the formula.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ---------- helpers ----------

    private PerfumeFormViewModel Fill(PerfumeFormViewModel model)
    {
        model.Categories = _productService.GetCategories();
        model.Images = GetImages();
        model.Materials = _inventoryService.GetMaterials().OrderBy(m => m.Name).ToList();
        if (model.PerfumeId != 0)
        {
            model.Bom = _productService.GetBom(model.PerfumeId);
            model.ProducibleBottles = _productService.GetPerfume(model.PerfumeId)?.ProducibleBottles ?? 0;
        }
        return model;
    }

    /// <summary>Images available in wwwroot/images (the administrator chooses one from the list).</summary>
    private List<string> GetImages()
    {
        var folder = Path.Combine(_environment.WebRootPath, "images");
        if (!Directory.Exists(folder))
        {
            return new List<string>();
        }
        return Directory.GetFiles(folder)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Select(f => "/images/" + Path.GetFileName(f))
            .Where(f => f != "/images/hero-bottles.svg")
            .OrderBy(f => f)
            .ToList();
    }

    private static Perfume ToPerfume(PerfumeFormViewModel model) => new()
    {
        PerfumeId = model.PerfumeId,
        Name = model.Name,
        CategoryId = model.CategoryId,
        Description = model.Description,
        Price = model.Price,
        SizeMl = model.SizeMl,
        Intensity = model.Intensity,
        Style = model.Style,
        ImageUrl = model.ImageUrl,
        IsActive = model.IsActive
    };
}
