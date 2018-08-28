using System;
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

            Action<TEntity> updateAction = null;
            if (updateExpression != null)
            {
                if (!(updateExpression.Body is MemberInitExpression entityUpdater))
                    throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updateExpression));

                var properties = entityUpdater.Bindings.Select(b => b.Member).OfType<PropertyInfo>();
                var updateFunc = updateExpression.Compile();
                updateAction = e =>
                {
                    var tmp = updateFunc(e);
                    foreach (var prop in properties)
                        prop.SetValue(e, prop.GetValue(tmp));
                };
            }

            foreach (var match in matches)
            {
                if (match.e2 == null)
                {
                    dbContext.Add(match.e1);
                    continue;
                }

                updateAction?.Invoke(match.e2);
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
