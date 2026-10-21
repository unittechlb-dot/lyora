using Lyora.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace Lyora.DAL.Repositories;

public class PerfumeRepository
{
    private readonly Db _db;

    // Columns shared by every perfume query (Perfumes + category name + producible bottles).
    private const string SelectPerfume = @"
        SELECT p.PerfumeId, p.CategoryId, c.Name AS CategoryName, p.Name, p.Description,
               p.Price, p.SizeMl, p.Intensity, p.Style, p.ImageUrl, p.IsActive,
               a.ProducibleBottles
        FROM dbo.Perfumes p
        JOIN dbo.Categories c              ON c.CategoryId = p.CategoryId
        JOIN dbo.vw_PerfumeAvailability a  ON a.PerfumeId = p.PerfumeId";

    public PerfumeRepository(Db db)
    {
        _db = db;
    }

    /// <summary>Active perfumes; every filter is optional (null = no filter).</summary>
    public List<Perfume> Search(string? name, int? categoryId, ScentIntensity? intensity, ScentStyle? style)
    {
        const string sql = SelectPerfume + @"
        WHERE p.IsActive = 1
          AND (@Name IS NULL OR p.Name LIKE '%' + @Name + '%')
          AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
          AND (@Intensity IS NULL OR p.Intensity = @Intensity)
          AND (@Style IS NULL OR p.Style = @Style)
        ORDER BY p.Name;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Name", (object?)name ?? DBNull.Value);
        command.Parameters.AddWithValue("@CategoryId", (object?)categoryId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Intensity", (object?)intensity?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Style", (object?)style?.ToString() ?? DBNull.Value);
        return ReadPerfumes(command);
    }

    /// <summary>Best-selling active perfumes first (newest first when there are no sales yet).</summary>
    public List<Perfume> GetFeatured(int count)
    {
        const string sql = SelectPerfume + @"
        LEFT JOIN dbo.vw_BestSellingPerfumes bs ON bs.PerfumeId = p.PerfumeId
        WHERE p.IsActive = 1
        ORDER BY ISNULL(bs.BottlesSold, 0) DESC, p.PerfumeId DESC
        OFFSET 0 ROWS FETCH NEXT @Count ROWS ONLY;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Count", count);
        return ReadPerfumes(command);
    }

    public Perfume? GetById(int perfumeId)
    {
        const string sql = SelectPerfume + " WHERE p.PerfumeId = @PerfumeId;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PerfumeId", perfumeId);
        return ReadPerfumes(command).FirstOrDefault();
    }

    /// <summary>The BOM (formula) of a perfume.</summary>
    public List<BOMItem> GetBom(int perfumeId)
    {
        const string sql = @"
        SELECT b.BomItemId, b.PerfumeId, b.MaterialId, r.Name, r.Unit, b.QuantityPerBottle, r.IsAllergen
        FROM dbo.BOMItems b
        JOIN dbo.RawMaterials r ON r.MaterialId = b.MaterialId
        WHERE b.PerfumeId = @PerfumeId
        ORDER BY b.QuantityPerBottle DESC;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PerfumeId", perfumeId);

        var items = new List<BOMItem>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new BOMItem
            {
                BomItemId = reader.GetInt32(0),
                PerfumeId = reader.GetInt32(1),
                MaterialId = reader.GetInt32(2),
                MaterialName = reader.GetString(3),
                Unit = reader.GetString(4),
                QuantityPerBottle = reader.GetDecimal(5),
                IsAllergen = reader.GetBoolean(6)
            });
        }
        return items;
    }

    private static List<Perfume> ReadPerfumes(SqlCommand command)
    {
        var perfumes = new List<Perfume>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            perfumes.Add(new Perfume
            {
                PerfumeId = reader.GetInt32(0),
                CategoryId = reader.GetInt32(1),
                CategoryName = reader.GetString(2),
                Name = reader.GetString(3),
                Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                Price = reader.GetDecimal(5),
                SizeMl = reader.GetInt32(6),
                Intensity = Enum.Parse<ScentIntensity>(reader.GetString(7)),
                Style = Enum.Parse<ScentStyle>(reader.GetString(8)),
                ImageUrl = reader.IsDBNull(9) ? null : reader.GetString(9),
                IsActive = reader.GetBoolean(10),
                ProducibleBottles = reader.GetInt32(11)
            });
        }
        return perfumes;
    }
}
