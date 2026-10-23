using Microsoft.Data.SqlClient;

namespace TeamHub.IntegrationTests;

public class DatabaseConnectionTests
{
    [Fact]
    public async Task TestConnectionAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEAMHUB_TEST_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync();

        Assert.Equal(1, result);
    }
}