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
    public class InMemoryUpsertCommandRunner : IUpsertCommandRunner
    {
        public bool Supports(string providerName) => providerName == "Microsoft.EntityFrameworkCore.InMemory";

        public void RunCore<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity>> updateExpression) where TEntity : class
        {
            var matches = entities.AsQueryable()
                .GroupJoin(dbContext.Set<TEntity>(), matchExpression, matchExpression, (e1, e2) => new { e1, e2 })
                .SelectMany(x => x.e2.DefaultIfEmpty(), (x, e2) => new { x.e1, e2 })
                .ToArray();

            Action<TEntity, TEntity> updateAction = null;
            if (updateExpression != null)
            {
                if (!(updateExpression.Body is MemberInitExpression entityUpdater))
                    throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updateExpression));

                var properties = entityUpdater.Bindings.Select(b => b.Member).OfType<PropertyInfo>();
                var updateFunc = updateExpression.Compile();
                updateAction = (e1, e2) =>
                {
                    var tmp = updateFunc(e2);
                    foreach (var prop in properties)
                        prop.SetValue(e2, prop.GetValue(tmp));
                };
            }
            else
            {
                List<PropertyInfo> joinColumns;
                if (matchExpression.Body is NewExpression newExpression)
                {
                    joinColumns = new List<PropertyInfo>();
                    foreach (MemberExpression arg in newExpression.Arguments)
                    {
                        if (arg == null || !(arg.Member is PropertyInfo property) || !typeof(TEntity).Equals(arg.Expression.Type))
                            throw new InvalidOperationException("Match columns have to be properties of the TEntity class");
                        joinColumns.Add(property);
                    }
                }
                else if (matchExpression.Body is UnaryExpression unaryExpression)
                {
                    if (!(unaryExpression.Operand is MemberExpression memberExp) || !typeof(TEntity).Equals(memberExp.Expression.Type) || !(memberExp.Member is PropertyInfo property))
                        throw new InvalidOperationException("Match columns have to be properties of the TEntity class");
                    joinColumns = new List<PropertyInfo> { property };
                }
                else if (matchExpression.Body is MemberExpression memberExpression)
                {
                    if (!typeof(TEntity).Equals(memberExpression.Expression.Type) || !(memberExpression.Member is PropertyInfo property))
                        throw new InvalidOperationException("Match columns have to be properties of the TEntity class");
                    joinColumns = new List<PropertyInfo> { property };
                }
                else
                {
                    throw new ArgumentException("match must be an anonymous object initialiser", nameof(matchExpression));
                }

                var properties = entityType.GetProperties()
                    .Where(p => p.ValueGenerated == ValueGenerated.Never)
                    .Select(p => typeof(TEntity).GetProperty(p.Name))
                    .Where(p => p != null)
                    .Except(joinColumns);
                updateAction = (e1, e2) =>
                {
                    foreach (var prop in properties)
                        prop.SetValue(e2, prop.GetValue(e1));
                };
            }

            foreach (var match in matches)
            {
                if (match.e2 == null)
                {
                    dbContext.Add(match.e1);
                    continue;
                }

                updateAction?.Invoke(match.e1, match.e2);
            }

            dbContext.SaveChanges();
        }

        public void Run<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity>> updateExpression) where TEntity : class
        {
            RunCore(dbContext, entityType, entities, matchExpression, updateExpression);
            dbContext.SaveChanges();
        }

        public Task RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity>> updateExpression, CancellationToken cancellationToken) where TEntity : class
        {
            RunCore(dbContext, entityType, entities, matchExpression, updateExpression);
            return dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
