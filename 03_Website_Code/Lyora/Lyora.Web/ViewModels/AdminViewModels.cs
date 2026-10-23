using System.ComponentModel.DataAnnotations;
using Lyora.DAL.Entities;

namespace Lyora.Web.ViewModels;

public class ManageOrdersViewModel
{
    public List<Order> Orders { get; set; } = new();
    public OrderStatus? Status { get; set; }
}

public class PerfumeFormViewModel
{
    public int PerfumeId { get; set; }

    [Required(ErrorMessage = "Please enter the name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2 to 100 characters.")]
    public string Name { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a family.")]
    [Display(Name = "Family")]
    public int CategoryId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "1", "10000", ErrorMessage = "Price must be between $1 and $10,000.")]
    [Display(Name = "Price ($)")]
    public decimal Price { get; set; }

    [Range(5, 1000, ErrorMessage = "Size must be between 5 and 1000 ml.")]
    [Display(Name = "Size (ml)")]
    public int SizeMl { get; set; } = 50;

    public ScentIntensity Intensity { get; set; }
    public ScentStyle Style { get; set; }

    [Display(Name = "Image")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Visible on the website")]
    public bool IsActive { get; set; } = true;

    // Lists for the drop-downs and the BOM section (not posted back)
    public List<Category> Categories { get; set; } = new();
    public List<string> Images { get; set; } = new();
    public List<BOMItem> Bom { get; set; } = new();
    public List<RawMaterial> Materials { get; set; } = new();
    public int ProducibleBottles { get; set; }
}

public class MaterialFormViewModel
{
    public int MaterialId { get; set; }

    [Required(ErrorMessage = "Please enter the name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2 to 100 characters.")]
    public string Name { get; set; } = "";

    [Required]
    public string Unit { get; set; } = "ml";

    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "Threshold must be between 0 and 1,000,000.")]
    [Display(Name = "Low-stock threshold")]
    public decimal Threshold { get; set; } = 1000;

    [Range(typeof(decimal), "0", "10000", ErrorMessage = "Cost must be between 0 and 10,000.")]
    [Display(Name = "Cost per unit ($)")]
    public decimal CostPerUnit { get; set; }

    [Display(Name = "Allergen (customers can be allergic to it)")]
    public bool IsAllergen { get; set; }
}

public class MaterialDetailsViewModel
{
    public RawMaterial Material { get; set; } = new();
    public List<InventoryTransaction> Transactions { get; set; } = new();
}

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please enter the name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2 to 100 characters.")]
    public string Name { get; set; } = "";

    [StringLength(300)]
    public string? Description { get; set; }
}
