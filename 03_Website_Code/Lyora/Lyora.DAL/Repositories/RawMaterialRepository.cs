using System.Data;
using Lyora.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace Lyora.DAL.Repositories;

/// <summary>Raw materials and their stock movements (Manage Raw Materials &amp; Manage Inventory).</summary>
public class RawMaterialRepository
{
    private readonly Db _db;

    private const string SelectMaterial =
        "SELECT MaterialId, Name, Unit, QuantityInStock, Threshold, CostPerUnit, IsAllergen FROM dbo.RawMaterials";

    public RawMaterialRepository(Db db)
    {
        _db = db;
    }

    /// <summary>All raw materials, the ones below their threshold first.</summary>
    public List<RawMaterial> GetAll()
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(SelectMaterial + @"
        ORDER BY CASE WHEN QuantityInStock < Threshold THEN 0 ELSE 1 END, Name;", connection);
        return ReadMaterials(command);
    }

    public RawMaterial? GetById(int materialId)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(SelectMaterial + " WHERE MaterialId = @MaterialId;", connection);
        command.Parameters.AddWithValue("@MaterialId", materialId);
        return ReadMaterials(command).FirstOrDefault();
    }

    public bool NameExists(string name, int? exceptMaterialId = null)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.RawMaterials WHERE Name = @Name AND (@Except IS NULL OR MaterialId <> @Except);", connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@Except", (object?)exceptMaterialId ?? DBNull.Value);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    /// <summary>A new raw material starts with 0 in stock; stock is only added through Restock (so it is always logged).</summary>
    public void Create(RawMaterial material)
    {
        const string sql = @"
        INSERT INTO dbo.RawMaterials (Name, Unit, QuantityInStock, Threshold, CostPerUnit, IsAllergen)
        VALUES (@Name, @Unit, 0, @Threshold, @CostPerUnit, @IsAllergen);";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        AddParameters(command, material);
        command.ExecuteNonQuery();
    }

    /// <summary>Changes the details only; the stock quantity cannot be edited directly.</summary>
    public void Update(RawMaterial material)
    {
        const string sql = @"
        UPDATE dbo.RawMaterials
        SET Name = @Name, Unit = @Unit, Threshold = @Threshold, CostPerUnit = @CostPerUnit, IsAllergen = @IsAllergen
        WHERE MaterialId = @MaterialId;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        AddParameters(command, material);
        command.Parameters.AddWithValue("@MaterialId", material.MaterialId);
        command.ExecuteNonQuery();
    }

    /// <summary>Adds the received quantity with sp_RestockRawMaterial (also logs a Restock transaction).</summary>
    public void Restock(int materialId, decimal quantity)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand("dbo.sp_RestockRawMaterial", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@MaterialId", materialId);
        command.Parameters.Add("@Quantity", SqlDbType.Decimal).Value = quantity;
        command.Parameters["@Quantity"].Precision = 12;
        command.Parameters["@Quantity"].Scale = 2;
        command.ExecuteNonQuery();
    }

    /// <summary>The latest stock movements of a raw material.</summary>
    public List<InventoryTransaction> GetTransactions(int materialId, int count = 50)
    {
        const string sql = @"
        SELECT TOP (@Count) TransactionId, MaterialId, OrderId, Type, QuantityChange, TransactionDate
        FROM dbo.InventoryTransactions
        WHERE MaterialId = @MaterialId
        ORDER BY TransactionDate DESC, TransactionId DESC;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@MaterialId", materialId);
        command.Parameters.AddWithValue("@Count", count);

        var list = new List<InventoryTransaction>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new InventoryTransaction
            {
                TransactionId = reader.GetInt32(0),
                MaterialId = reader.GetInt32(1),
                OrderId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Type = Enum.Parse<TransactionType>(reader.GetString(3)),
                QuantityChange = reader.GetDecimal(4),
                TransactionDate = reader.GetDateTime(5)
            });
        }
        return list;
    }

    private static void AddParameters(SqlCommand command, RawMaterial material)
    {
        command.Parameters.AddWithValue("@Name", material.Name);
        command.Parameters.AddWithValue("@Unit", material.Unit);
        command.Parameters.AddWithValue("@Threshold", material.Threshold);
        command.Parameters.AddWithValue("@CostPerUnit", material.CostPerUnit);
        command.Parameters.AddWithValue("@IsAllergen", material.IsAllergen);
    }

    private static List<RawMaterial> ReadMaterials(SqlCommand command)
    {
        var list = new List<RawMaterial>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new RawMaterial
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
        return list;
    }
}
