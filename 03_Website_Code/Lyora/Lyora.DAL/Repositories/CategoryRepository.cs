using Lyora.DAL.Entities;

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
}
