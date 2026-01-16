using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Base class with common helper methods for upsert command runners
    /// </summary>
    public abstract class UpsertCommandRunnerBase : IUpsertCommandRunner
    {
        /// <inheritdoc/>
        public abstract bool Supports(string providerName);

        /// <inheritdoc/>
        public abstract int Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>>? matchExpression,
            ICollection<Expression<Func<TEntity, object>>>? excludeExpressions,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression,
            Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions)
            where TEntity : class;

        /// <inheritdoc/>
        public abstract ICollection<TEntity> RunAndReturn<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>>? matchExpression, ICollection<Expression<Func<TEntity, object>>>? excludeExpressions, Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition,
            RunnerQueryOptions queryOptions) where TEntity : class;
    

        /// <inheritdoc/>
        public abstract Task<int> RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>>? matchExpression, ICollection<Expression<Func<TEntity, object>>>? excludeExpressions,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions,
            CancellationToken cancellationToken) where TEntity : class;

        /// <inheritdoc/>
        public abstract Task<ICollection<TEntity>> RunAndReturnAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>>? matchExpression, ICollection<Expression<Func<TEntity, object>>>? excludeExpressions,
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
        protected static ICollection<IProperty> ProcessMatchExpression<TEntity>(IEntityType entityType, Expression<Func<TEntity, object>>? matchExpression,RunnerQueryOptions queryOptions)
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
        /// <param name="excludeExpressions">The exclude expression provided by the user</param>
        /// <returns>A list of model properties used to exclude columns entities</returns>
        protected static ICollection<IProperty> ProcessExcludeExpression<TEntity>(IEntityType entityType, ICollection<Expression<Func<TEntity, object>>>? excludeExpressions)
        {
            ArgumentNullException.ThrowIfNull(entityType);
            List<List<IProperty>> excludeColumns = [];
            if (excludeExpressions != null)
                excludeColumns.AddRange(
                    excludeExpressions.Select(excludeExpression =>
                        ProcessPropertiesExpression(entityType, excludeExpression, false)));

            return excludeColumns.SelectMany(i => i).ToList();
        }

        private static List<IProperty> ProcessPropertiesExpression<TEntity>(IEntityType entityType, Expression<Func<TEntity, object>> propertiesExpression, bool match)
        {
            static string UnknownPropertiesExceptionMessage(bool match)
                => match
                ? Resources.MatchColumnsHaveToBePropertiesOfTheTEntityClass
                : Resources.ExcludeColumnsHaveToBePropertiesOfTheTEntityClass;

            switch (propertiesExpression.Body)
            {
                case NewExpression newExpression:
                {
                    var columns = new List<IProperty>();
                    foreach (MemberExpression arg in newExpression.Arguments)
                    {
                        if (arg == null || arg.Member is not PropertyInfo || !typeof(TEntity).Equals(arg.Expression?.Type))
                            throw new InvalidOperationException(UnknownPropertiesExceptionMessage(match));
                        // TODO use table.FindColumn(..) to have unified ColumnName resolution and to support owned properties in Match Expression!
                        var property = entityType.FindProperty(arg.Member.Name)
                                       ?? throw new InvalidOperationException(Resources.FormatUnknownProperty(arg.Member.Name));
                        columns.Add(property);
                    }
                    return columns;
                }
                case UnaryExpression unaryExpression:
                {
                    if (unaryExpression.Operand is not MemberExpression memberExp || memberExp.Member is not PropertyInfo || !typeof(TEntity).Equals(memberExp.Expression?.Type))
                        throw new InvalidOperationException(UnknownPropertiesExceptionMessage(match));
                    // TODO use table.FindColumn(..) to have unified ColumnName resolution and to support owned properties in Match Expression!
                    var property = entityType.FindProperty(memberExp.Member.Name)
                                   ?? throw new InvalidOperationException(Resources.FormatUnknownProperty(memberExp.Member.Name));
                    return [property];
                }
                case MemberExpression memberExpression when !typeof(TEntity).Equals(memberExpression.Expression?.Type) || memberExpression.Member is not PropertyInfo:
                    throw new InvalidOperationException(UnknownPropertiesExceptionMessage(match));
                // TODO use table.FindColumn(..) to have unified ColumnName resolution and to support owned properties in Match Expression!
                case MemberExpression memberExpression:
                {
                    var property = entityType.FindProperty(memberExpression.Member.Name)
                                   ?? throw new InvalidOperationException(Resources.FormatUnknownProperty(memberExpression.Member.Name));
                    return [property];
                }
                default:
                    throw new ArgumentException(Resources.FormatArgumentMustBeAnAnonymousObjectInitialiser(match ? "match" : "exclude"), nameof(propertiesExpression));
            }
        }
    }
}
