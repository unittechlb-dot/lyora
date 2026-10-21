using Microsoft.Data.SqlClient;

namespace Lyora.DAL;

/// <summary>
/// Opens connections to the LyoraDB SQL Server database.
/// The connection string comes from appsettings.json (key: ConnectionStrings:LyoraDB).
/// </summary>
public class Db
{
    private readonly string _connectionString;

    public Db(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection Open()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
