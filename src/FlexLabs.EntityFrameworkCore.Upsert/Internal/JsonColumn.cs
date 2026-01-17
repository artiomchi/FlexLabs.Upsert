using FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

internal sealed record JsonColumn(
    IColumn Column,
    INavigation Navigation,
    string Name,
    string ColumnName,
    OwnershipType Owned,
    string? Path
) : IColumnBase
{
    public (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var rawValue = Navigation.GetGetter().GetClrValueUsingContainingEntity(entity);

        // SQL Server and SQLite expect JSON values to be already serialized.
        // So we need to serialize them here with the same logic EF Core uses.
        // Since EF Core 10 removed the serialization in the internals we used before,
        // we are now forced to rebuild the serialization logic here in our `RelationalJsonHelper`.
        var jsonValue = RelationalJsonHelper.SerializeToJson(Navigation, rawValue);

        var value = new ConstantValue(jsonValue, this);

        return (ColumnName, value, DefaultSql: null, AllowInserts: true);
    }
}
