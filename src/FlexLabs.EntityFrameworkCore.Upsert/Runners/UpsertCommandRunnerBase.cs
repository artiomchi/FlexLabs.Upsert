using System;
using System.Collections.Generic;
using System.Globalization;
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
        public abstract bool Supports(string name);

        /// <inheritdoc/>
        public abstract int Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>>? matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions)
            where TEntity : class;

        /// <inheritdoc/>
        public abstract Task<int> RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>>? matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions,
            CancellationToken cancellationToken) where TEntity : class;

        /// <summary>
        /// Extract property metadata from the match expression
        /// </summary>
        /// <typeparam name="TEntity">Type of the entity being upserted</typeparam>
        /// <param name="entityType">Metadata type of the entity being upserted</param>
        /// <param name="matchExpression">The match expression provided by the user</param>
        /// <param name="queryOptions">Options for the current query that will affect it's behaviour</param>
        /// <returns>A list of model properties used to match entities</returns>
        protected static List<IProperty> ProcessMatchExpression<TEntity>(IEntityType entityType, Expression<Func<TEntity, object>>? matchExpression, RunnerQueryOptions queryOptions)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));

            List<IProperty> joinColumns;
            if (matchExpression is null)
            {
                joinColumns = entityType.GetProperties()
                    .Where(p => p.IsKey())
                    .ToList();
            }
            else if (matchExpression.Body is NewExpression newExpression)
            {
                joinColumns = new List<IProperty>();
                foreach (MemberExpression arg in newExpression.Arguments)
                {
                    if (arg == null || arg.Member is not PropertyInfo || !typeof(TEntity).Equals(arg.Expression.Type))
                        throw new InvalidOperationException(Resources.MatchColumnsHaveToBePropertiesOfTheTEntityClass);
                    var property = entityType.FindProperty(arg.Member.Name);
                    if (property == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.UnknownProperty, arg.Member.Name));
                    joinColumns.Add(property);
                }
            }
            else if (matchExpression.Body is UnaryExpression unaryExpression)
            {
                if (unaryExpression.Operand is not MemberExpression memberExp || memberExp.Member is not PropertyInfo || !typeof(TEntity).Equals(memberExp.Expression.Type))
                    throw new InvalidOperationException(Resources.MatchColumnsHaveToBePropertiesOfTheTEntityClass);
                var property = entityType.FindProperty(memberExp.Member.Name);
                joinColumns = new List<IProperty> { property };
            }
            else if (matchExpression.Body is MemberExpression memberExpression)
            {
                if (!typeof(TEntity).Equals(memberExpression.Expression.Type) || memberExpression.Member is not PropertyInfo)
                    throw new InvalidOperationException(Resources.MatchColumnsHaveToBePropertiesOfTheTEntityClass);
                var property = entityType.FindProperty(memberExpression.Member.Name);
                joinColumns = new List<IProperty> { property };
            }
            else
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ArgumentMustBeAnAnonymousObjectInitialiser, "match"), nameof(matchExpression));
            }

            if (!queryOptions.AllowIdentityMatch && joinColumns.Any(p => p.ValueGenerated != ValueGenerated.Never))
                throw new InvalidMatchColumnsException();

            return joinColumns;
        }
    }
}
