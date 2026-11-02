using System.Data;
using Dapper;

namespace TeamHub.Infrastructure.Database;

/// <summary>
/// Allows Dapper to map SQL Server <c>date</c>/<c>datetime</c> columns (returned as <see cref="DateTime"/>)
/// to <see cref="DateOnly"/> properties, and to send <see cref="DateOnly"/> values back as parameters.
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => value switch
    {
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        DateTimeOffset offset => DateOnly.FromDateTime(offset.DateTime),
        string text when DateOnly.TryParse(text, out var parsed) => parsed,
        DateOnly dateOnly => dateOnly,
        _ => DateOnly.FromDateTime(Convert.ToDateTime(value))
    };

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
}
