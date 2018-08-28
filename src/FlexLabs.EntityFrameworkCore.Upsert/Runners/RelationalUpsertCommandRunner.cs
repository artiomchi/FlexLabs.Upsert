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
    public abstract class RelationalUpsertCommandRunner : IUpsertCommandRunner
    {
        private IList<IProperty> _joinColumns;
        private IList<(IProperty Property, KnownExpressions Value)> _updateExpressions;
        private IList<(IProperty Property, object Value)> _updateValues;

        public abstract bool Supports(string name);
        public abstract string GenerateCommand(IEntityType entityType, int entityCount, ICollection<string> insertColumns,
            ICollection<string> joinColumns, ICollection<string> updateColumns,
            List<(string ColumnName, KnownExpressions Value)> updateExpressions);

        private void ProcessExpressions<TEntity>(IEntityType entityType, Expression<Func<TEntity, object>> match, Expression<Func<TEntity, TEntity>> updater)
        {
            if (match.Body is NewExpression newExpression)
            {
                _joinColumns = new List<IProperty>();
                foreach (MemberExpression arg in newExpression.Arguments)
                {
                    if (arg == null || !(arg.Member is PropertyInfo) || !typeof(TEntity).Equals(arg.Expression.Type))
                        throw new InvalidOperationException("Match columns have to be properties of the TEntity class");
                    var property = entityType.FindProperty(arg.Member.Name);
                    if (property == null)
                        throw new InvalidOperationException("Unknown property " + arg.Member.Name);
                    _joinColumns.Add(property);
                }
            }
            else if (match.Body is UnaryExpression unaryExpression)
            {
                if (!(unaryExpression.Operand is MemberExpression memberExp) || !typeof(TEntity).Equals(memberExp.Expression.Type))
                    throw new InvalidOperationException("Match columns have to be properties of the TEntity class");
                var property = entityType.FindProperty(memberExp.Member.Name);
                _joinColumns = new List<IProperty> { property };
            }
            else if (match.Body is MemberExpression memberExpression)
            {
                if (!typeof(TEntity).Equals(memberExpression.Expression.Type))
                    throw new InvalidOperationException("Match columns have to be properties of the TEntity class");
                var property = entityType.FindProperty(memberExpression.Member.Name);
                _joinColumns = new List<IProperty> { property };
            }
            else
            {
                throw new ArgumentException("match must be an anonymous object initialiser", nameof(match));
            }

            if (updater != null)
            {
                if (!(updater.Body is MemberInitExpression entityUpdater))
                    throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updater));

                _updateExpressions = new List<(IProperty, KnownExpressions)>();
                _updateValues = new List<(IProperty, object)>();
                foreach (MemberAssignment binding in entityUpdater.Bindings)
                {
                    var property = entityType.FindProperty(binding.Member.Name);
                    if (property == null)
                        throw new InvalidOperationException("Unknown property " + binding.Member.Name);
                    var value = binding.Expression.GetValue();
                    if (value is KnownExpressions knownExp && typeof(TEntity).Equals(knownExp.SourceType) && knownExp.SourceProperty == binding.Member.Name)
                        _updateExpressions.Add((property, knownExp));
                    else
                        _updateValues.Add((property, value));
                }
            }
        }

        private (string SqlCommand, IEnumerable<object> Arguments) PrepareCommand<TEntity>(IEntityType entityType, TEntity[] entities) where TEntity : class
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

            var joinColumnNames = _joinColumns.Select(c => c.Relational().ColumnName).ToArray();

            var updArguments = new List<object>();
            var updColumns = new List<string>();
            if (_updateValues != null)
            {
                foreach (var (Property, Value) in _updateValues)
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
            if (_updateExpressions != null)
            {
                foreach (var (Property, Value) in _updateExpressions)
                {
                    updExpressions.Add((Property.Relational().ColumnName, Value));
                }
            }

            var allArguments = arguments.Concat(updArguments).Concat(updExpressions.Select(e => e.Value.Value)).ToList();
            return (GenerateCommand(entityType, entities.Length, allColumns, joinColumnNames, updColumns, updExpressions), allArguments);
        }

        public void Run<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, Expression<Func<TEntity, object>> matchExpression, Expression<Func<TEntity, TEntity>> updateExpression) where TEntity : class
        {
            ProcessExpressions(entityType, matchExpression, updateExpression);
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities);
            dbContext.Database.ExecuteSqlCommand(sqlCommand, arguments);
        }

        public Task RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, Expression<Func<TEntity, object>> matchExpression, Expression<Func<TEntity, TEntity>> updateExpression, CancellationToken cancellationToken) where TEntity : class
        {
            ProcessExpressions(entityType, matchExpression, updateExpression);
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities);
            return dbContext.Database.ExecuteSqlCommandAsync(sqlCommand, arguments);
        }
    }
}
