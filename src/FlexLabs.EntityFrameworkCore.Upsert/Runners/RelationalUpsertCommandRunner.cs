using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners;

/// <summary>
/// Base class with common functionality for most relational database runners
/// </summary>
public abstract class RelationalUpsertCommandRunner : UpsertCommandRunnerBase
{
    private static readonly ConcurrentDictionary<(Type RunnerType, IEntityType EntityType, bool AllowIdentityMatch), RelationalTable> TableCache = new();

    /// <summary>
    /// Generate a full command for the upsert operation, given the inputs passed
    /// </summary>
    /// <param name="tableName">The name of the database table</param>
    /// <param name="entities">A collection of entity data (column names and values) to be upserted</param>
    /// <param name="joinColumns">The columns used to match existing items in the database</param>
    /// <param name="updateExpressions">The expressions that represent update commands for matched entities</param>
    /// <param name="updateCondition">The expression that tests whether existing entities should be updated</param>
    /// <param name="returnColumns">
    /// Controls what the generated command returns:
    /// <list type="bullet">
    /// <item><description><see langword="null"/> – do not return any rows.</description></item>
    /// <item><description>empty collection – return all columns via <c>inserted.*</c> / <c>RETURNING *</c>.</description></item>
    /// <item><description>non-empty collection – return the specified columns from the <c>deleted</c>/<c>inserted</c> pseudo-tables. Providers that do not support this should throw <see cref="NotSupportedException"/>.</description></item>
    /// </list>
    /// </param>
    /// <returns>A fully formed database query</returns>
    public abstract string GenerateCommand(string tableName, ICollection<ICollection<(string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts)>> entities,
        ICollection<(string ColumnName, bool IsNullable)> joinColumns, ICollection<(string ColumnName, IKnownValue Value)>? updateExpressions,
        KnownExpression? updateCondition,
        ICollection<(string Alias, bool IsDeletedParam, string ColumnName)>? returnColumns = null);
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

    /// <summary>
    /// Checks if a column should be mapped or ignored based on provider-specific rules.
    /// </summary>
    protected virtual bool ShouldMapColumn(IProperty property) => true;

    /// <summary>
    /// Gets whether this runner supports returning custom projections with access to deleted (before-update) values.
    /// Only SQL Server supports this via the MERGE...OUTPUT deleted.* syntax.
    /// </summary>
    protected virtual bool SupportsDeletedInReturn => false;

    private IEnumerable<(string SqlCommand, IEnumerable<ConstantValue> Arguments)> PrepareCommand<TEntity>(IEntityType entityType, ICollection<TEntity> entities,
        UpsertCommandArgs<TEntity> commandArgs,
        ICollection<(string Alias, bool IsDeletedParam, string ColumnName)>? returnColumns = null)
    {
        var table = TableCache.GetOrAdd(
            (GetType(), entityType, commandArgs.AllowIdentityMatch),
            k => new RelationalTable(k.EntityType, GetTableName(k.EntityType), k.AllowIdentityMatch, ShouldMapColumn));
        var expressionParser = new ExpressionParser<TEntity>(table, commandArgs);

        var joinColumnNames = commandArgs.MatchProperties.Select(c => (ColumnName: c.GetColumnName(), Nullable: c.IsColumnNullable())).ToArray();

        var updateExpressions = commandArgs.UpdateExpression != null
            ? expressionParser.ParseUpdateExpression(commandArgs.UpdateExpression)
            : expressionParser.GetUpdateMappings(joinColumnNames, commandArgs.ExcludeProperties);
        var updateConditionExpression = expressionParser.ParseUpdateConditionExpression(commandArgs.UpdateCondition);
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
            var sqlCommand = GenerateCommand(table.TableName, newEntities.Skip(entitiesProcessed - entitiesHere).Take(entitiesHere).ToArray(), joinColumnNames, columnUpdateExpressions, updateConditionExpression, returnColumns);
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
    public override int Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, UpsertCommandArgs<TEntity> commandArgs)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(commandArgs);

        var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
        var commands = PrepareCommand(entityType, entities, commandArgs);

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
    public override ICollection<TEntity> RunAndReturn<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, UpsertCommandArgs<TEntity> commandArgs)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(commandArgs);

        var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
        var commands = PrepareCommand(entityType, entities, commandArgs, returnColumns: []);

        var result = new List<TEntity>();
        foreach (var (sqlCommand, arguments) in commands)
        {
            using var dbCommand = dbContext.Database.GetDbConnection().CreateCommand();
            var dbArguments = arguments.Select(a => PrepareDbCommandArgument(dbCommand, relationalTypeMappingSource, a)).ToArray();
            var returnedEntities = dbContext.Set<TEntity>().FromSqlRaw(sqlCommand, dbArguments).AsNoTracking().IgnoreQueryFilters().IgnoreAutoIncludes().ToArray();
            AttachOrUpdateEntities(dbContext, entityType, returnedEntities);
            result.AddRange(returnedEntities);
        }
        return result;
    }

    /// <inheritdoc/>
    public override async Task<int> RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
        UpsertCommandArgs<TEntity> commandArgs, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(commandArgs);

        var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
        var commands = PrepareCommand(entityType, entities, commandArgs);

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
        UpsertCommandArgs<TEntity> commandArgs, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(commandArgs);

        var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
        var commands = PrepareCommand(entityType, entities, commandArgs, returnColumns: []);

        var result = new List<TEntity>();
        foreach (var (sqlCommand, arguments) in commands)
        {
            using var dbCommand = dbContext.Database.GetDbConnection().CreateCommand();
            var dbArguments = arguments.Select(a => PrepareDbCommandArgument(dbCommand, relationalTypeMappingSource, a)).ToArray();
            var returnedEntities = await dbContext.Set<TEntity>().FromSqlRaw(sqlCommand, dbArguments).AsNoTracking().IgnoreQueryFilters().IgnoreAutoIncludes().ToArrayAsync(cancellationToken).ConfigureAwait(false);
            AttachOrUpdateEntities(dbContext, entityType, returnedEntities);
            result.AddRange(returnedEntities);
        }
        return result;
    }

    /// <inheritdoc/>
    public override ICollection<TOutput> RunAndReturn<TEntity, TOutput>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
        UpsertCommandArgs<TEntity> commandArgs, Expression<Func<TEntity?, TEntity?, TOutput>> returnExpression)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(commandArgs);
        ArgumentNullException.ThrowIfNull(returnExpression);

        if (!SupportsDeletedInReturn)
            throw new NotSupportedException(Resources.ReturnWithDeletedNotSupported);

        var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
        var returnColumns = ParseReturnExpression(returnExpression, entityType);
        var mapper = CreateReaderMapper(returnExpression);
        var commands = PrepareCommand(entityType, entities, commandArgs, returnColumns: returnColumns).ToArray();

        var result = new List<TOutput>();
        var connection = dbContext.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen)
            connection.Open();
        try
        {
            foreach (var (sqlCommand, arguments) in commands)
            {
                using var dbCommand = connection.CreateCommand();
                dbCommand.CommandText = sqlCommand;
                dbCommand.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
                foreach (var arg in arguments.Select(a => PrepareDbCommandArgument(dbCommand, relationalTypeMappingSource, a)))
                    dbCommand.Parameters.Add(arg);
                using var reader = dbCommand.ExecuteReader();
                while (reader.Read())
                    result.Add(mapper(reader));
            }
        }
        finally
        {
            if (!wasOpen)
                connection.Close();
        }
        return result;
    }

    /// <inheritdoc/>
    public override async Task<ICollection<TOutput>> RunAndReturnAsync<TEntity, TOutput>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
        UpsertCommandArgs<TEntity> commandArgs, Expression<Func<TEntity?, TEntity?, TOutput>> returnExpression, CancellationToken cancellationToken)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(commandArgs);
        ArgumentNullException.ThrowIfNull(returnExpression);

        if (!SupportsDeletedInReturn)
            throw new NotSupportedException(Resources.ReturnWithDeletedNotSupported);

        var relationalTypeMappingSource = dbContext.GetService<IRelationalTypeMappingSource>();
        var returnColumns = ParseReturnExpression(returnExpression, entityType);
        var mapper = CreateReaderMapper(returnExpression);
        var commands = PrepareCommand(entityType, entities, commandArgs, returnColumns: returnColumns).ToArray();

        var result = new List<TOutput>();
        var connection = dbContext.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var (sqlCommand, arguments) in commands)
            {
                using var dbCommand = connection.CreateCommand();
                dbCommand.CommandText = sqlCommand;
                dbCommand.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
                foreach (var arg in arguments.Select(a => PrepareDbCommandArgument(dbCommand, relationalTypeMappingSource, a)))
                    dbCommand.Parameters.Add(arg);
                var reader = await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using(reader.ConfigureAwait(false))
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        result.Add(mapper(reader));
            }
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync().ConfigureAwait(false);
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
        if (constantValue.ColumnProperty is ComplexJsonColumn complexJson)
        {
            relationalTypeMapping = relationalTypeMappingSource.FindMapping(complexJson.Column.ProviderClrType, complexJson.Column.Table.Model.Model, complexJson.Column.StoreTypeMapping);
        }
        else if (constantValue.ColumnProperty is JsonColumn json)
        {
            relationalTypeMapping = relationalTypeMappingSource.FindMapping(json.Column.ProviderClrType, json.Column.Table.Model.Model, json.Column.StoreTypeMapping);
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

    /// <summary>
    /// Attaches or updates entities in the change tracker with fresh database values.
    /// If an entity is already tracked (by matching primary key), updates its values. Otherwise, attaches it.
    /// </summary>
    private static void AttachOrUpdateEntities<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities)
        where TEntity : class
    {
        if (entities.Length == 0)
            return;

        // Get primary key properties once (same for all entities of this type)
        var keyProperties = entityType.FindPrimaryKey()!.Properties;

        // Local function to handle the attach-or-update code
        void ProcessEntities<TKey>(Dictionary<TKey, EntityEntry<TEntity>> trackedEntries, Func<TEntity, TKey> keyExtractor)
            where TKey : notnull
        {
            foreach (var entity in entities)
            {
                var key = keyExtractor(entity);
                if (trackedEntries.TryGetValue(key, out var trackedEntry))
                {
                    trackedEntry.CurrentValues.SetValues(entity);
                }
                else
                {
                    dbContext.Attach(entity);
                }
            }
        }

        // Shadow properties: Use EntityEntry-based key extraction (slower but necessary)
        if (keyProperties.Any(p => p.IsShadowProperty()))
        {
            var trackedEntries = dbContext.ChangeTracker.Entries<TEntity>()
                .ToDictionary(e => CompositeKey.FromEntityEntry(e, keyProperties));

            ProcessEntities(trackedEntries, e => CompositeKey.FromEntityEntry(dbContext.Entry(e), keyProperties));
            return;
        }

        // Fast path: Use CLR property getters directly
        var keyGetters = keyProperties.Select(p => p.GetGetter()).ToArray();

        // Single key: Avoid CompositeKey allocation for better performance
        if (keyProperties.Count == 1)
        {
            var getter = keyGetters[0];
            var trackedEntries = dbContext.ChangeTracker.Entries<TEntity>()
                .ToDictionary(e => getter.GetClrValue(e.Entity)!);

            ProcessEntities(trackedEntries, entity => getter.GetClrValue(entity)!);
        }
        else
        {
            // Composite key: Use CompositeKey for multiple-column primary keys
            var trackedEntries = dbContext.ChangeTracker.Entries<TEntity>()
                .ToDictionary(e => CompositeKey.FromEntity(e.Entity, keyGetters));

            ProcessEntities(trackedEntries, entity => CompositeKey.FromEntity(entity, keyGetters));
        }
    }

    private readonly record struct CompositeKey(object[] Values)
    {
        public bool Equals(CompositeKey other)
        {
            if (Values.Length != other.Values.Length)
                return false;
            for (var i = 0; i < Values.Length; i++)
            {
                if (!Equals(Values[i], other.Values[i]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var value in Values)
                hash.Add(value);
            return hash.ToHashCode();
        }

        public static CompositeKey FromEntity(object entity, IClrPropertyGetter[] getters)
        {
            var values = new object[getters.Length];
            for (int i = 0; i < getters.Length; i++)
            {
                values[i] = getters[i].GetClrValue(entity)!;
            }
            return new CompositeKey(values);
        }

        public static CompositeKey FromEntityEntry(EntityEntry entry, IReadOnlyList<IProperty> keyProperties)
        {
            var values = new object[keyProperties.Count];
            for (int i = 0; i < keyProperties.Count; i++)
            {
                values[i] = entry.Property(keyProperties[i].Name).CurrentValue!;
            }
            return new CompositeKey(values);
        }
    }

    /// <summary>
    /// Parses a return expression to produce the list of OUTPUT columns.
    /// Supports <see cref="MemberInitExpression"/> (named class initialisers) and
    /// <see cref="NewExpression"/> (anonymous types).
    /// </summary>
    internal static ICollection<(string Alias, bool IsDeletedParam, string ColumnName)> ParseReturnExpression<TEntity, TOutput>(
        Expression<Func<TEntity, TEntity, TOutput>> expression,
        IEntityType entityType)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var deletedParam = expression.Parameters[0];
        var insertedParam = expression.Parameters[1];

        return expression.Body switch
        {
            MemberInitExpression memberInit =>
                memberInit.Bindings.Cast<MemberAssignment>()
                    .Select(b => ParseReturnColumn(b.Member.Name, b.Expression, deletedParam, insertedParam, entityType))
                    .ToArray(),
            NewExpression { Members: not null } newExpr =>
                newExpr.Members.Select((m, i) =>
                        ParseReturnColumn(m.Name, newExpr.Arguments[i], deletedParam, insertedParam, entityType))
                    .ToArray(),
            _ => throw new ArgumentException(
                Resources.FormatReturnExpressionMustBeAnInitialiserOfTOutputType(typeof(TOutput).Name),
                nameof(expression))
        };
    }

    private static (string Alias, bool IsDeletedParam, string ColumnName) ParseReturnColumn(
        string alias,
        Expression valueExpr,
        ParameterExpression deletedParam,
        ParameterExpression insertedParam,
        IEntityType entityType)
    {
        // Strip type-conversion wrappers
        while (valueExpr is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
            valueExpr = u.Operand;

        if (valueExpr is MemberExpression { Member: PropertyInfo propInfo, Expression: ParameterExpression paramExpr })
        {
            if (!ReferenceEquals(paramExpr, deletedParam) && !ReferenceEquals(paramExpr, insertedParam))
                throw new ArgumentException(
                    Resources.FormatReturnExpressionBindingMustAccessDeletedOrInserted(alias));

            var isDeleted = ReferenceEquals(paramExpr, deletedParam);
            var property = entityType.FindProperty(propInfo.Name)
                ?? throw new ArgumentException(Resources.FormatUnknownProperty(propInfo.Name));

            return (alias, isDeleted, property.GetColumnName());
        }

        throw new ArgumentException(
            Resources.FormatReturnExpressionBindingMustAccessDeletedOrInserted(alias));
    }

    /// <summary>
    /// Creates a <see cref="DbDataReader"/>-to-<typeparamref name="TOutput"/> mapper from the return expression.
    /// Columns are read by ordinal position matching the order produced by
    /// <see cref="ParseReturnExpression{TEntity,TOutput}"/>.
    /// </summary>
    protected static Func<DbDataReader, TOutput> CreateReaderMapper<TEntity, TOutput>(
        Expression<Func<TEntity?, TEntity?, TOutput>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        switch (expression.Body)
        {
            case MemberInitExpression memberInit:
            {
                var ctor = typeof(TOutput).GetConstructor(Type.EmptyTypes);
                if (ctor is null)
                    throw new InvalidOperationException(
                        $"Type '{typeof(TOutput).Name}' does not have a public parameterless constructor, which is required for member-init projection.");

                var properties = memberInit.Bindings.Cast<MemberAssignment>()
                    .Select(b => (PropertyInfo)b.Member)
                    .ToArray();

                return reader =>
                {
                    var obj = Activator.CreateInstance<TOutput>();
                    for (var i = 0; i < properties.Length; i++)
                        properties[i].SetValue(obj, ReadValue(reader, i, properties[i].PropertyType));
                    return obj;
                };
            }

            case NewExpression { Constructor: not null } newExpr:
            {
                var ctor = newExpr.Constructor;
                var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToArray();

                return reader =>
                {
                    var args = new object?[paramTypes.Length];
                    for (var i = 0; i < paramTypes.Length; i++)
                        args[i] = ReadValue(reader, i, paramTypes[i]);
                    return (TOutput)ctor.Invoke(args);
                };
            }

            default:
                throw new ArgumentException(
                    Resources.FormatReturnExpressionMustBeAnInitialiserOfTOutputType(typeof(TOutput).Name),
                    nameof(expression));
        }
    }

    private static object? ReadValue(DbDataReader reader, int ordinal, Type targetType)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetValue(ordinal);
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsEnum)
            return Enum.ToObject(underlyingType, value);

        if (underlyingType.IsInstanceOfType(value))
            return value;

        return Convert.ChangeType(value, underlyingType, System.Globalization.CultureInfo.InvariantCulture);
    }
}
