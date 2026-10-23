using System.Data;
using Microsoft.Data.SqlClient;

namespace TeamHub.Infrastructure.Database;

public sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}