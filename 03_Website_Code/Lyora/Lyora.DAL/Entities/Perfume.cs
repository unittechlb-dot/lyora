namespace Lyora.DAL.Entities;

public class Perfume
{
    public int PerfumeId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
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
