namespace Lyora.DAL.Entities;

/// <summary>Everything shown on the administrator's Analytics page.</summary>
public class AnalyticsReport
{
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
    public int Bottles { get; set; }
    public int BuyingCustomers { get; set; }
    public decimal AverageOrder => Orders == 0 ? 0 : Math.Round(Revenue / Orders, 2);

    public List<MonthlySales> SalesByMonth { get; set; } = new();
    public List<PerfumeSales> BestSellers { get; set; } = new();
    public List<CustomerSegment> AgeGroups { get; set; } = new();
    public List<CustomerSegment> Genders { get; set; } = new();
    public List<MaterialUsage> RawMaterialsUsed { get; set; } = new();
}

public class MonthlySales
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Orders { get; set; }
    public int Bottles { get; set; }
    public decimal Revenue { get; set; }
}

public class PerfumeSales
{
    public int PerfumeId { get; set; }
    public string Name { get; set; } = "";
    public int BottlesSold { get; set; }
    public decimal Revenue { get; set; }
}

public class CustomerSegment
{
    public string Segment { get; set; } = "";
    public int Customers { get; set; }
    public int Orders { get; set; }
}

public class MaterialUsage
{
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal QuantityUsed { get; set; }
}
