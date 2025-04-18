using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;


namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

internal class ExpressionParser {
    public static List<(IColumnBase Property, IKnownValue Value)> ParseUpdaterExpression<TEntity>(RelationalTable table, Expression<Func<TEntity, TEntity, TEntity>> updater, RunnerQueryOptions queryOptions)
    {
        if (updater.Body is not MemberInitExpression entityUpdater) {
            throw new ArgumentException(Resources.FormatUpdaterMustBeAnInitialiserOfTheTEntityType(nameof(updater)), nameof(updater));
        }

        List<(IColumnBase Property, IKnownValue Value)> updateExpressions = [];

        foreach (var binding in entityUpdater.Bindings.Cast<MemberAssignment>()) {
            var property = table.FindColumn(binding.Member.Name);
            if (property == null) {
                throw new InvalidOperationException("Unknown property " + binding.Member.Name);
            }

            if (property.Owned == Owned.InlineOwner && binding.Expression is MemberInitExpression navigationUpdater) {
                foreach (var navigationBinding in navigationUpdater.Bindings.Cast<MemberAssignment>()) {
                    var navigationProperty = table.FindColumn(property, navigationBinding.Member.Name);
                    if (navigationProperty == null) {
                        throw new InvalidOperationException("Unknown navigation-property " + binding.Member.Name);
                    }

                    // TODO: Support navigation property expressions! (currently only allows direct values)
                    var columnFinder = (string name) => table.FindColumn(property, name);
                    var navigationValue = navigationBinding.Expression.GetValue<TEntity>(updater, columnFinder, queryOptions.UseExpressionCompiler);
                    if (navigationValue is not IKnownValue knownNavigationVal)
                        knownNavigationVal = new ConstantValue(navigationValue, property);

                    updateExpressions.Add((navigationProperty, knownNavigationVal));
                }
            }
            else {
                var value = binding.Expression.GetValue<TEntity>(updater, table.FindColumn, queryOptions.UseExpressionCompiler);
                if (value is not IKnownValue knownVal)
                    knownVal = new ConstantValue(value, property);
                updateExpressions.Add((property, knownVal));
            }
        }

        return updateExpressions;
    }
}
