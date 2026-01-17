using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners;

/// <summary>
/// Base class with common helper methods for upsert command runners
/// </summary>
public abstract class UpsertCommandRunnerBase : IUpsertCommandRunner
{
    /// <inheritdoc/>
    public abstract bool Supports(string providerName);

    /// <inheritdoc/>
    public abstract int Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>>? matchExpression,
        Expression<Func<TEntity, object>>? excludeExpression, Expression<Func<TEntity, TEntity, TEntity>>? updateExpression,
        Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions)
        where TEntity : class;

    /// <inheritdoc/>
    public abstract ICollection<TEntity> RunAndReturn<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
        Expression<Func<TEntity, object>>? matchExpression, Expression<Func<TEntity, object>>? excludeExpression,
        Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions)
        where TEntity : class;

    /// <inheritdoc/>
    public abstract Task<int> RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
        Expression<Func<TEntity, object>>? matchExpression, Expression<Func<TEntity, object>>? excludeExpression,
        Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions,
        CancellationToken cancellationToken) where TEntity : class;

    /// <inheritdoc/>
    public abstract Task<ICollection<TEntity>> RunAndReturnAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
        Expression<Func<TEntity, object>>? matchExpression, Expression<Func<TEntity, object>>? excludeExpression,
        Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions,
        CancellationToken cancellationToken)
        where TEntity : class;

    /// <summary>
    /// Extract property metadata from the match expression
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity being upserted</typeparam>
    /// <param name="entityType">Metadata type of the entity being upserted</param>
    /// <param name="matchExpression">The match expression provided by the user</param>
    /// <param name="queryOptions">Options for the current query that will affect its behaviour</param>
    /// <returns>A list of model properties used to match entities</returns>
    protected static ICollection<IProperty> ProcessMatchExpression<TEntity>(IEntityType entityType, Expression<Func<TEntity, object>>? matchExpression,
        RunnerQueryOptions queryOptions)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        List<IProperty> joinColumns;
        if (matchExpression is null)
        {
            joinColumns = entityType.GetProperties()
                .Where(p => p.IsKey())
                .ToList();
        }
        else
        {
            joinColumns = ProcessPropertiesExpression(entityType, matchExpression, true);
        }

        if (!queryOptions.AllowIdentityMatch && joinColumns.Any(p => p.ValueGenerated != ValueGenerated.Never))
            throw new InvalidMatchColumnsException();

        return joinColumns;
    }

    /// <summary>
    /// Extract property metadata from the exclude expression
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity being upserted</typeparam>
    /// <param name="entityType">Metadata type of the entity being upserted</param>
    /// <param name="excludeExpression">The exclude expression provided by the user</param>
    /// <returns>A list of model properties used to exclude columns entities</returns>
    protected static ICollection<IProperty> ProcessExcludeExpression<TEntity>(IEntityType entityType, Expression<Func<TEntity, object>>? excludeExpression)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        if (excludeExpression is null)
            return [];

        return ProcessPropertiesExpression(entityType, excludeExpression, false);
    }

    private static List<IProperty> ProcessPropertiesExpression<TEntity>(IEntityType entityType, Expression<Func<TEntity, object>> propertiesExpression, bool match)
    {
        static string UnkwownPropertiesExceptionMessage(bool match)
            => match
            ? Resources.MatchColumnsHaveToBePropertiesOfTheTEntityClass
            : Resources.ExcludeColumnsHaveToBePropertiesOfTheTEntityClass;

        if (propertiesExpression.Body is NewExpression newExpression)
        {
            var columns = new List<IProperty>();
            foreach (MemberExpression arg in newExpression.Arguments)
            {
                if (arg == null || arg.Member is not PropertyInfo || !typeof(TEntity).Equals(arg.Expression?.Type))
                    throw new InvalidOperationException(UnkwownPropertiesExceptionMessage(match));
                // TODO use table.FindColumn(..) to have unified ColumnName resolution and to support owned properties in Match Expression!
                var property = entityType.FindProperty(arg.Member.Name)
                    ?? throw new InvalidOperationException(Resources.FormatUnknownProperty(arg.Member.Name));
                columns.Add(property);
            }
            return columns;
        }
        else if (propertiesExpression.Body is UnaryExpression unaryExpression)
        {
            if (unaryExpression.Operand is not MemberExpression memberExp || memberExp.Member is not PropertyInfo || !typeof(TEntity).Equals(memberExp.Expression?.Type))
                throw new InvalidOperationException(UnkwownPropertiesExceptionMessage(match));
            // TODO use table.FindColumn(..) to have unified ColumnName resolution and to support owned properties in Match Expression!
            var property = entityType.FindProperty(memberExp.Member.Name)
                ?? throw new InvalidOperationException(Resources.FormatUnknownProperty(memberExp.Member.Name));
            return [property];
        }
        else if (propertiesExpression.Body is MemberExpression memberExpression)
        {
            if (!typeof(TEntity).Equals(memberExpression.Expression?.Type) || memberExpression.Member is not PropertyInfo)
                throw new InvalidOperationException(UnkwownPropertiesExceptionMessage(match));
            // TODO use table.FindColumn(..) to have unified ColumnName resolution and to support owned properties in Match Expression!
            var property = entityType.FindProperty(memberExpression.Member.Name)
                ?? throw new InvalidOperationException(Resources.FormatUnknownProperty(memberExpression.Member.Name));
            return [property];
        }
        else
        {
            throw new ArgumentException(Resources.FormatArgumentMustBeAnAnonymousObjectInitialiser(match ? "match" : "exclude"), nameof(propertiesExpression));
        }
    }
}
