using System.Data;
using Npgsql;

namespace HighLoad.Streamline.Shared; // Убедись, что неймспейс такой

public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}