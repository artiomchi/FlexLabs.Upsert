using System;
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
        var jsonValue = rawValue != null
            ? Column.StoreTypeMapping.GenerateProviderValueSqlLiteral(rawValue).Trim('\'')
            : null;

        var value = new ConstantValue(jsonValue, this);

        return (ColumnName, value, DefaultSql: null, AllowInserts: true);
    }
}
