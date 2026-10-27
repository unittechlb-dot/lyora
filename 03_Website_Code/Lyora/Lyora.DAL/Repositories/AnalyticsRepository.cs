using Lyora.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace Lyora.DAL.Repositories;

/// <summary>Reads the analytics views (sales, best sellers, customer segments, raw-material usage).</summary>
public class AnalyticsRepository
{
    private readonly Db _db;

    public AnalyticsRepository(Db db)
    {
        _db = db;
    }

    public AnalyticsReport GetReport()
    {
        const string sql = @"
        SELECT ISNULL(SUM(oi.Quantity * oi.UnitPrice), 0), COUNT(DISTINCT o.OrderId),
               ISNULL(SUM(oi.Quantity), 0), COUNT(DISTINCT o.CustomerId)
        FROM dbo.Orders o
        JOIN dbo.OrderItems oi ON oi.OrderId = o.OrderId
        WHERE o.Status <> N'Cancelled';

        SELECT SalesYear, SalesMonth, Orders, BottlesSold, Revenue
        FROM dbo.vw_SalesByMonth
        WHERE DATEFROMPARTS(SalesYear, SalesMonth, 1) >= DATEADD(MONTH, -11, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
        ORDER BY SalesYear, SalesMonth;

        SELECT TOP (10) PerfumeId, Name, BottlesSold, Revenue
        FROM dbo.vw_BestSellingPerfumes
        ORDER BY BottlesSold DESC, Revenue DESC;

        SELECT AgeGroup, Customers, Orders FROM dbo.vw_CustomerAgeSegments ORDER BY AgeGroup;

        SELECT Gender, Customers, Orders FROM dbo.vw_CustomerGenderSegments ORDER BY Customers DESC;

        SELECT Name, Unit, QuantityUsed FROM dbo.vw_RawMaterialConsumption
        WHERE QuantityUsed > 0
        ORDER BY Unit, QuantityUsed DESC;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        var report = new AnalyticsReport();
        if (reader.Read())
        {
            report.Revenue = reader.GetDecimal(0);
            report.Orders = reader.GetInt32(1);
            report.Bottles = reader.GetInt32(2);
            report.BuyingCustomers = reader.GetInt32(3);
        }

        reader.NextResult();
        while (reader.Read())
        {
            report.SalesByMonth.Add(new MonthlySales
            {
                Year = reader.GetInt32(0),
                Month = reader.GetInt32(1),
                Orders = reader.GetInt32(2),
                Bottles = reader.GetInt32(3),
                Revenue = reader.GetDecimal(4)
            });
        }

        reader.NextResult();
        while (reader.Read())
        {
            report.BestSellers.Add(new PerfumeSales
            {
                PerfumeId = reader.GetInt32(0),
                Name = reader.GetString(1),
                BottlesSold = reader.GetInt32(2),
                Revenue = reader.GetDecimal(3)
            });
        }

        reader.NextResult();
        while (reader.Read())
        {
            report.AgeGroups.Add(new CustomerSegment { Segment = reader.GetString(0), Customers = reader.GetInt32(1), Orders = reader.GetInt32(2) });
        }

        reader.NextResult();
        while (reader.Read())
        {
            report.Genders.Add(new CustomerSegment { Segment = reader.GetString(0), Customers = reader.GetInt32(1), Orders = reader.GetInt32(2) });
        }

        reader.NextResult();
        while (reader.Read())
        {
            report.RawMaterialsUsed.Add(new MaterialUsage { Name = reader.GetString(0), Unit = reader.GetString(1), QuantityUsed = reader.GetDecimal(2) });
        }
        return report;
    }
}
