namespace Lyora.DAL.Entities;

public class RawMaterial
{
    public int MaterialId { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal QuantityInStock { get; set; }
    public decimal Threshold { get; set; }
    public decimal CostPerUnit { get; set; }
    public bool IsAllergen { get; set; }

    public bool IsBelowThreshold => QuantityInStock < Threshold;
}
