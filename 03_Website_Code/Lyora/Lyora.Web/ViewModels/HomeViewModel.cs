using Lyora.DAL.Entities;

namespace Lyora.Web.ViewModels;

public class HomeViewModel
{
    public List<Perfume> Featured { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}
