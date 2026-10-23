using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.AddressType;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.AddressTypes;

public sealed class AddressTypeRepository(IDbConnectionFactory connectionFactory) : IAddressTypeRepository
{
    public async Task<IReadOnlyList<GetAddressTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT AddressTypeId, AddressTypeName, IsActive FROM AddressType WHERE IsActive = 1 ORDER BY AddressTypeName;";
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<GetAddressTypeDto>(new CommandDefinition(sql, cancellationToken: cancellationToken))).AsList();
    }
    public async Task<GetAddressTypeDto?> GetByIdAsync(int addressTypeId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT AddressTypeId, AddressTypeName, IsActive FROM AddressType WHERE AddressTypeId = @AddressTypeId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<GetAddressTypeDto>(new CommandDefinition(sql, new { AddressTypeId = addressTypeId }, cancellationToken: cancellationToken));
    }
    public async Task<int> CreateAsync(AddressType addressType, CancellationToken cancellationToken = default)
    {
        const string sql = @"INSERT INTO AddressType (AddressTypeName, IsActive, CreatedDate, CreatedBy) OUTPUT INSERTED.AddressTypeId VALUES (@AddressTypeName, @IsActive, @CreatedDate, @CreatedBy);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, addressType, cancellationToken: cancellationToken));
    }
    public async Task<bool> UpdateAsync(AddressType addressType, CancellationToken cancellationToken = default)
    {
        const string sql = @"UPDATE AddressType SET AddressTypeName = @AddressTypeName, IsActive = @IsActive, LastModifiedDate = @LastModifiedDate, LastModifiedBy = @LastModifiedBy WHERE AddressTypeId = @AddressTypeId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, addressType, cancellationToken: cancellationToken)) > 0;
    }
    public async Task<bool> DeleteAsync(int addressTypeId, CancellationToken cancellationToken = default)
    {
        const string sql = @"UPDATE AddressType SET IsActive = 0 WHERE AddressTypeId = @AddressTypeId AND IsActive = 1;";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { AddressTypeId = addressTypeId }, cancellationToken: cancellationToken)) > 0;
    }
}