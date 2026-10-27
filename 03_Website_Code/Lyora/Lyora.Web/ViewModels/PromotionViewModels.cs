using System.ComponentModel.DataAnnotations;
using Lyora.DAL.Entities;

namespace Lyora.Web.ViewModels;

public class PromotionFormViewModel
{
    public int PromotionId { get; set; }

    [Required(ErrorMessage = "Please enter the title.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Title must be 3 to 150 characters.")]
    public string Title { get; set; } = "";

    [Range(typeof(decimal), "1", "90", ErrorMessage = "The discount must be between 1% and 90%.")]
    [Display(Name = "Discount (%)")]
    public decimal DiscountPercent { get; set; } = 10;

    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "End date")]
    public DateTime EndDate { get; set; }

    [Display(Name = "Perfumes in the promotion")]
    public List<int> PerfumeIds { get; set; } = new();

    public List<Perfume> AllPerfumes { get; set; } = new();
}

public class SendPromotionViewModel
{
    public Promotion Promotion { get; set; } = new();
    public PromotionAudience Audience { get; set; } = PromotionAudience.AllCustomers;
    public string? AgeGroup { get; set; }
    public string? Gender { get; set; }
}
