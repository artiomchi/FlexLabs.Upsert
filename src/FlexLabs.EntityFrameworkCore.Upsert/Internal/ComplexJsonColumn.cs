using FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

internal sealed record ComplexJsonColumn(
    IColumn Column,
    IComplexProperty Property,
    string ColumnName,
    OwnershipType Owned,
    string? Path = null
) : IColumnBase
{
    public string Name => Property.Name;

    public (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var rawValue = Property.GetGetter().GetClrValueUsingContainingEntity(entity);

        var jsonValue = RelationalJsonUtilities.SerializeComplexTypeToJson(Property.ComplexType, rawValue, Property.IsCollection);

        var value = new ConstantValue(jsonValue, this);

        return (ColumnName, value, DefaultSql: null, AllowInserts: true);
    }
}
