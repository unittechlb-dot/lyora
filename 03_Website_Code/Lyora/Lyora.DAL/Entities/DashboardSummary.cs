namespace Lyora.DAL.Entities;

/// <summary>Numbers shown on the administrator dashboard.</summary>
public class DashboardSummary
{
    public int ActivePerfumes { get; set; }
    public int Customers { get; set; }
    public int OpenOrders { get; set; }
    public List<RawMaterial> LowStockMaterials { get; set; } = new();
}
