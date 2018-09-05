using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    public abstract class RelationalUpsertCommandRunner : UpsertCommandRunnerBase
    {
        public abstract string GenerateCommand(IEntityType entityType, int entityCount, ICollection<string> insertColumns,
            ICollection<string> joinColumns, ICollection<string> updateColumns,
            List<(string ColumnName, KnownExpressions Value)> updateExpressions);

        private (string SqlCommand, IEnumerable<object> Arguments) PrepareCommand<TEntity>(IEntityType entityType, IReadOnlyList<TEntity> entities,
            Expression<Func<TEntity, object>> match, Expression<Func<TEntity, TEntity>> updater) where TEntity : class
        {
            var joinColumns = ProcessMatchExpression(entityType, match);

            List<(IProperty, KnownExpressions)> updateExpressions = null;
            List<(IProperty, object)> updateValues = null;
            if (updater != null)
            {
                if (!(updater.Body is MemberInitExpression entityUpdater))
                    throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updater));

                updateExpressions = new List<(IProperty, KnownExpressions)>();
                updateValues = new List<(IProperty, object)>();
                foreach (var memberBinding in entityUpdater.Bindings)
                {
                    var binding = (MemberAssignment) memberBinding;
                    var property = entityType.FindProperty(binding.Member.Name);
                    if (property == null)
                        throw new InvalidOperationException("Unknown property " + binding.Member.Name);
                    var value = binding.Expression.GetValue();
                    if (value is KnownExpressions knownExp && typeof(TEntity) == knownExp.SourceType && knownExp.SourceProperty == binding.Member.Name)
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

            var updExpressions = new List<(string ColumnName, KnownExpressions Value)>();
            if (updateExpressions != null)
            {
                foreach (var (Property, Value) in updateExpressions)
                {
                    updExpressions.Add((Property.Relational().ColumnName, Value));
                }
            }

            var allArguments = arguments.Concat(updArguments).Concat(updExpressions.Select(e => e.Value.Value)).ToList();
            return (GenerateCommand(entityType, entities.Count, allColumns, joinColumnNames, updColumns, updExpressions), allArguments);
        }

        public override void Run<TEntity>(DbContext dbContext, IEntityType entityType, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity>> updateExpression)
        {
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities.ToList(), matchExpression, updateExpression);
            dbContext.Database.ExecuteSqlCommand(sqlCommand, arguments);
        }

        public override Task RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity>> updateExpression, CancellationToken cancellationToken)
        {
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities.ToList(), matchExpression, updateExpression);
            return dbContext.Database.ExecuteSqlCommandAsync(sqlCommand, arguments, cancellationToken);
        }
    }
}
