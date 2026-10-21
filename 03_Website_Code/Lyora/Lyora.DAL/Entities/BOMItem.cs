namespace Lyora.DAL.Entities;

/// <summary>One line of a perfume formula: a raw material and its quantity for one bottle.</summary>
public class BOMItem
{
    public int BomItemId { get; set; }
    public int PerfumeId { get; set; }
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal QuantityPerBottle { get; set; }
    public bool IsAllergen { get; set; }
}
