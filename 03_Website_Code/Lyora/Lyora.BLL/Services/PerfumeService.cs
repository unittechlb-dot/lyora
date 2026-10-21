using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>
/// Business rules for browsing perfumes (used by visitors and customers).
/// The web pages call this service; the service calls the repositories.
/// </summary>
public class PerfumeService
{
    private const int MaxSearchLength = 100;

    private readonly PerfumeRepository _perfumes;
    private readonly CategoryRepository _categories;

    public PerfumeService(PerfumeRepository perfumes, CategoryRepository categories)
    {
        _perfumes = perfumes;
        _categories = categories;
    }

    public List<Perfume> Search(string? name, int? categoryId, ScentIntensity? intensity, ScentStyle? style)
    {
        name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        if (name is { Length: > MaxSearchLength })
        {
            name = name[..MaxSearchLength];
        }
        return _perfumes.Search(name, categoryId, intensity, style);
    }

    public List<Perfume> GetFeatured(int count = 3) => _perfumes.GetFeatured(count);

    public List<Category> GetCategories() => _categories.GetAll();

    /// <summary>Returns null when the perfume does not exist or is no longer sold.</summary>
    public Perfume? GetPerfume(int perfumeId)
    {
        var perfume = _perfumes.GetById(perfumeId);
        return perfume is { IsActive: true } ? perfume : null;
    }

    /// <summary>
    /// Ingredient names shown to customers (allergens first) – quantities stay private
    /// because the formula is the brand's business secret.
    /// </summary>
    public List<BOMItem> GetIngredients(int perfumeId) =>
        _perfumes.GetBom(perfumeId)
                 .Where(i => i.Unit != "pcs")          // packaging (bottles) is not an ingredient
                 .OrderByDescending(i => i.IsAllergen)
                 .ToList();
}
