using Lyora.DAL.Entities;

namespace Lyora.Web.ViewModels;

public class PerfumeDetailsViewModel
{
    public Perfume Perfume { get; set; } = new();
    public List<BOMItem> Ingredients { get; set; } = new();
}
