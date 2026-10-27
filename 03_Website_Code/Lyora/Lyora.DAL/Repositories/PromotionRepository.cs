using System.Data;
using Lyora.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace Lyora.DAL.Repositories;

public class PromotionRepository
{
    private readonly Db _db;

    public PromotionRepository(Db db)
    {
        _db = db;
    }

    /// <summary>All promotions (newest first) with the perfumes they apply to.</summary>
    public List<Promotion> GetAll() => Read(null);

    public Promotion? GetById(int promotionId) => Read(promotionId).FirstOrDefault();

    public int Create(Promotion promotion)
    {
        using var connection = _db.Open();
        using var transaction = connection.BeginTransaction();
        using var command = new SqlCommand(@"
            INSERT INTO dbo.Promotions (Title, DiscountPercent, StartDate, EndDate)
            VALUES (@Title, @Discount, @Start, @End);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction);
        AddParameters(command, promotion);
        promotion.PromotionId = Convert.ToInt32(command.ExecuteScalar());

        SavePerfumes(connection, transaction, promotion);
        transaction.Commit();
        return promotion.PromotionId;
    }

    public void Update(Promotion promotion)
    {
        using var connection = _db.Open();
        using var transaction = connection.BeginTransaction();
        using var command = new SqlCommand(@"
            UPDATE dbo.Promotions
            SET Title = @Title, DiscountPercent = @Discount, StartDate = @Start, EndDate = @End
            WHERE PromotionId = @PromotionId;", connection, transaction);
        AddParameters(command, promotion);
        command.Parameters.AddWithValue("@PromotionId", promotion.PromotionId);
        command.ExecuteNonQuery();

        SavePerfumes(connection, transaction, promotion);
        transaction.Commit();
    }

    /// <summary>Deletes a promotion (its perfume links are deleted by ON DELETE CASCADE; old orders keep their price).</summary>
    public void Delete(int promotionId)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand("DELETE FROM dbo.Promotions WHERE PromotionId = @PromotionId;", connection);
        command.Parameters.AddWithValue("@PromotionId", promotionId);
        command.ExecuteNonQuery();
    }

    private static void SavePerfumes(SqlConnection connection, SqlTransaction transaction, Promotion promotion)
    {
        using (var delete = new SqlCommand("DELETE FROM dbo.PromotionPerfumes WHERE PromotionId = @PromotionId;", connection, transaction))
        {
            delete.Parameters.AddWithValue("@PromotionId", promotion.PromotionId);
            delete.ExecuteNonQuery();
        }
        foreach (var perfumeId in promotion.PerfumeIds.Distinct())
        {
            using var insert = new SqlCommand(
                "INSERT INTO dbo.PromotionPerfumes (PromotionId, PerfumeId) VALUES (@PromotionId, @PerfumeId);", connection, transaction);
            insert.Parameters.AddWithValue("@PromotionId", promotion.PromotionId);
            insert.Parameters.AddWithValue("@PerfumeId", perfumeId);
            insert.ExecuteNonQuery();
        }
    }

    private static void AddParameters(SqlCommand command, Promotion promotion)
    {
        command.Parameters.AddWithValue("@Title", promotion.Title);
        command.Parameters.AddWithValue("@Discount", promotion.DiscountPercent);
        command.Parameters.Add("@Start", SqlDbType.Date).Value = promotion.StartDate.Date;
        command.Parameters.Add("@End", SqlDbType.Date).Value = promotion.EndDate.Date;
    }

    private List<Promotion> Read(int? promotionId)
    {
        const string sql = @"
        SELECT PromotionId, Title, DiscountPercent, StartDate, EndDate
        FROM dbo.Promotions
        WHERE @PromotionId IS NULL OR PromotionId = @PromotionId
        ORDER BY StartDate DESC;

        SELECT pp.PromotionId, p.PerfumeId, p.Name
        FROM dbo.PromotionPerfumes pp
        JOIN dbo.Perfumes p ON p.PerfumeId = pp.PerfumeId
        WHERE @PromotionId IS NULL OR pp.PromotionId = @PromotionId
        ORDER BY p.Name;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PromotionId", (object?)promotionId ?? DBNull.Value);
        using var reader = command.ExecuteReader();

        var list = new List<Promotion>();
        while (reader.Read())
        {
            list.Add(new Promotion
            {
                PromotionId = reader.GetInt32(0),
                Title = reader.GetString(1),
                DiscountPercent = reader.GetDecimal(2),
                StartDate = reader.GetDateTime(3),
                EndDate = reader.GetDateTime(4)
            });
        }
        reader.NextResult();
        while (reader.Read())
        {
            var promotion = list.FirstOrDefault(p => p.PromotionId == reader.GetInt32(0));
            promotion?.PerfumeIds.Add(reader.GetInt32(1));
            promotion?.PerfumeNames.Add(reader.GetString(2));
        }
        return list;
    }
}
