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
        public abstract bool Supports(string name);

        /// <inheritdoc/>
        public abstract int Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>> updateExpression, bool noUpdate, bool useExpressionCompiler) where TEntity : class;

        /// <inheritdoc/>
        public abstract Task<int> RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>> updateExpression, bool noUpdate, bool useExpressionCompiler, CancellationToken cancellationToken) where TEntity : class;

        /// <summary>
        /// Extract property metadata from the match expression
        /// </summary>
        /// <typeparam name="TEntity">Type of the entity being upserted</typeparam>
        /// <param name="entityType">Metadata type of the entity being upserted</param>
        /// <param name="matchExpression">The match expression provided by the user</param>
        /// <returns>A list of model properties used to match entities</returns>
        protected List<IProperty> ProcessMatchExpression<TEntity>(IEntityType entityType, Expression<Func<TEntity, object>> matchExpression)
        {
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
                    if (arg == null || !(arg.Member is PropertyInfo) || !typeof(TEntity).Equals(arg.Expression.Type))
                        throw new InvalidOperationException("Match columns have to be properties of the TEntity class");
                    var property = entityType.FindProperty(arg.Member.Name);
                    if (property == null)
                        throw new InvalidOperationException("Unknown property " + arg.Member.Name);
                    joinColumns.Add(property);
                }
            }
            else if (matchExpression.Body is UnaryExpression unaryExpression)
            {
                if (!(unaryExpression.Operand is MemberExpression memberExp) || !(memberExp.Member is PropertyInfo) || !typeof(TEntity).Equals(memberExp.Expression.Type))
                    throw new InvalidOperationException("Match columns have to be properties of the TEntity class");
                var property = entityType.FindProperty(memberExp.Member.Name);
                joinColumns = new List<IProperty> { property };
            }
            else if (matchExpression.Body is MemberExpression memberExpression)
            {
                if (!typeof(TEntity).Equals(memberExpression.Expression.Type) || !(memberExpression.Member is PropertyInfo))
                    throw new InvalidOperationException("Match columns have to be properties of the TEntity class");
                var property = entityType.FindProperty(memberExpression.Member.Name);
                joinColumns = new List<IProperty> { property };
            }
            else
            {
                throw new ArgumentException("match must be an anonymous object initialiser", nameof(matchExpression));
            }

            if (joinColumns.All(p => p.ValueGenerated != ValueGenerated.Never))
                throw new InvalidMatchColumnsException();

            return joinColumns;
        }
    }
}
