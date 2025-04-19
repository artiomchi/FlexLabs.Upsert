using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;


namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

internal record struct PropertyMapping(
    IColumnBase Property,
    IKnownValue Value
);

internal sealed class ExpressionParser<TEntity>(RelationalTableBase table, RunnerQueryOptions queryOptions) {
    public IEnumerable<PropertyMapping> ParseUpdaterExpression(Expression<Func<TEntity, TEntity, TEntity>> updater)
    {
        if (updater.Body is not MemberInitExpression entityUpdater) {
            throw new ArgumentException(Resources.FormatUpdaterMustBeAnInitialiserOfTheTEntityType(nameof(updater)), nameof(updater));
        }

        foreach (var binding in entityUpdater.Bindings.Cast<MemberAssignment>()) {
            var property = table.FindColumn(binding.Member.Name);
            if (property == null) {
                throw new InvalidOperationException($"Unknown property {binding.Member.Name}");
            }

            if (property.Owned == Owned.InlineOwner && binding.Expression is MemberInitExpression navigationUpdater) {
                foreach (var mapping in ParseSubExpression(updater, property, navigationUpdater)) {
                    yield return mapping;
                }
            }
            else {
                var value = binding.Expression.GetValue<TEntity>(updater, table.FindColumn, queryOptions.UseExpressionCompiler);

                if (value is not IKnownValue knownVal) {
                    knownVal = new ConstantValue(value, property);
                }

                yield return (new PropertyMapping(property, knownVal));
            }
        }
    }


    private IEnumerable<PropertyMapping> ParseSubExpression(
        Expression<Func<TEntity, TEntity, TEntity>> updater,
        IColumnBase owner,
        MemberInitExpression navigationUpdater
    )
    {
        foreach (var binding in navigationUpdater.Bindings.Cast<MemberAssignment>()) {
            var property = table.FindColumn(owner, binding.Member.Name);
            if (property == null) {
                throw new InvalidOperationException($"Unknown property {binding.Member.Name}");
            }

            // TODO: Support navigation property expressions! (currently only allows direct values)

            var value = binding.Expression.GetValue<TEntity>(updater, ColumnFinder, queryOptions.UseExpressionCompiler);

            if (value is not IKnownValue knownVal) {
                knownVal = new ConstantValue(value, owner); // BUG?? owner should be property...
            }

            yield return new PropertyMapping(property, knownVal);
        }

        yield break;

        IColumnBase? ColumnFinder(string name) => table.FindColumn(owner, name);
    }
}
