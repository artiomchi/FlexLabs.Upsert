using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;
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
        /// Generate a full command for the upsert operation, given the inputs passed
        /// </summary>
        /// <param name="tableName">The name of the database table</param>
        /// <param name="entities">A collection of entity data (column names and values) to be upserted</param>
        /// <param name="joinColumns">The columns used to match existing items in the database</param>
        /// <param name="updateExpressions">The expressions that represent update commands for matched entities</param>
        /// <param name="updateCondition">The expression that tests whether existing entities should be updated</param>
        /// <param name="returnResult">If true, the generated command should return upserted entities</param>
        /// <returns>A fully formed database query</returns>
        public abstract string GenerateCommand(string tableName, ICollection<ICollection<(string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts)>> entities,
            ICollection<(string ColumnName, bool IsNullable)> joinColumns, ICollection<(string ColumnName, IKnownValue Value)>? updateExpressions,
            KnownExpression? updateCondition, bool returnResult = false);
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
        /// Reference a named variable defined by the query runner
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <returns>The reference to the variable</returns>
        protected virtual string Variable(string name) => "@x" + name;
        /// <summary>
        /// Get the escaped database table schema
        /// </summary>
        /// <param name="entityType">The entity type of the table</param>
        /// <returns>The escaped schema name of the table, followed by a '.'. If the table has no schema - returns null</returns>
        protected virtual string? GetSchema(IEntityType entityType)
        {
            var schema = entityType.GetSchema();
            return schema != null
                ? EscapeName(schema) + "."
                : null;
        }
        /// <summary>
        /// Get the fully qualified, escaped table name
        /// </summary>
        /// <param name="entityType">The entity type of the table</param>
        /// <returns>The fully qualified and escaped table reference</returns>
        protected virtual string GetTableName(IEntityType entityType)
        {
            var tableName = entityType.GetTableName() ?? entityType.GetViewName()
                ?? throw new InvalidOperationException(Resources.FormatCouldNotGetTableNameForEntityType(entityType?.Name));
            return GetSchema(entityType) + EscapeName(tableName);
        }

        /// <summary>
        /// Prefix used to reference source dataset columns
        /// </summary>
        protected abstract string? SourcePrefix { get; }
        /// <summary>
        /// Suffix used when referencing source dataset columns
        /// </summary>
        protected virtual string? SourceSuffix => null;
        /// <summary>
        /// Prefix used to reference target table columns
        /// </summary>
        protected abstract string? TargetPrefix { get; }
        /// <summary>
        /// Suffix used when referencing target table columns
        /// </summary>
        protected virtual string? TargetSuffix => null;
        /// <summary>
        /// The maximum number of parameters that the db engine allows to be passed to a query
        /// </summary>
        protected virtual int? MaxQueryParams => null;

        private IEnumerable<(string SqlCommand, IEnumerable<ConstantValue> Arguments)> PrepareCommand<TEntity>(IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>>? match, Expression<Func<TEntity, object>>? exclude, Expression<Func<TEntity, TEntity, TEntity>>? updater,
            Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions, bool returnResult = false)
        {
            var table = new RelationalTable(entityType, GetTableName(entityType), queryOptions);
            var expressionParser = new ExpressionParser<TEntity>(table, queryOptions);

            var joinColumns = ProcessMatchExpression(entityType, match, queryOptions);
            var joinColumnNames = joinColumns.Select(c => (ColumnName: c.GetColumnName(), Nullable: c.IsColumnNullable())).ToArray();

            var updateExpressions = updater != null
                ? expressionParser.ParseUpdateExpression(updater)
                : expressionParser.GetUpdateMappings(joinColumnNames, ProcessExcludeExpression(entityType, exclude));
            var updateConditionExpression = expressionParser.ParseUpdateConditionExpression(updateCondition);
            var newEntities = entities
                .Select(e => table.Columns
                    .Select(p => p.GetValue(e!))
                    .ToArray())
                .ToArray();

            var constantArgumentSourceValues = updateExpressions?.Select(e => e.Value);
            if (updateConditionExpression != null)
                constantArgumentSourceValues = constantArgumentSourceValues?.Append(updateConditionExpression) ?? [updateConditionExpression];
            var expressionConstants = constantArgumentSourceValues?.SelectMany(v => v.GetConstantValues()).ToArray();

            var entitiesProcessed = 0;
            var singleEntityArguments = newEntities[0].Length + (expressionConstants?.Length ?? 0);
            while (entitiesProcessed < newEntities.Length)
            {
                var arguments = new List<ConstantValue>();

                var entitiesHere = 0;
                do
                {
                    arguments.AddRange(newEntities[entitiesProcessed].Select(p => p.Value));
                    entitiesProcessed++;
                    entitiesHere++;
                }
                while (entitiesProcessed < newEntities.Length &&
                    (MaxQueryParams == null || arguments.Count + singleEntityArguments < MaxQueryParams));

                if (expressionConstants != null)
                    arguments.AddRange(expressionConstants);

                foreach (var (arg, index) in arguments.Select((a, i) => (a, i)))
                    arg.ArgumentIndex = index;

                var columnUpdateExpressions = updateExpressions?.Length > 0
                    ? updateExpressions.Select(x => (x.Property.ColumnName, x.Value)).ToArray()
                    : null;
                var sqlCommand = GenerateCommand(table.TableName, newEntities.Skip(entitiesProcessed - entitiesHere).Take(entitiesHere).ToArray(), joinColumnNames, columnUpdateExpressions, updateConditionExpression, returnResult);
                yield return (sqlCommand, arguments);
            }
        }

        /// <summary>
        /// Expand a known value into database syntax
        /// </summary>
        /// <param name="value">The KnownValue that has to be converted to database language</param>
        /// <param name="expandLeftColumn">Override the way the table column names are rendered</param>
        /// <returns>A string containing the expression converted to database language</returns>
        protected virtual string ExpandValue(IKnownValue value, Func<string, string>? expandLeftColumn = null)
        {
            switch (value)
            {
                case PropertyValue prop:
                    var columnName = prop.Column.ColumnName;
                    if (expandLeftColumn != null && prop.IsLeftParameter)
                        return expandLeftColumn(columnName);

                    var prefix = prop.IsLeftParameter ? TargetPrefix : SourcePrefix;
                    var suffix = prop.IsLeftParameter ? TargetSuffix : SourceSuffix;
                    return prefix + EscapeName(columnName) + suffix;

                case ConstantValue constVal:
                    return Parameter(constVal.ArgumentIndex);

                case KnownExpression expression:
                    return $"( {ExpandExpression(expression, expandLeftColumn)} )";

                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Expand a known expression into database syntax
        /// </summary>
        /// <param name="expression">The KnownExpression that has to be converted to database language</param>
        /// <param name="expandLeftColumn">Override the way the table column names are rendered</param>
        /// <returns>A string containing the expression converted to database language</returns>
        protected virtual string ExpandExpression(KnownExpression expression, Func<string, string>? expandLeftColumn = null)
        {
            ArgumentNullException.ThrowIfNull(expression);

            switch (expression.ExpressionType)
            {
                case ExpressionType.Add:
                case ExpressionType.And:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.Or:
                case ExpressionType.Subtract:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    {
                        var left = ExpandValue(expression.Value1, expandLeftColumn);
                        var right = ExpandValue(expression.Value2!, expandLeftColumn);
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
                            return IsNullExpression(value2Null ? expression.Value1! : expression.Value2!, expression.ExpressionType == ExpressionType.NotEqual);
                        }

                        var left = ExpandValue(expression.Value1, expandLeftColumn);
                        var right = ExpandValue(expression.Value2!, expandLeftColumn);
                        var op = GetSimpleOperator(expression.ExpressionType);
                        return $"{left} {op} {right}";
                    }

                case ExpressionType.Coalesce:
                    {
                        var left = ExpandValue(expression.Value1, expandLeftColumn);
                        var right = ExpandValue(expression.Value2!, expandLeftColumn);
                        return $"COALESCE({left}, {right})";
                    }

                case ExpressionType.Conditional:
                    {
                        var ifTrue = ExpandValue(expression.Value1, expandLeftColumn);
                        var ifFalse = ExpandValue(expression.Value2!, expandLeftColumn);
                        var test = ExpandValue(expression.Value3!, expandLeftColumn);
                        return $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";
                    }

                case ExpressionType.MemberAccess:
                case ExpressionType.Constant:
                    {
                        return ExpandValue(expression.Value1, expandLeftColumn);
                    }

                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    {
                        var exp = expression.ExpressionType == ExpressionType.AndAlso ? "AND" : "OR";
                        var left = ExpandValue(expression.Value1, expandLeftColumn);
                        var right = ExpandValue(expression.Value2!, expandLeftColumn);
                        return $"{left} {exp} {right}";
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
            return expressionType switch
            {
                ExpressionType.Add => "+",
                ExpressionType.And => "&",
                ExpressionType.Divide => "/",
                ExpressionType.Modulo => "%",
                ExpressionType.Multiply => "*",
                ExpressionType.Or => "|",
                ExpressionType.Subtract => "-",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "!=",
                _ => throw new InvalidOperationException($"{expressionType} is not a simple arithmetic operation"),
            };
        }

        /// <inheritdoc/>
        public override int Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>>? matchExpression,
            Expression<Func<TEntity, object>>? excludeExpression, Expression<Func<TEntity, TEntity, TEntity>>? updateExpression,
            Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(entityType);

            var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
            var commands = PrepareCommand(entityType, entities, matchExpression, excludeExpression, updateExpression, updateCondition, queryOptions);

            int result = 0;
            foreach (var (sqlCommand, arguments) in commands)
            {
                using var dbCommand = dbContext.Database.GetDbConnection().CreateCommand();
                var dbArguments = arguments.Select(a => PrepareDbCommandArgument(dbCommand, relationalTypeMappingSource, a));
                result += dbContext.Database.ExecuteSqlRaw(sqlCommand, dbArguments);
            }
            return result;
        }

        /// <inheritdoc/>
        public override ICollection<TEntity> RunAndReturn<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>>? matchExpression, Expression<Func<TEntity, object>>? excludeExpression,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(entityType);

            var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
            var commands = PrepareCommand(entityType, entities, matchExpression, excludeExpression, updateExpression, updateCondition, queryOptions, true);

            var result = new List<TEntity>();
            foreach (var (sqlCommand, arguments) in commands)
            {
                using var dbCommand = dbContext.Database.GetDbConnection().CreateCommand();
                var dbArguments = arguments.Select(a => PrepareDbCommandArgument(dbCommand, relationalTypeMappingSource, a)).ToArray();
                result.AddRange(dbContext.Set<TEntity>().FromSqlRaw(sqlCommand, dbArguments).ToArray());
            }
            return result;
        }

        /// <inheritdoc/>
        public override async Task<int> RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>>? matchExpression, Expression<Func<TEntity, object>>? excludeExpression,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(entityType);

            var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
            var commands = PrepareCommand(entityType, entities, matchExpression, excludeExpression, updateExpression, updateCondition, queryOptions);

            int result = 0;
            foreach (var (sqlCommand, arguments) in commands)
            {
                using var dbCommand = dbContext.Database.GetDbConnection().CreateCommand();
                var dbArguments = arguments.Select(a => PrepareDbCommandArgument(dbCommand, relationalTypeMappingSource, a));
                result += await dbContext.Database.ExecuteSqlRawAsync(sqlCommand, dbArguments, cancellationToken).ConfigureAwait(false);
            }
            return result;
        }

        /// <inheritdoc/>
        public override async Task<ICollection<TEntity>> RunAndReturnAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>>? matchExpression, Expression<Func<TEntity, object>>? excludeExpression,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(entityType);

            var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
            var commands = PrepareCommand(entityType, entities, matchExpression, excludeExpression, updateExpression, updateCondition, queryOptions, true);

            var result = new List<TEntity>();
            foreach (var (sqlCommand, arguments) in commands)
            {
                using var dbCommand = dbContext.Database.GetDbConnection().CreateCommand();
                var dbArguments = arguments.Select(a => PrepareDbCommandArgument(dbCommand, relationalTypeMappingSource, a)).ToArray();
                result.AddRange(await dbContext.Set<TEntity>().FromSqlRaw(sqlCommand, dbArguments).ToArrayAsync(cancellationToken).ConfigureAwait(false));
            }
            return result;
        }

        private DbParameter PrepareDbCommandArgument(DbCommand dbCommand, IRelationalTypeMappingSource relationalTypeMappingSource, ConstantValue constantValue)
        {
            RelationalTypeMapping? relationalTypeMapping = null;

            if (constantValue.ColumnProperty is RelationalColumn relational)
            {
                relationalTypeMapping = relationalTypeMappingSource.FindMapping(relational.Property);
            }
            else if (constantValue.ColumnProperty is JsonColumn json)
            {
                relationalTypeMapping = relationalTypeMappingSource.FindMapping(json.Column.StoreType);
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
                dbParameter.Value = constantValue.Value ?? DBNull.Value;
                dbParameter.ParameterName = Parameter(constantValue.ArgumentIndex);
            }
            return dbParameter;
        }
    }
}
