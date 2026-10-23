using System.Data;

namespace TeamHub.Infrastructure.Database;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}