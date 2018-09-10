using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    public abstract class RelationalUpsertCommandRunner : UpsertCommandRunnerBase
    {
        public abstract string GenerateCommand(IEntityType entityType, int entityCount, ICollection<string> insertColumns,
            ICollection<string> joinColumns, ICollection<string> updateColumns,
            List<(string ColumnName, KnownExpression Value)> updateExpressions);

        private (string SqlCommand, IEnumerable<object> Arguments) PrepareCommand<TEntity>(IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>> match, Expression<Func<TEntity, TEntity>> updater)
        {
            var joinColumns = ProcessMatchExpression(entityType, match);

            List<(IProperty, KnownExpression)> updateExpressions = null;
            List<(IProperty, object)> updateValues = null;
            if (updater != null)
            {
                if (!(updater.Body is MemberInitExpression entityUpdater))
                    throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updater));

                updateExpressions = new List<(IProperty, KnownExpression)>();
                updateValues = new List<(IProperty, object)>();
                foreach (MemberAssignment binding in entityUpdater.Bindings)
                {
                    var property = entityType.FindProperty(binding.Member.Name);
                    if (property == null)
                        throw new InvalidOperationException("Unknown property " + binding.Member.Name);
                    var value = binding.Expression.GetValue<TEntity>(updater);
                    if (value is KnownExpression knownExp)
                        updateExpressions.Add((property, knownExp));
                    else
                        updateValues.Add((property, value));
                }
            }

            var arguments = new List<object>();
            var allColumns = new List<string>();
            var columnsDone = false;
            foreach (var entity in entities)
            {
                foreach (var prop in entityType.GetProperties())
                {
                    if (prop.ValueGenerated != ValueGenerated.Never)
                        continue;
                    var classProp = typeof(TEntity).GetProperty(prop.Name);
                    if (classProp == null)
                        continue;
                    if (!columnsDone)
                        allColumns.Add(prop.Relational().ColumnName);
                    arguments.Add(classProp.GetValue(entity));
                }
                columnsDone = true;
            }

            var joinColumnNames = joinColumns.Select(c => c.PropertyMetadata.Relational().ColumnName).ToArray();

            var updArguments = new List<object>();
            var updColumns = new List<string>();
            if (updateValues != null)
            {
                foreach (var (Property, Value) in updateValues)
                {
                    updColumns.Add(Property.Relational().ColumnName);
                    updArguments.Add(Value);
                }
            }
            else
            {
                for (int i = 0; i < allColumns.Count; i++)
                {
                    if (joinColumnNames.Contains(allColumns[i]))
                        continue;
                    updArguments.Add(arguments[i]);
                    updColumns.Add(allColumns[i]);
                }
            }

            var updExpressions = new List<(string ColumnName, KnownExpression Value)>();
            if (updateExpressions != null)
            {
                foreach (var (Property, Value) in updateExpressions)
                {
                    updExpressions.Add((Property.Relational().ColumnName, Value));
                }
            }

            var allArguments = arguments.Concat(updArguments).Concat(updExpressions.Select(e => e.Value.Value2)).ToList();
            return (GenerateCommand(entityType, entities.Count, allColumns, joinColumnNames, updColumns, updExpressions), allArguments);
        }

        public override void Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity>> updateExpression)
        {
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities, matchExpression, updateExpression);
            dbContext.Database.ExecuteSqlCommand(sqlCommand, arguments);
        }

        public override Task RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity>> updateExpression, CancellationToken cancellationToken)
        {
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities, matchExpression, updateExpression);
            return dbContext.Database.ExecuteSqlCommandAsync(sqlCommand, arguments);
        }
    }
}
