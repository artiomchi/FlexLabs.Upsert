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
    /// Upsert command runner for the Microsoft.EntityFrameworkCore.InMemory provider
    /// </summary>
    public class InMemoryUpsertCommandRunner : UpsertCommandRunnerBase
    {
        /// <inheritdoc/>
        public override bool Supports(string providerName) => providerName == "Microsoft.EntityFrameworkCore.InMemory";

        private void RunCore<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>>? matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions) where TEntity : class
        {
            // Find matching entities in the dbContext
            var matches = FindMatches(entityType, entities, dbContext, matchExpression);

            Action<TEntity, TEntity>? updateAction = null;
            Func<TEntity, TEntity, bool>? updateTest = updateCondition?.Compile();
            if (updateExpression != null)
            {
                // If update expression is specified, create an update delegate based on that
                if (!(updateExpression.Body is MemberInitExpression entityUpdater))
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ArgumentMustBeAnInitialiserOfTheTEntityType, "updater"), nameof(updateExpression));

                var properties = entityUpdater.Bindings.Select(b => b.Member).OfType<PropertyInfo>();
                var updateFunc = updateExpression.Compile();
                updateAction = (dbEntity, newEntity) =>
                {
                    var tmp = updateFunc(dbEntity, newEntity);
                    foreach (var prop in properties)
                    {
                        var property = entityType.FindProperty(prop.Name);
                        prop.SetValue(dbEntity, prop.GetValue(tmp) ?? property.GetDefaultValue());
                    }
                };
            }
            else if (!queryOptions.NoUpdate)
            {
                // Otherwise create a default update delegate that updates all non match, non auto generated columns
                var joinColumns = ProcessMatchExpression(entityType, matchExpression, queryOptions);

                var properties = entityType.GetProperties()
                    .Where(p => p.ValueGenerated == ValueGenerated.Never)
                    .Select(p => typeof(TEntity).GetProperty(p.Name))
                    .Where(p => p != null)
                    .Except(joinColumns.Select(c => c.PropertyInfo));
                updateAction = (dbEntity, newEntity) =>
                {
                    foreach (var prop in properties)
                    {
                        var property = entityType.FindProperty(prop.Name);
                        prop.SetValue(dbEntity, prop.GetValue(newEntity) ?? property.GetDefaultValue());
                    }
                };
            }

            foreach (var (dbEntity, newEntity) in matches)
            {
                if (dbEntity == null)
                {
                    foreach (var prop in typeof(TEntity).GetProperties())
                    {
                        if (prop.GetValue(newEntity) == null)
                        {
                            var property = entityType.FindProperty(prop.Name);
                            if (property != null)
                            {
                                var defaultValue = property.GetDefaultValue();
                                if (defaultValue != null)
                                {
                                    prop.SetValue(newEntity, defaultValue);
                                }
                            }
                        }
                    }
                    dbContext.Add(newEntity);
                    continue;
                }

                if (updateTest?.Invoke(dbEntity, newEntity) == false)
                    continue;

                updateAction?.Invoke(dbEntity, newEntity);
            }
        }

        private ICollection<(TEntity dbEntity, TEntity newEntity)> FindMatches<TEntity>(IEntityType entityType, IEnumerable<TEntity> entities, DbContext dbContext,
            Expression<Func<TEntity, object>>? matchExpression) where TEntity : class
        {
            if (matchExpression != null)
                return entities.AsQueryable()
                    .GroupJoin(dbContext.Set<TEntity>(), matchExpression, matchExpression, (newEntity, dbEntity) => new { newEntity, dbEntity })
                    .SelectMany(x => x.dbEntity.DefaultIfEmpty(), (x, dbEntity) => new { dbEntity, x.newEntity })
                    .AsEnumerable()
                    .Select(x => (x.dbEntity, x.newEntity))
                    .ToArray();

            // If we're resorting to matching on PKs, we'll have to load them manually
            object[] getPKs(TEntity entity)
            {
                return entityType.FindPrimaryKey()
                    .Properties
                    .Select(p => p.PropertyInfo.GetValue(entity))
                    .ToArray();
            }
            return entities
                .Select(e => (dbContext.Find<TEntity>(getPKs(e)), e))
                .ToArray();
        }

        /// <inheritdoc/>
        public override int Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>>? matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));

            RunCore(dbContext, entityType, entities, matchExpression, updateExpression, updateCondition, queryOptions);
            return dbContext.SaveChanges();
        }

        /// <inheritdoc/>
        public override Task<int> RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>>? matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions,
            CancellationToken cancellationToken)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));

            RunCore(dbContext, entityType, entities, matchExpression, updateExpression, updateCondition, queryOptions);
            return dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
