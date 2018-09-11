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
    /// Upsert command runner for the Microsoft.EntityFrameworkCore.InMemory provider
    /// </summary>
    public class InMemoryUpsertCommandRunner : UpsertCommandRunnerBase
    {
        public override bool Supports(string providerName) => providerName == "Microsoft.EntityFrameworkCore.InMemory";

        public void RunCore<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>> updateExpression) where TEntity : class
        {
            // Find matching entities in the dbContext
            var matches = entities.AsQueryable()
                .GroupJoin(dbContext.Set<TEntity>(), matchExpression, matchExpression, (newEntity, dbEntity) => new { newEntity, dbEntity })
                .SelectMany(x => x.dbEntity.DefaultIfEmpty(), (x, dbEntity) => new { dbEntity, x.newEntity })
                .ToArray();

            Action<TEntity, TEntity> updateAction = null;
            if (updateExpression != null)
            {
                // If update expression is specified, create an update delegate based on that
                if (!(updateExpression.Body is MemberInitExpression entityUpdater))
                    throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updateExpression));

                var properties = entityUpdater.Bindings.Select(b => b.Member).OfType<PropertyInfo>();
                var updateFunc = updateExpression.Compile();
                updateAction = (dbEntity, newEntity) =>
                {
                    var tmp = updateFunc(dbEntity, newEntity);
                    foreach (var prop in properties)
                        prop.SetValue(dbEntity, prop.GetValue(tmp));
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
                updateAction = (dbEntity, newEntity) =>
                {
                    foreach (var prop in properties)
                        prop.SetValue(dbEntity, prop.GetValue(newEntity));
                };
            }

            foreach (var match in matches)
            {
                if (match.dbEntity == null)
                {
                    dbContext.Add(match.newEntity);
                    continue;
                }

                updateAction?.Invoke(match.dbEntity, match.newEntity);
            }
        }

        public override void Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>> updateExpression)
        {
            RunCore(dbContext, entityType, entities, matchExpression, updateExpression);
            dbContext.SaveChanges();
        }

        public override Task RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>> updateExpression, CancellationToken cancellationToken)
        {
            RunCore(dbContext, entityType, entities, matchExpression, updateExpression);
            return dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
