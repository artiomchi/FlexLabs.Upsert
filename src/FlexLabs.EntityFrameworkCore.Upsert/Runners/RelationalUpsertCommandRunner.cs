using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

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
        /// <param name="updateCondition">The expression that tests whether existing entities should be updated</param>
        /// <returns>A fully formed database query</returns>
        public abstract string GenerateCommand(string tableName, ICollection<ICollection<(string ColumnName, ConstantValue Value)>> entities,
            ICollection<(string ColumnName, bool IsNullable)> joinColumns, ICollection<(string ColumnName, IKnownValue Value)> updateExpressions,
            KnownExpression updateCondition);
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
        /// Reference an named variable defined by the query runner
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <returns>The reference to the variable</returns>
        protected virtual string Variable(string name) => "@x" + name;
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

        private (string SqlCommand, IEnumerable<ConstantValue> Arguments) PrepareCommand<TEntity>(IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>> match, Expression<Func<TEntity, TEntity, TEntity>> updater, Expression<Func<TEntity, TEntity, bool>> updateCondition,
            bool noUpdate, bool useExpressionCompiler)
        {
            var joinColumns = ProcessMatchExpression(entityType, match);
            var joinColumnNames = joinColumns.Select(c => (c.Relational().ColumnName, c.IsColumnNullable())).ToArray();

            var properties = entityType.GetProperties()
                .Where(p => p.ValueGenerated == ValueGenerated.Never || p.AfterSaveBehavior == PropertySaveBehavior.Save)
                .Where(p => p.PropertyInfo != null)
                .ToArray();

            List<(IProperty Property, IKnownValue Value)> updateExpressions = null;
            if (updater != null)
            {
                if (!(updater.Body is MemberInitExpression entityUpdater))
                    throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updater));

                updateExpressions = new List<(IProperty Property, IKnownValue Value)>();
                foreach (MemberAssignment binding in entityUpdater.Bindings)
                {
                    var property = entityType.FindProperty(binding.Member.Name);
                    if (property == null)
                        throw new InvalidOperationException("Unknown property " + binding.Member.Name);

                    var value = binding.Expression.GetValue<TEntity>(updater, useExpressionCompiler);
                    if (!(value is IKnownValue knownVal))
                        knownVal = new ConstantValue(value, property);

                    foreach (var valProperty in knownVal.GetPropertyValues())
                        valProperty.Property = entityType.FindProperty(valProperty.PropertyName);

                    updateExpressions.Add((property, knownVal));
                }
            }
            else if (!noUpdate)
            {
                updateExpressions = new List<(IProperty Property, IKnownValue Value)>();
                foreach (var property in properties)
                {
                    if (joinColumnNames.Any(c => c.ColumnName == property.Relational().ColumnName))
                        continue;

                    var propertyAccess = new PropertyValue(property.Name, false) { Property = property };
                    updateExpressions.Add((property, propertyAccess));
                }
            }

            KnownExpression updateConditionExpression = null;
            if (updateCondition != null)
            {
                var updateConditionValue = updateCondition.Body.GetValue<TEntity>(updateCondition, useExpressionCompiler);
                if (!(updateConditionValue is KnownExpression updateConditionExp))
                    throw new InvalidOperationException("The update condition must be a comparison expression");
                updateConditionExpression = updateConditionExp;

                foreach (var valProperty in updateConditionExpression.GetPropertyValues())
                    valProperty.Property = entityType.FindProperty(valProperty.PropertyName);
            }

            var newEntities = entities
                .Select(e => properties
                    .Select(p =>
                    {
                        var columnName = p.Relational().ColumnName;
                        var value = new ConstantValue(p.PropertyInfo.GetValue(e), p);
                        return (columnName, value);
                    })
                    .ToArray() as ICollection<(string ColumnName, ConstantValue Value)>)
                .ToArray();

            var arguments = newEntities.SelectMany(e => e.Select(p => p.Value)).ToList();
            if (updateExpressions != null)
                arguments.AddRange(updateExpressions.SelectMany(e => e.Value.GetConstantValues()));
            int i = 0;
            foreach (var arg in arguments)
                arg.ArgumentIndex = i++;

            var columnUpdateExpressions = updateExpressions?.Count > 0
                ? updateExpressions.Select(x => (x.Property.Relational().ColumnName, x.Value)).ToArray()
                : null;
            var sqlCommand = GenerateCommand(GetTableName(entityType), newEntities, joinColumnNames, columnUpdateExpressions, updateConditionExpression);
            return (sqlCommand, arguments);
        }

        /// <summary>
        /// Expand a known value into database syntax
        /// </summary>
        /// <param name="value">The KnownValue that has to be converted to database language</param>
        /// <param name="modifiers">Modifier flags that may affect how an expression is translated to SQL</param>
        /// <returns>A string containing the expression converted to database language</returns>
        protected virtual string ExpandValue(IKnownValue value, ExpressionModifiers modifiers = 0)
        {
            switch (value)
            {
                case PropertyValue prop:
                    if (modifiers.HasFlag(ExpressionModifiers.LeftPropertyAsVariable) && prop.IsLeftParameter)
                        return Variable(prop.Property.Relational().ColumnName);

                    var prefix = prop.IsLeftParameter ? TargetPrefix : SourcePrefix;
                    var suffix = prop.IsLeftParameter ? TargetSuffix : SourceSuffix;
                    return prefix + EscapeName(prop.Property.Relational().ColumnName) + suffix;

                case ConstantValue constVal:
                    return Parameter(constVal.ArgumentIndex);

                case KnownExpression expression:
                    return $"( {ExpandExpression(expression)} )";

                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Expand a known expression into database syntax
        /// </summary>
        /// <param name="expression">The KnownExpression that has to be converted to database language</param>
        /// <param name="modifiers">Modifier flags that may affect how an expression is translated to SQL</param>
        /// <returns>A string containing the expression converted to database language</returns>
        protected virtual string ExpandExpression(KnownExpression expression, ExpressionModifiers modifiers = 0)
        {
            switch (expression.ExpressionType)
            {
                case ExpressionType.Add:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.Subtract:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    {
                        var left = ExpandValue(expression.Value1, modifiers);
                        var right = ExpandValue(expression.Value2, modifiers);
                        var op = GetSimpleOperator(expression.ExpressionType);
                        return $"{left} {op} {right}";
                    }

                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    {
                        var value1Null = expression.Value1 is ConstantValue constant1 && constant1.Value == null;
                        var value2Null = expression.Value2 is ConstantValue constant2 && constant2.Value == null;
                        if (value1Null || value2Null)
                        {
                            return IsNullExpression(value2Null ? expression.Value1 : expression.Value2, expression.ExpressionType == ExpressionType.NotEqual);
                        }

                        var left = ExpandValue(expression.Value1, modifiers);
                        var right = ExpandValue(expression.Value2, modifiers);
                        var op = GetSimpleOperator(expression.ExpressionType);
                        return $"{left} {op} {right}";
                    }

                case ExpressionType.Coalesce:
                    {
                        var left = ExpandValue(expression.Value1, modifiers);
                        var right = ExpandValue(expression.Value2, modifiers);
                        return $"COALESCE({left}, {right})";
                    }

                case ExpressionType.Conditional:
                    {
                        var ifTrue = ExpandValue(expression.Value1, modifiers);
                        var ifFalse = ExpandValue(expression.Value2, modifiers);
                        var test = ExpandValue(expression.Value3, modifiers);
                        return $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";
                    }

                case ExpressionType.MemberAccess:
                case ExpressionType.Constant:
                    {
                        return ExpandValue(expression.Value1, modifiers);
                    }

                default: throw new NotSupportedException("Don't know how to process operation: " + expression.ExpressionType);
            }
        }

        /// <summary>
        /// Translates a check for null values to sql
        /// </summary>
        /// <param name="value">Value to be checked for null</param>
        /// <param name="notNull">Reverse the check to test for non null value</param>
        /// <returns>Sql statement representing the check</returns>
        protected virtual string IsNullExpression(IKnownValue value, bool notNull)
        {
            return !notNull
                ? $"{ExpandValue(value)} IS NULL"
                : $"{ExpandValue(value)} IS NOT NULL";
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
                case ExpressionType.Modulo: return "%";
                case ExpressionType.Multiply: return "*";
                case ExpressionType.Subtract: return "-";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.Equal: return "=";
                case ExpressionType.NotEqual: return "!=";
                default: throw new InvalidOperationException($"{expressionType} is not a simple arithmetic operation");
            }
        }

        /// <inheritdoc/>
        public override int Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>> updateExpression, Expression<Func<TEntity, TEntity, bool>> updateCondition, bool noUpdate, bool useExpressionCompiler)
        {
            var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
            using (var dbCommand = dbContext.Database.GetDbConnection().CreateCommand())
            {
                var (sqlCommand, arguments) = PrepareCommand(entityType, entities, matchExpression, updateExpression, updateCondition, noUpdate, useExpressionCompiler);
                var dbArguments = arguments.Select(a => PrepareDbCommandArgument(dbCommand, relationalTypeMappingSource, a));
                return dbContext.Database.ExecuteSqlCommand(sqlCommand, dbArguments);
            }
        }

        /// <inheritdoc/>
        public override async Task<int> RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>> updateExpression, Expression<Func<TEntity, TEntity, bool>> updateCondition, bool noUpdate, bool useExpressionCompiler,
            CancellationToken cancellationToken)
        {
            var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
            using (var dbCommand = dbContext.Database.GetDbConnection().CreateCommand())
            {
                var (sqlCommand, arguments) = PrepareCommand(entityType, entities, matchExpression, updateExpression, updateCondition, noUpdate, useExpressionCompiler);
                var dbArguments = arguments.Select(a => PrepareDbCommandArgument(dbCommand, relationalTypeMappingSource, a));
                return await dbContext.Database.ExecuteSqlCommandAsync(sqlCommand, dbArguments);
            }
        }

        private object PrepareDbCommandArgument(DbCommand dbCommand, IRelationalTypeMappingSource relationalTypeMappingSource, ConstantValue constantValue)
        {
            RelationalTypeMapping relationalTypeMapping = null;

            if (constantValue.Property != null)
            {
                relationalTypeMapping = relationalTypeMappingSource.FindMapping(constantValue.Property);
            }
            else if (constantValue.MemberInfo != null)
            {
                relationalTypeMapping = relationalTypeMappingSource.FindMapping(constantValue.MemberInfo);
            }

            var dbParameter = relationalTypeMapping?.CreateParameter(dbCommand, Parameter(constantValue.ArgumentIndex), constantValue.Value);
            if (dbParameter == null)
            {
                dbParameter = dbCommand.CreateParameter();
                dbParameter.Direction = ParameterDirection.Input;
                dbParameter.Value = constantValue.Value;
                dbParameter.ParameterName = Parameter(constantValue.ArgumentIndex);
            }
            return dbParameter;
        }
    }
}
