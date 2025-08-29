using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

internal sealed class ExpressionParser<TEntity>(RelationalTableBase table, RunnerQueryOptions queryOptions)
{
    public PropertyMapping[]? GetUpdateMappings((string ColumnName, bool Nullable)[] joinColumnNames)
    {
        if (!queryOptions.NoUpdate)
        {
            return table.Columns
                .Where(column => joinColumnNames.All(c => c.ColumnName != column.ColumnName))
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

        var visitor = new UpdateExpressionVisitor(table, updater.Parameters[0], updater.Parameters[1], queryOptions.UseExpressionCompiler);
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

        var visitor = new UpdateExpressionVisitor(table, updateCondition.Parameters[0], updateCondition.Parameters[1], queryOptions.UseExpressionCompiler);
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
                yield return new PropertyMapping(column, value);
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
}
