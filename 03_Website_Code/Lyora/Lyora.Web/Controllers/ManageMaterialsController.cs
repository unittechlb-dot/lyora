using Lyora.BLL.Services;
using Lyora.DAL.Entities;
using Lyora.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Manage Raw Materials and Manage Inventory (restock + stock history) use cases.</summary>
public class ManageMaterialsController : AdminControllerBase
{
    private readonly InventoryService _inventoryService;

    public ManageMaterialsController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public IActionResult Index() => View(_inventoryService.GetMaterials());

    public IActionResult Details(int id)
    {
        var material = _inventoryService.GetMaterial(id);
        if (material == null)
        {
            return NotFound();
        }
        return View(new MaterialDetailsViewModel { Material = material, Transactions = _inventoryService.GetTransactions(id) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Restock(int id, decimal quantity)
    {
        var result = _inventoryService.Restock(id, quantity);
        if (result.Succeeded)
        {
            var material = _inventoryService.GetMaterial(id)!;
            Success(material.IsBelowThreshold
                ? $"Stock updated. {material.Name} is still below its threshold."
                : $"Stock updated. {material.Name} is above its threshold again.");
        }
        else
        {
            Error(result.Error!);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new MaterialFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(MaterialFormViewModel model) => Save(model, "Raw material added. Use Restock to add the first quantity.");

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var m = _inventoryService.GetMaterial(id);
        if (m == null)
        {
            return NotFound();
        }
        return View("Form", new MaterialFormViewModel
        {
            MaterialId = m.MaterialId,
            Name = m.Name,
            Unit = m.Unit,
            Threshold = m.Threshold,
            CostPerUnit = m.CostPerUnit,
            IsAllergen = m.IsAllergen
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, MaterialFormViewModel model)
    {
        if (_inventoryService.GetMaterial(id) == null)
        {
            return NotFound();
        }
        model.MaterialId = id;
        return Save(model, "Raw material saved.");
    }

    private IActionResult Save(MaterialFormViewModel model, string successMessage)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var material = new RawMaterial
        {
            MaterialId = model.MaterialId,
            Name = model.Name,
            Unit = model.Unit,
            Threshold = model.Threshold,
            CostPerUnit = model.CostPerUnit,
            IsAllergen = model.IsAllergen
        };
        var result = _inventoryService.SaveMaterial(material);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", model);
        }

        Success(successMessage);
        return RedirectToAction(nameof(Index));
    }
}
