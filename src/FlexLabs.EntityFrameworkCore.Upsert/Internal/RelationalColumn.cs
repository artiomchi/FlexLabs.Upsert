using System.Diagnostics.CodeAnalysis;
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

    private bool IsNull([NotNullWhen(false)] object? entity)
    {
        if (entity == null) return true;

        var complexProperties = new Stack<IComplexProperty>();

        // Need to collect root to leaf.
        // With Parent -> Child -> SubChild you need to check Parent first, then Child, then SubChild.
        for (var declaringType = Property.DeclaringType; declaringType is IComplexType complexType; declaringType = complexType.ComplexProperty.DeclaringType)
        {
            complexProperties.Push(complexType.ComplexProperty);
        }

        while (complexProperties.TryPop(out var complexProperty))
        {
            entity = complexProperty.GetGetter().GetClrValue(entity);

            if (entity == null)
                return true;
        }

        return false;
    }

    public (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var obj = entity;
        if (EntityGetter is not null)
        {
            obj = EntityGetter(entity);
        }

        bool isAbsent = IsNull(obj);
        var rawValue = isAbsent ? null
            : Property.GetGetter().GetClrValueUsingContainingEntity(obj!);

        string? defaultSql = null;
        var hasDefaultValue = Property.TryGetUsableDefaultValue(out var defaultValue);
        var defaultValueSql = Property.GetDefaultValueSql();
        var hasConfiguredDefault = hasDefaultValue || defaultValueSql != null;
        if (rawValue == null || (hasConfiguredDefault && Property.IsSentinel(rawValue)))
        {
            if (hasDefaultValue && !isAbsent)
                rawValue = defaultValue;
            else
                defaultSql = defaultValueSql;
        }

        var value = new ConstantValue(rawValue, this);
        var allowInserts = Property.ValueGenerated == ValueGenerated.Never || Property.GetAfterSaveBehavior() == PropertySaveBehavior.Save;

        return (ColumnName, value, defaultSql, allowInserts);
    }
}
