namespace Lyora.DAL.Entities;

public class Promotion
{
    public int PromotionId { get; set; }
    public string Title { get; set; } = "";
    public decimal DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public List<int> PerfumeIds { get; set; } = new();
    public List<string> PerfumeNames { get; set; } = new();

    /// <summary>A promotion is active from the start date to the end date (both included).</summary>
    public bool IsActive => DateTime.Today >= StartDate.Date && DateTime.Today <= EndDate.Date;
    public bool IsUpcoming => DateTime.Today < StartDate.Date;
}
