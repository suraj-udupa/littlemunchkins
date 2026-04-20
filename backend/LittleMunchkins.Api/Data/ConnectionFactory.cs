using Npgsql;
using System.Data;

namespace LittleMunchkins.Api.Data;

public interface IConnectionFactory
{
    IDbConnection Create();
}

public class NpgsqlConnectionFactory(string connectionString) : IConnectionFactory
{
    public IDbConnection Create() => new NpgsqlConnection(connectionString);
}
