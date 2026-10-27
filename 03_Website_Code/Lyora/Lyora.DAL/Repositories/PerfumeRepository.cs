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
               a.ProducibleBottles, pr.DiscountPercent, pr.FinalPrice
        FROM dbo.Perfumes p
        JOIN dbo.Categories c              ON c.CategoryId = p.CategoryId
        JOIN dbo.vw_PerfumeAvailability a  ON a.PerfumeId = p.PerfumeId
        JOIN dbo.vw_PerfumePrices pr       ON pr.PerfumeId = p.PerfumeId";

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

    // ---------- Recommendation data ----------

    /// <summary>For every active perfume: the raw materials of its formula (BOM).</summary>
    public Dictionary<int, HashSet<int>> GetBomMaterialsOfActivePerfumes()
    {
        const string sql = @"
        SELECT b.PerfumeId, b.MaterialId
        FROM dbo.BOMItems b
        JOIN dbo.Perfumes p ON p.PerfumeId = b.PerfumeId
        WHERE p.IsActive = 1;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();

        var result = new Dictionary<int, HashSet<int>>();
        while (reader.Read())
        {
            var perfumeId = reader.GetInt32(0);
            if (!result.TryGetValue(perfumeId, out var materials))
            {
                result[perfumeId] = materials = new HashSet<int>();
            }
            materials.Add(reader.GetInt32(1));
        }
        return result;
    }

    /// <summary>Bottles sold per perfume (orders that were not cancelled).</summary>
    public Dictionary<int, int> GetBottlesSold()
    {
        using var connection = _db.Open();
        using var command = new SqlCommand("SELECT PerfumeId, BottlesSold FROM dbo.vw_BestSellingPerfumes;", connection);
        using var reader = command.ExecuteReader();

        var result = new Dictionary<int, int>();
        while (reader.Read())
        {
            result[reader.GetInt32(0)] = reader.GetInt32(1);
        }
        return result;
    }

    // ---------- Administrator (Manage Products & Manage BOM) ----------

    /// <summary>All perfumes, including hidden ones.</summary>
    public List<Perfume> GetAllForAdmin()
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(SelectPerfume + " ORDER BY p.IsActive DESC, p.Name;", connection);
        return ReadPerfumes(command);
    }

    public bool NameExists(string name, int? exceptPerfumeId = null)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Perfumes WHERE Name = @Name AND (@Except IS NULL OR PerfumeId <> @Except);", connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@Except", (object?)exceptPerfumeId ?? DBNull.Value);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public int Create(Perfume perfume)
    {
        const string sql = @"
        INSERT INTO dbo.Perfumes (CategoryId, Name, Description, Price, SizeMl, Intensity, Style, ImageUrl, IsActive)
        VALUES (@CategoryId, @Name, @Description, @Price, @SizeMl, @Intensity, @Style, @ImageUrl, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        AddPerfumeParameters(command, perfume);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Update(Perfume perfume)
    {
        const string sql = @"
        UPDATE dbo.Perfumes
        SET CategoryId = @CategoryId, Name = @Name, Description = @Description, Price = @Price, SizeMl = @SizeMl,
            Intensity = @Intensity, Style = @Style, ImageUrl = @ImageUrl, IsActive = @IsActive
        WHERE PerfumeId = @PerfumeId;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        AddPerfumeParameters(command, perfume);
        command.Parameters.AddWithValue("@PerfumeId", perfume.PerfumeId);
        command.ExecuteNonQuery();
    }

    /// <summary>Adds a raw material to the formula, or changes its quantity if it is already there.</summary>
    public void SaveBomLine(int perfumeId, int materialId, decimal quantityPerBottle)
    {
        const string sql = @"
        UPDATE dbo.BOMItems SET QuantityPerBottle = @Quantity
        WHERE PerfumeId = @PerfumeId AND MaterialId = @MaterialId;

        IF @@ROWCOUNT = 0
            INSERT INTO dbo.BOMItems (PerfumeId, MaterialId, QuantityPerBottle) VALUES (@PerfumeId, @MaterialId, @Quantity);";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PerfumeId", perfumeId);
        command.Parameters.AddWithValue("@MaterialId", materialId);
        command.Parameters.AddWithValue("@Quantity", quantityPerBottle);
        command.ExecuteNonQuery();
    }

    public void RemoveBomLine(int perfumeId, int bomItemId)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand("DELETE FROM dbo.BOMItems WHERE BomItemId = @BomItemId AND PerfumeId = @PerfumeId;", connection);
        command.Parameters.AddWithValue("@PerfumeId", perfumeId);
        command.Parameters.AddWithValue("@BomItemId", bomItemId);
        command.ExecuteNonQuery();
    }

    private static void AddPerfumeParameters(SqlCommand command, Perfume perfume)
    {
        command.Parameters.AddWithValue("@CategoryId", perfume.CategoryId);
        command.Parameters.AddWithValue("@Name", perfume.Name);
        command.Parameters.AddWithValue("@Description", (object?)perfume.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@Price", perfume.Price);
        command.Parameters.AddWithValue("@SizeMl", perfume.SizeMl);
        command.Parameters.AddWithValue("@Intensity", perfume.Intensity.ToString());
        command.Parameters.AddWithValue("@Style", perfume.Style.ToString());
        command.Parameters.AddWithValue("@ImageUrl", (object?)perfume.ImageUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", perfume.IsActive);
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
                ProducibleBottles = reader.GetInt32(11),
                DiscountPercent = reader.GetDecimal(12),
                FinalPrice = reader.GetDecimal(13)
            });
        }
        return perfumes;
    }
}
