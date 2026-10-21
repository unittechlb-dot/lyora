using Lyora.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace Lyora.DAL.Repositories;

public class DashboardRepository
{
    private readonly Db _db;

    public DashboardRepository(Db db)
    {
        _db = db;
    }

    public DashboardSummary GetSummary()
    {
        const string sql = @"
        SELECT (SELECT COUNT(*) FROM dbo.Perfumes  WHERE IsActive = 1),
               (SELECT COUNT(*) FROM dbo.Customers),
               (SELECT COUNT(*) FROM dbo.Orders    WHERE Status IN (N'Pending', N'OutForDelivery'));

        SELECT MaterialId, Name, Unit, QuantityInStock, Threshold, CostPerUnit, IsAllergen
        FROM dbo.RawMaterials
        WHERE QuantityInStock < Threshold
        ORDER BY QuantityInStock / NULLIF(Threshold, 0);";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        var summary = new DashboardSummary();
        if (reader.Read())
        {
            summary.ActivePerfumes = reader.GetInt32(0);
            summary.Customers = reader.GetInt32(1);
            summary.OpenOrders = reader.GetInt32(2);
        }

        reader.NextResult();
        while (reader.Read())
        {
            summary.LowStockMaterials.Add(new RawMaterial
            {
                MaterialId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Unit = reader.GetString(2),
                QuantityInStock = reader.GetDecimal(3),
                Threshold = reader.GetDecimal(4),
                CostPerUnit = reader.GetDecimal(5),
                IsAllergen = reader.GetBoolean(6)
            });
        }
        return summary;
    }
}
