using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

internal sealed class ExpressionParser<TEntity>(RelationalTableBase table, UpsertCommandArgs<TEntity> commandArgs)
{
    public PropertyMapping[]? GetUpdateMappings((string ColumnName, bool Nullable)[] joinColumnNames, IReadOnlyCollection<IProperty> excludeProperties)
    {
        if (!commandArgs.NoUpdate)
        {
            return table.Columns
                .Where(column => joinColumnNames.All(c => c.ColumnName != column.ColumnName))
                .Where(column => excludeProperties.All(c => c.GetColumnName() != column.ColumnName))
                .Select(column => new PropertyMapping(column, new PropertyValue(column.Name, false, column)))
                .ToArray();
        }

        return null;
    }

    public PropertyMapping[] ParseUpdateExpression(Expression<Func<TEntity, TEntity, TEntity>> updater)
    {
        if (updater.Body is not MemberInitExpression entityUpdater)
        {
            throw new ArgumentException(Resources.FormatUpdaterMustBeAnInitialiserOfTheTEntityType(nameof(updater)), nameof(updater));
        }

        var visitor = new UpdateExpressionVisitor(table, updater.Parameters[0], updater.Parameters[1], commandArgs.UseExpressionCompiler);
        var result = ParseMemberInitExpression(entityUpdater, visitor).ToArray();
        return result;
    }

    [return: NotNullIfNotNull(nameof(updateCondition))]
    public KnownExpression? ParseUpdateConditionExpression(Expression<Func<TEntity, TEntity, bool>>? updateCondition)
    {
        if (updateCondition is null)
        {
            return null;
        }

        var visitor = new UpdateExpressionVisitor(table, updateCondition.Parameters[0], updateCondition.Parameters[1], commandArgs.UseExpressionCompiler);
        var result = visitor.GetKnownValue(updateCondition.Body);
        if (result is not KnownExpression knownExpression)
        {
            throw new InvalidOperationException(Resources.TheUpdateConditionMustBeAComparisonExpression);
        }

        return knownExpression;
    }

    private IEnumerable<PropertyMapping> ParseMemberInitExpression(MemberInitExpression node, UpdateExpressionVisitor visitor)
    {
        foreach (var binding in node.Bindings.Cast<MemberAssignment>())
        {
            var column = table.FindColumn(binding.Member.Name)
                ?? throw new InvalidOperationException(Resources.FormatUnknownPropertyInExpression(binding.Member.Name, binding.Expression));
            var value = visitor.GetKnownValue(binding.Expression);

            foreach (var mapping in ExpandKnownValue(column, value, binding.Expression))
            {
                yield return mapping;
            }
        }
    }

    /// <summary>
    /// Expand and validate all known values.
    /// </summary>
    private IEnumerable<PropertyMapping> ExpandKnownValue(IColumnBase column, IKnownValue value, Expression expression)
    {
        if (value is BindingValue bindingValue)
        {
            foreach (var mapping in ExpandBindingValue(column, bindingValue.Bindings, expression))
            {
                yield return mapping;
            }
        }
        else if (value is PropertyValue or ConstantValue or KnownExpression)
        {
            if (column.Owned == OwnershipType.InlineOwner &&
                value is PropertyValue { Column: var source, IsLeftParameter: var isLeft } &&
                column == source)
            {
                foreach (var col in table.FindColumnFor(column))
                {
                    yield return new PropertyMapping(col, new PropertyValue(col.Name, isLeft, col));
                }
            }
            else if (column.Owned is OwnershipType.None or OwnershipType.Inline or OwnershipType.Json)
            {
                // Ensure any constants embedded in the value expression inherit the target column,
                // so parameter typing can use the column's relational type mapping.
                yield return new PropertyMapping(column, ApplyColumnToConstants(value, column));
            }
            else
            {
                throw new UnsupportedExpressionException(expression);
            }
        }
        else
        {
            throw new UnsupportedExpressionException(expression);
        }
    }

    /// <summary>
    /// Expands nested member bindings to support owned entities.
    /// </summary>
    private IEnumerable<PropertyMapping> ExpandBindingValue(IColumnBase owner, List<MemberBinding> bindings, Expression expression)
    {
        if (owner.Owned == OwnershipType.InlineOwner)
        {
            foreach (var binding in bindings)
            {
                var column = table.FindColumn(owner, binding.MemberName)
                    ?? throw new InvalidOperationException(Resources.FormatUnknownPropertyInExpression(binding.MemberName, expression));

                foreach (var mapping in ExpandKnownValue(column, binding.Value, binding.Expression))
                {
                    yield return mapping;
                }
            }
        }
        else if (owner.Owned == OwnershipType.Json)
        {
            throw UnsupportedExpressionException.ModifyingJsonMemberNotSupported(expression);
        }
        else
        {
            throw new UnsupportedExpressionException(expression);
        }
    }

    [return: NotNullIfNotNull(nameof(value))]
    private static IKnownValue? ApplyColumnToConstants(IKnownValue? value, IColumnBase column)
    {
        return value switch
        {
            // If a constant value is used as (part of) a value assigned to a column,
            // attach the target column so relational type mapping/value conversion can be applied.
            // This is important for providers that require explicit typing for some CLR types
            // (e.g. UInt64 parameters in Npgsql).
            ConstantValue { ColumnProperty: null } constant => new ConstantValue(constant.Value, column, constant.MemberInfo),

            KnownExpression known => ApplyColumnToConstants(known, column),

            _ => value,
        };
    }

    private static KnownExpression ApplyColumnToConstants(KnownExpression known, IColumnBase column)
    {
        var value1 = ApplyColumnToConstants(known.Value1, column);
        var value2 = ApplyColumnToConstants(known.Value2, column);
        var value3 = ApplyColumnToConstants(known.Value3, column);

        if (ReferenceEquals(value1, known.Value1) &&
            ReferenceEquals(value2, known.Value2) &&
            ReferenceEquals(value3, known.Value3))
        {
            return known;
        }

        return (value2, value3) switch
        {
            (null, null) => new KnownExpression(known.ExpressionType, value1),
            ({}, null) => new KnownExpression(known.ExpressionType, value1, value2),
            ({}, {}) => new KnownExpression(known.ExpressionType, value1, value2, value3),
            _ => throw new InvalidOperationException("Invalid KnownExpression value state"),
        };
    }
}
