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
    /// <summary>
    /// Upsert command runner for the Microsoft.EntityFrameworkCore.InMemory provider
    /// </summary>
    public class InMemoryUpsertCommandRunner : UpsertCommandRunnerBase
    {
        public override bool Supports(string providerName) => providerName == "Microsoft.EntityFrameworkCore.InMemory";

        public void RunCore<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity>> updateExpression) where TEntity : class
        {
            // Find matching entities in the dbContext
            var matches = entities.AsQueryable()
                .GroupJoin(dbContext.Set<TEntity>(), matchExpression, matchExpression, (e1, e2) => new { e1, e2 })
                .SelectMany(x => x.e2.DefaultIfEmpty(), (x, e2) => new { x.e1, e2 })
                .ToArray();

            Action<TEntity, TEntity> updateAction = null;
            if (updateExpression != null)
            {
                // If update expression is specified, create an update delegate based on that
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
                // Otherwise create a default update delegate that updates all non match, non auto generated columns
                var joinColumns = ProcessMatchExpression(entityType, matchExpression);

                var properties = entityType.GetProperties()
                    .Where(p => p.ValueGenerated == ValueGenerated.Never)
                    .Select(p => typeof(TEntity).GetProperty(p.Name))
                    .Where(p => p != null)
                    .Except(joinColumns.Select(c => c.PropertyInfo));
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
        }

        public override void Run<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity>> updateExpression)
        {
            RunCore(dbContext, entityType, entities, matchExpression, updateExpression);
            dbContext.SaveChanges();
        }

        public override Task RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity>> updateExpression, CancellationToken cancellationToken)
        {
            RunCore(dbContext, entityType, entities, matchExpression, updateExpression);
            return dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
