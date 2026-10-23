using Lyora.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace Lyora.DAL.Repositories;

public class CategoryRepository
{
    private readonly Db _db;

    public CategoryRepository(Db db)
    {
        _db = db;
    }

    public List<Category> GetAll()
    {
        const string sql = "SELECT CategoryId, Name, Description FROM dbo.Categories ORDER BY Name;";

        var categories = new List<Category>();
        using var connection = _db.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            categories.Add(new Category
            {
                CategoryId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }
        return categories;
    }

    public bool NameExists(string name, int? exceptCategoryId = null)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Categories WHERE Name = @Name AND (@Except IS NULL OR CategoryId <> @Except);", connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@Except", (object?)exceptCategoryId ?? DBNull.Value);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public void Create(Category category)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand("INSERT INTO dbo.Categories (Name, Description) VALUES (@Name, @Description);", connection);
        command.Parameters.AddWithValue("@Name", category.Name);
        command.Parameters.AddWithValue("@Description", (object?)category.Description ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public void Update(Category category)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(
            "UPDATE dbo.Categories SET Name = @Name, Description = @Description WHERE CategoryId = @CategoryId;", connection);
        command.Parameters.AddWithValue("@CategoryId", category.CategoryId);
        command.Parameters.AddWithValue("@Name", category.Name);
        command.Parameters.AddWithValue("@Description", (object?)category.Description ?? DBNull.Value);
        command.ExecuteNonQuery();
    }
}
