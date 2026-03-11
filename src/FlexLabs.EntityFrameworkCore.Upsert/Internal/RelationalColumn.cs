using FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

internal sealed record RelationalColumn(
    IProperty Property,
    string ColumnName,
    OwnershipType Owned,
    string? Path = null,
    Func<object, object?>? EntityGetter = null
) : IColumnBase
{
    public string Name => Property.Name;

    public (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var obj = entity;
        if (EntityGetter is not null)
        {
            obj = EntityGetter(entity);
        }

        var rawValue = obj != null
            ? Property.GetGetter().GetClrValueUsingContainingEntity(obj)
            : null;

        string? defaultSql = null;
        if (rawValue == null)
        {
            if (Property.GetDefaultValue() != null)
                rawValue = Property.GetDefaultValue();
            else
                defaultSql = Property.GetDefaultValueSql();
        }

        var value = new ConstantValue(rawValue, this);
        var allowInserts = Property.ValueGenerated == ValueGenerated.Never || Property.GetAfterSaveBehavior() == PropertySaveBehavior.Save;

        return (ColumnName, value, defaultSql, allowInserts);
    }
}
