using Lyora.DAL.Entities;

namespace Lyora.Web.ViewModels;

/// <summary>The perfume list page: results + the filters the visitor selected.</summary>
public class CatalogViewModel
{
    public List<Perfume> Perfumes { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public ScentIntensity? Intensity { get; set; }
    public ScentStyle? Style { get; set; }

    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || CategoryId.HasValue || Intensity.HasValue || Style.HasValue;
}
