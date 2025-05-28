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
        /// <inheritdoc/>
        public override bool Supports(string providerName) => providerName == "Microsoft.EntityFrameworkCore.InMemory";

        private static IEnumerable<TEntity> RunCore<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>>? matchExpression, Expression<Func<TEntity, object>>? excludeExpression,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions) where TEntity : class
        {
            // Find matching entities in the dbContext
            var matches = FindMatches(entityType, entities, dbContext, matchExpression);

            Action<TEntity, TEntity>? updateAction = null;
            Func<TEntity, TEntity, bool>? updateTest = updateCondition?.Compile();
            if (updateExpression != null)
            {
                // If update expression is specified, create an update delegate based on that
                if (updateExpression.Body is not MemberInitExpression entityUpdater)
                    throw new ArgumentException(Resources.FormatArgumentMustBeAnInitialiserOfTheTEntityType("updater"), nameof(updateExpression));

                var properties = entityUpdater.Bindings.Select(b => b.Member).OfType<PropertyInfo>();
                var updateFunc = updateExpression.Compile();
                updateAction = (dbEntity, newEntity) =>
                {
                    var tmp = updateFunc(dbEntity, newEntity);
                    foreach (var prop in properties)
                    {
                        var property = entityType.FindProperty(prop.Name);
                        prop.SetValue(dbEntity, prop.GetValue(tmp) ?? property?.GetDefaultValue());
                    }
                };
            }
            else if (!queryOptions.NoUpdate)
            {
                // Otherwise create a default update delegate that updates all non match, non auto generated, non excluded columns
                var joinColumns = ProcessMatchExpression(entityType, matchExpression, queryOptions);
                var excludeColumns = ProcessExcludeExpression(entityType, excludeExpression);

                var properties = entityType.GetProperties()
                    .Where(p => p.ValueGenerated == ValueGenerated.Never)
                    .Select(p => typeof(TEntity).GetProperty(p.Name))
                    .Where(p => p != null)
                    .Except(joinColumns.Select(c => c.PropertyInfo))
                    .Except(excludeColumns.Select(c => c.PropertyInfo));
                updateAction = (dbEntity, newEntity) =>
                {
                    foreach (var prop in properties)
                    {
                        var property = entityType.FindProperty(prop!.Name);
                        prop.SetValue(dbEntity, prop.GetValue(newEntity) ?? property?.GetDefaultValue());
                    }
                };
            }

            foreach (var match in matches)
            {
                if (match.DbEntity == null)
                {
                    foreach (var prop in typeof(TEntity).GetProperties())
                    {
                        if (prop.GetValue(match.NewEntity) == null)
                        {
                            var property = entityType.FindProperty(prop.Name);
                            if (property != null)
                            {
                                var defaultValue = property.GetDefaultValue();
                                if (defaultValue != null)
                                {
                                    prop.SetValue(match.NewEntity, defaultValue);
                                }
                            }
                        }
                    }
                    dbContext.Add(match.NewEntity);
                    continue;
                }

                if (updateTest?.Invoke(match.DbEntity, match.NewEntity) == false)
                    continue;

                updateAction?.Invoke(match.DbEntity, match.NewEntity);
            }

            return matches.Select(m => m.DbEntity ?? m.NewEntity);
        }

        private struct EntityMatch<TEntity>
        {
            public EntityMatch(TEntity? dbEntity, TEntity newEntity)
            {
                DbEntity = dbEntity;
                NewEntity = newEntity;
            }

            public TEntity? DbEntity;
            public TEntity NewEntity;
        }

        private static EntityMatch<TEntity>[] FindMatches<TEntity>(IEntityType entityType, IEnumerable<TEntity> entities, DbContext dbContext,
            Expression<Func<TEntity, object>>? matchExpression) where TEntity : class
        {
            if (matchExpression != null)
                return entities.AsQueryable()
                    .GroupJoin(dbContext.Set<TEntity>().ToList(), matchExpression, matchExpression, (newEntity, dbEntity) => new { dbEntity, newEntity })
                    .SelectMany(x => x.dbEntity.DefaultIfEmpty(), (x, dbEntity) => new EntityMatch<TEntity>(dbEntity, x.newEntity))
                    .ToArray();

            // If we're resorting to matching on PKs, we'll have to load them manually
            var primaryKeyProperties = entityType.FindPrimaryKey()?.Properties;
            if (primaryKeyProperties == null)
                return [];

            object?[] getPKs(TEntity entity)
            {
                return primaryKeyProperties
                    .Select(p => p.PropertyInfo?.GetValue(entity))
                    .ToArray();
            }
            return entities
                .Select(e => new EntityMatch<TEntity>(dbContext.Find<TEntity>(getPKs(e)), e))
                .ToArray();
        }

        /// <inheritdoc/>
        public override int Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>>? matchExpression, Expression<Func<TEntity, object>>? excludeExpression,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(entityType);

            RunCore(dbContext, entityType, entities, matchExpression, excludeExpression, updateExpression, updateCondition, queryOptions);
            return dbContext.SaveChanges();
        }

        /// <inheritdoc/>
        public override ICollection<TEntity> RunAndReturn<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>>? matchExpression, Expression<Func<TEntity, object>>? excludeExpression, Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition,
            RunnerQueryOptions queryOptions)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(entityType);

            var result = RunCore(dbContext, entityType, entities, matchExpression, excludeExpression, updateExpression, updateCondition, queryOptions);
            dbContext.SaveChanges();

            return result.ToArray();
        }

        /// <inheritdoc/>
        public override Task<int> RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>>? matchExpression, Expression<Func<TEntity, object>>? excludeExpression,
            Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition, RunnerQueryOptions queryOptions,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(entityType);

            RunCore(dbContext, entityType, entities, matchExpression, excludeExpression, updateExpression, updateCondition, queryOptions);
            return dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<ICollection<TEntity>> RunAndReturnAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>>? matchExpression, Expression<Func<TEntity, object>>? excludeExpression, Expression<Func<TEntity, TEntity, TEntity>>? updateExpression, Expression<Func<TEntity, TEntity, bool>>? updateCondition,
            RunnerQueryOptions queryOptions)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(entityType);

            var result = RunCore(dbContext, entityType, entities, matchExpression, excludeExpression, updateExpression, updateCondition, queryOptions);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return result.ToArray();
        }
    }
}
