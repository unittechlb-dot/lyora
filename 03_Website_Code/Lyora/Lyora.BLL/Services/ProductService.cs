using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>Administrator: Manage Products &amp; Categories and Manage BOM (perfume formulas).</summary>
public class ProductService
{
    private readonly PerfumeRepository _perfumes;
    private readonly CategoryRepository _categories;
    private readonly RawMaterialRepository _materials;

    public ProductService(PerfumeRepository perfumes, CategoryRepository categories, RawMaterialRepository materials)
    {
        _perfumes = perfumes;
        _categories = categories;
        _materials = materials;
    }

    // ---------- Perfumes ----------

    public List<Perfume> GetPerfumes() => _perfumes.GetAllForAdmin();

    public Perfume? GetPerfume(int perfumeId) => _perfumes.GetById(perfumeId);

    public List<BOMItem> GetBom(int perfumeId) => _perfumes.GetBom(perfumeId);

    /// <summary>Creates or updates a perfume. On create, <paramref name="perfume"/> gets its new id.</summary>
    public ServiceResult SavePerfume(Perfume perfume)
    {
        perfume.Name = perfume.Name.Trim();
        perfume.Description = string.IsNullOrWhiteSpace(perfume.Description) ? null : perfume.Description.Trim();
        perfume.ImageUrl = string.IsNullOrWhiteSpace(perfume.ImageUrl) ? null : perfume.ImageUrl.Trim();

        if (perfume.ImageUrl != null && !perfume.ImageUrl.StartsWith("/images/", StringComparison.Ordinal))
        {
            return ServiceResult.Fail("Please choose an image from the list.");
        }
        if (_categories.GetAll().All(c => c.CategoryId != perfume.CategoryId))
        {
            return ServiceResult.Fail("Please choose a family.");
        }
        var exceptId = perfume.PerfumeId == 0 ? (int?)null : perfume.PerfumeId;
        if (_perfumes.NameExists(perfume.Name, exceptId))
        {
            return ServiceResult.Fail("Another perfume already has this name.");
        }

        if (perfume.PerfumeId == 0)
        {
            perfume.PerfumeId = _perfumes.Create(perfume);
        }
        else
        {
            _perfumes.Update(perfume);
        }
        return ServiceResult.Ok();
    }

    /// <summary>Adds a raw material to a perfume formula, or changes its quantity per bottle.</summary>
    public ServiceResult SaveBomLine(int perfumeId, int materialId, decimal quantityPerBottle)
    {
        if (_perfumes.GetById(perfumeId) == null)
        {
            return ServiceResult.Fail("Perfume not found.");
        }
        var material = _materials.GetById(materialId);
        if (material == null)
        {
            return ServiceResult.Fail("Please choose a raw material.");
        }
        if (quantityPerBottle <= 0 || quantityPerBottle > 1000)
        {
            return ServiceResult.Fail("The quantity per bottle must be between 0.01 and 1000.");
        }
        if (material.Unit == "pcs" && quantityPerBottle != Math.Floor(quantityPerBottle))
        {
            return ServiceResult.Fail($"{material.Name} is counted in pieces: use a whole number.");
        }

        _perfumes.SaveBomLine(perfumeId, materialId, quantityPerBottle);
        return ServiceResult.Ok();
    }

    public void RemoveBomLine(int perfumeId, int bomItemId) => _perfumes.RemoveBomLine(perfumeId, bomItemId);

    // ---------- Categories (perfume families) ----------

    public List<Category> GetCategories() => _categories.GetAll();

    public ServiceResult SaveCategory(Category category)
    {
        category.Name = category.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(category.Description) ? null : category.Description.Trim();

        var exceptId = category.CategoryId == 0 ? (int?)null : category.CategoryId;
        if (_categories.NameExists(category.Name, exceptId))
        {
            return ServiceResult.Fail("Another family already has this name.");
        }

        if (category.CategoryId == 0)
        {
            _categories.Create(category);
        }
        else
        {
            _categories.Update(category);
        }
        return ServiceResult.Ok();
    }
}
