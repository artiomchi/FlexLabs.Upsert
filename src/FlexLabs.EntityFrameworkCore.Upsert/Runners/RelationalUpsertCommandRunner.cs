﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Base class with common functionality for most relational database runners
    /// </summary>
    public abstract class RelationalUpsertCommandRunner : UpsertCommandRunnerBase
    {
        /// <summary>
        /// Generate a full command for the opsert operation, given the inputs passed
        /// </summary>
        /// <param name="tableName">The name of the database table</param>
        /// <param name="entities">A collection of entity data (column names and values) to be upserted</param>
        /// <param name="joinColumns">The columns used to match existing items in the database</param>
        /// <param name="updateExpressions">The expressions that represent update commands for matched entities</param>
        /// <returns>A fully formed database query</returns>
        public abstract string GenerateCommand(string tableName, ICollection<ICollection<(string ColumnName, ConstantValue Value)>> entities,
            ICollection<string> joinColumns, ICollection<(string ColumnName, KnownExpression Value)> updateExpressions);
        /// <summary>
        /// Escape the name of the table/column/schema in a given database language
        /// </summary>
        /// <param name="name">The name of the entity</param>
        /// <returns>The escaped name of the entity</returns>
        protected abstract string EscapeName(string name);
        /// <summary>
        /// Reference an indexed parameter passed to the query in a given database language
        /// </summary>
        /// <param name="index">The 0 based index of the parameter</param>
        /// <returns>The reference to the parameter</returns>
        protected virtual string Parameter(int index) => "@p" + index;
        /// <summary>
        /// Get the escaped database table schema
        /// </summary>
        /// <param name="entityType">The entity type of the table</param>
        /// <returns>The escaped schema name of the table, followed by a '.'. If the table has no schema - returns null</returns>
        protected virtual string GetSchema(IEntityType entityType)
        {
            var schema = entityType.Relational().Schema;
            return schema != null
                ? EscapeName(schema) + "."
                : null;
        }
        /// <summary>
        /// Get the fully qualified, escaped table name
        /// </summary>
        /// <param name="entityType">The entity type of the table</param>
        /// <returns>The fully qualified and escaped table reference</returns>
        protected virtual string GetTableName(IEntityType entityType) => GetSchema(entityType) + EscapeName(entityType.Relational().TableName);
        /// <summary>
        /// Prefix used to reference source dataset columns
        /// </summary>
        protected abstract string SourcePrefix { get; }
        /// <summary>
        /// Suffix used when referencing source dataset columns
        /// </summary>
        protected virtual string SourceSuffix => null;
        /// <summary>
        /// Prefix used to reference target table columns
        /// </summary>
        protected abstract string TargetPrefix { get; }
        /// <summary>
        /// Suffix used when referencing target table columns
        /// </summary>
        protected virtual string TargetSuffix => null;

        private (string SqlCommand, IEnumerable<object> Arguments) PrepareCommand<TEntity>(IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> match,
            Expression<Func<TEntity, TEntity, TEntity>> updater, Expression<Func<TEntity, object>> excludeFromUpdateExpression, bool noUpdate)
        {
            var joinColumns = ProcessMatchExpression(entityType, match);
            var joinColumnNames = joinColumns.Select(c => c.Relational().ColumnName).ToArray();

            var properties = entityType.GetProperties()
                .Where(p => p.ValueGenerated == ValueGenerated.Never)
                .ToArray();

            List<(IProperty Property, KnownExpression Value)> updateExpressions = null;
            if (updater != null)
            {
                if (!(updater.Body is MemberInitExpression entityUpdater))
                    throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updater));

                updateExpressions = new List<(IProperty Property, KnownExpression Value)>();
                foreach (MemberAssignment binding in entityUpdater.Bindings)
                {
                    var property = entityType.FindProperty(binding.Member.Name);
                    if (property == null)
                        throw new InvalidOperationException("Unknown property " + binding.Member.Name);

                    var value = binding.Expression.GetValue<TEntity>(updater);
                    if (!(value is KnownExpression knownExp))
                        knownExp = new KnownExpression(ExpressionType.Constant, new ConstantValue(value));

                    if (knownExp.Value1 is ParameterProperty epp1)
                        epp1.Property = entityType.FindProperty(epp1.PropertyName);
                    if (knownExp.Value2 is ParameterProperty epp2)
                        epp2.Property = entityType.FindProperty(epp2.PropertyName);
                    updateExpressions.Add((property, knownExp));
                }
            }
            else if (!noUpdate)
            {
                updateExpressions = new List<(IProperty Property, KnownExpression Value)>();
                foreach (var property in properties)
                {
                    if (joinColumnNames.Contains(property.Relational().ColumnName))
                        continue;

                    var propertyAccess = new ParameterProperty(property.Name, false) { Property = property };
                    var updateExpression = new KnownExpression(ExpressionType.MemberAccess, propertyAccess);
                    updateExpressions.Add((property, updateExpression));
                }
            }

            var newEntities = entities
                .Select(e => properties
                    .Select(p =>
                    {
                        var columnName = p.Relational().ColumnName;
                        var value = new ConstantValue(p.PropertyInfo.GetValue(e));
                        return (columnName, value);
                    })
                    .ToArray() as ICollection<(string ColumnName, ConstantValue Value)>)
                .ToArray();

            var arguments = newEntities.SelectMany(e => e.Select(p => p.Value)).ToList();
            if (updateExpressions != null)
                arguments.AddRange(updateExpressions.SelectMany(e => new[] { e.Value.Value1, e.Value.Value2 }).OfType<ConstantValue>());
            int i = 0;
            foreach (var arg in arguments)
                arg.ArgumentIndex = i++;

            var excludeFromUpdateProperties = excludeFromUpdateExpression != null
                ? ProcessMatchExpression(entityType, excludeFromUpdateExpression)
                : new List<IProperty>();

            var columnUpdateExpressions = updateExpressions
                ?.Where(x => !excludeFromUpdateProperties.Contains(x.Property))
                .Select(x => (x.Property.Relational().ColumnName, x.Value)).ToArray();
            var sqlCommand = GenerateCommand(GetTableName(entityType), newEntities, joinColumnNames, columnUpdateExpressions);
            return (sqlCommand, arguments.Select(a => a.Value));
        }

        private string ExpandValue(IKnownValue value)
        {
            switch (value)
            {
                case ParameterProperty prop:
                    var prefix = prop.IsLeftParameter ? TargetPrefix : SourcePrefix;
                    var suffix = prop.IsLeftParameter ? TargetSuffix : SourceSuffix;
                    return prefix + EscapeName(prop.Property.Relational().ColumnName) + suffix;

                case ConstantValue constVal:
                    return Parameter(constVal.ArgumentIndex);

                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Expand a known expression into database syntax
        /// </summary>
        /// <param name="expression">The KnownExpression that has to be converted to database language</param>
        /// <returns>A string containing the expression converted to database language</returns>
        protected virtual string ExpandExpression(KnownExpression expression)
        {
            switch (expression.ExpressionType)
            {
                case ExpressionType.Add:
                case ExpressionType.Divide:
                case ExpressionType.Multiply:
                case ExpressionType.Subtract:
                    var left = ExpandValue(expression.Value1);
                    var right = ExpandValue(expression.Value2);
                    var op = GetSimpleOperator(expression.ExpressionType);
                    return $"{left} {op} {right}";

                case ExpressionType.MemberAccess:
                case ExpressionType.Constant:
                    return ExpandValue(expression.Value1);

                default: throw new NotSupportedException("Don't know how to process operation: " + expression.ExpressionType);
            }
        }

        /// <summary>
        /// Get the symbol used for basic expression operators in the database's syntax
        /// </summary>
        /// <param name="expressionType">Type of the basic expression</param>
        /// <returns>A string containing the operator</returns>
        protected virtual string GetSimpleOperator(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Add: return "+";
                case ExpressionType.Divide: return "/";
                case ExpressionType.Multiply: return "*";
                case ExpressionType.Subtract: return "-";
                default: throw new InvalidOperationException($"{expressionType} is not a simple arithmetic operation");
            }
        }

        /// <inheritdoc/>
        public override void Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>> updateExpression, Expression<Func<TEntity, object>> excludeFromUpdateExpression, bool noUpdate)
        {
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities, matchExpression, updateExpression, excludeFromUpdateExpression, noUpdate);
            dbContext.Database.ExecuteSqlCommand(sqlCommand, arguments);
        }

        /// <inheritdoc/>
        public override Task RunAsync<TEntity>(DbContext dbContext, IEntityType entityType,
            ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>> updateExpression,
            Expression<Func<TEntity, object>> excludeFromUpdateExpression, bool noUpdate,
            CancellationToken cancellationToken)
        {
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities, matchExpression, updateExpression,
                excludeFromUpdateExpression, noUpdate);
            return dbContext.Database.ExecuteSqlCommandAsync(sqlCommand, arguments);
        }
    }
}
