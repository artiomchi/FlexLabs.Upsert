using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;


namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

internal record struct PropertyMapping(
    IColumnBase Property,
    IKnownValue Value
);

internal sealed class ExpressionParser<TEntity>(RelationalTableBase table, RunnerQueryOptions queryOptions) {
    public PropertyMapping[]? ParseUpdateExpression(Expression<Func<TEntity, TEntity, TEntity>>? updater, (string ColumnName, bool Nullable)[] joinColumnNames)
    {
        if (updater is not null) {
            return ParseUpdateExpression(updater);
        }

        if (!queryOptions.NoUpdate) {
            return table.Columns
                .Where(column => joinColumnNames.All(c => c.ColumnName != column.ColumnName))
                .Select(column => new PropertyMapping(
                    Property: column,
                    Value: new PropertyValue(column.Name, false, column)
                ))
                .ToArray();
        }

        return null;
    }

    public PropertyMapping[] ParseUpdateExpression(Expression<Func<TEntity, TEntity, TEntity>> updater)
    {
        if (updater.Body is not MemberInitExpression entityUpdater) {
            throw new ArgumentException(Resources.FormatUpdaterMustBeAnInitialiserOfTheTEntityType(nameof(updater)), nameof(updater));
        }

        var visitor = new UpdateExpressionVisitor(table, updater.Parameters[0], updater.Parameters[1], queryOptions.UseExpressionCompiler);
        var result = ParseMemberInitExpression(entityUpdater, visitor).ToArray();
        return result;
    }


    [return: NotNullIfNotNull(nameof(updateCondition))]
    public KnownExpression? ParseUpdateConditionExpression(Expression<Func<TEntity, TEntity, bool>>? updateCondition)
    {
        if (updateCondition is null) {
            return null;
        }

        var visitor = new UpdateExpressionVisitor(table, updateCondition.Parameters[0], updateCondition.Parameters[1], queryOptions.UseExpressionCompiler);
        var result = visitor.GetKnownValue(updateCondition.Body);

        if (result is not KnownExpression knownExpression) {
            throw new InvalidOperationException(Resources.TheUpdateConditionMustBeAComparisonExpression);
        }

        return knownExpression;
    }


    private IEnumerable<PropertyMapping> ParseMemberInitExpression(MemberInitExpression node, UpdateExpressionVisitor visitor)
    {
        foreach (var binding in node.Bindings.Cast<MemberAssignment>()) {
            var column = table.FindColumn(binding.Member.Name) ?? throw UnknownPropertyInExpressionException(binding.Member.Name, binding.Expression);
            var value = visitor.GetKnownValue(binding.Expression);

            foreach (var mapping in ExpandKnownValue(column, value, binding.Expression)) {
                yield return mapping;
            }
        }
    }

    /// <summary>
    /// Expand and validate all known values.
    /// </summary>
    private IEnumerable<PropertyMapping> ExpandKnownValue(IColumnBase column, IKnownValue value, Expression expression)
    {
        if (value is BindingValue bindingValue) {
            foreach (var mapping in ExpandBindingValue(column, bindingValue.Bindings, expression)) {
                yield return mapping;
            }
        }
        else if (value is PropertyValue or ConstantValue or KnownExpression) {
            yield return new PropertyMapping(column, value);
        }
        else {
            throw new UnsupportedExpressionException(expression);
        }
    }

    /// <summary>
    /// Expands nested member bindings to support owned entities.
    /// </summary>
    private IEnumerable<PropertyMapping> ExpandBindingValue(IColumnBase owner, List<MemberBinding> bindings, Expression expression)
    {
        if (owner.Owned == Owned.InlineOwner) {
            foreach (var binding in bindings) {
                var column = table.FindColumn(owner, binding.MemberName) ?? throw UnknownPropertyInExpressionException(binding.MemberName, expression);

                foreach (var mapping in ExpandKnownValue(column, binding.Value, binding.Expression)) {
                    yield return mapping;
                }
            }
        }
        else if (owner.Owned == Owned.Json) {
            throw UnsupportedExpressionException.JsonMemberBinding(expression);
        }
        else {
            throw new UnsupportedExpressionException(expression);
        }
    }


    private static InvalidOperationException UnknownPropertyInExpressionException(string propertyName, Expression expression)
    {
        return new InvalidOperationException($"Unknown property {propertyName} in expression {expression}");
    }
}
