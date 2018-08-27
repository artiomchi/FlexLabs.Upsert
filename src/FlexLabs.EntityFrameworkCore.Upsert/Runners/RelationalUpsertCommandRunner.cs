using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    public abstract class RelationalUpsertCommandRunner : IUpsertCommandRunner
    {
        public abstract bool Supports(string name);
        public abstract string GenerateCommand(IEntityType entityType, int entityCount, ICollection<string> insertColumns,
            ICollection<string> joinColumns, ICollection<string> updateColumns,
            List<(string ColumnName, KnownExpressions Value)> updateExpressions);

        private (string SqlCommand, IEnumerable<object> Arguments) PrepareCommand<TEntity>(IEntityType entityType, TEntity[] entities, IList<IProperty> joinColumns, IList<(IProperty Property, KnownExpressions Value)> updateExpressions, IList<(IProperty Property, object Value)> updateValues) where TEntity : class
        {
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

            var joinColumnNames = joinColumns.Select(c => c.Relational().ColumnName).ToArray();

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
            return (GenerateCommand(entityType, entities.Length, allColumns, joinColumnNames, updColumns, updExpressions), allArguments);
        }

        public void Run<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, IList<IProperty> joinColumns, IList<(IProperty Property, KnownExpressions Value)> updateExpressions, IList<(IProperty Property, object Value)> updateValues) where TEntity : class
        {
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities, joinColumns, updateExpressions, updateValues);
            dbContext.Database.ExecuteSqlCommand(sqlCommand, arguments);
        }

        public Task RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, IList<IProperty> joinColumns, IList<(IProperty Property, KnownExpressions Value)> updateExpressions, IList<(IProperty Property, object Value)> updateValues, CancellationToken cancellationToken) where TEntity : class
        {
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities, joinColumns, updateExpressions, updateValues);
            return dbContext.Database.ExecuteSqlCommandAsync(sqlCommand, arguments);
        }
    }
}
