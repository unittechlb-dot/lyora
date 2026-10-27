namespace Lyora.DAL.Entities;

public class Perfume
{
    public int PerfumeId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    /// <summary>List price (without promotion).</summary>
    public decimal Price { get; set; }

    /// <summary>Discount of the active promotion today (0 = none) and the price the customer pays.</summary>
    public decimal DiscountPercent { get; set; }
    public decimal FinalPrice { get; set; }
    public bool HasDiscount => DiscountPercent > 0;
    public int SizeMl { get; set; }
    public ScentIntensity Intensity { get; set; }
    public ScentStyle Style { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// How many bottles can still be produced from the raw-material stock
    /// (calculated by the view vw_PerfumeAvailability from the BOM).
    /// </summary>
    public int ProducibleBottles { get; set; }

    public bool IsAvailable => ProducibleBottles > 0;
}
