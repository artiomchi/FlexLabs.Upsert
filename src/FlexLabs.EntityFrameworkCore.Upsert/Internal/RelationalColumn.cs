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
        var hasDefaultValue = Property.TryGetDefaultValue(out var defaultValue) && defaultValue != null;
        var defaultValueSql = Property.GetDefaultValueSql();
        var hasConfiguredDefault = hasDefaultValue || defaultValueSql != null;
        if (rawValue == null || (hasConfiguredDefault && Equals(rawValue, Property.Sentinel)))
        {
            if (hasDefaultValue)
                rawValue = defaultValue;
            else
                defaultSql = defaultValueSql;
        }

        var value = new ConstantValue(rawValue, this);
        var allowInserts = Property.ValueGenerated == ValueGenerated.Never || Property.GetAfterSaveBehavior() == PropertySaveBehavior.Save;

        return (ColumnName, value, defaultSql, allowInserts);
    }
}
