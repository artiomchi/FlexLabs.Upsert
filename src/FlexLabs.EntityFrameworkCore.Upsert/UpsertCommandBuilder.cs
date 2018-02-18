using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using FlexLabs.EntityFrameworkCore.Upsert.Generators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    public class UpsertCommandBuilder<TEntity> where TEntity : class
    {
        private readonly DbContext _dbContext;
        private readonly IEntityType _entityType;
        private readonly TEntity _entity;
        private IList<IProperty> _joinColumns;
        private IList<(IProperty Property, KnownExpressions Value)> _updateExpressions;
        private IList<(IProperty Property, object Value)> _updateValues;

        internal UpsertCommandBuilder(DbContext dbContext, IEntityType entityType, TEntity entity)
        {
            _dbContext = dbContext;
            _entityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public UpsertCommandBuilder<TEntity> On(Expression<Func<TEntity, object>> match)
        {
            if (_joinColumns != null)
                throw new InvalidOperationException($"Can't call {nameof(On)} twice!");
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            if (!(match.Body is NewExpression matchExpression))
                throw new ArgumentException("match must be an anonymous object initialiser", nameof(match));

            _joinColumns = new List<IProperty>();
            foreach (MemberExpression arg in matchExpression.Arguments)
            {
                if (arg == null || !(arg.Member is PropertyInfo) || !typeof(TEntity).Equals(arg.Expression.Type))
                    throw new InvalidOperationException("Match columns have to be properties of the TEntity class");
                var property = _entityType.FindProperty(arg.Member.Name);
                if (property == null)
                    throw new InvalidOperationException("Unknown property " + arg.Member.Name);
                _joinColumns.Add(property);
            }

            return this;
        }

        public UpsertCommandBuilder<TEntity> UpdateColumns(Expression<Func<TEntity, TEntity>> updater)
        {
            if (_updateValues != null)
                throw new InvalidOperationException($"Can't call {nameof(UpdateColumns)} twice!");
            if (updater == null)
                throw new ArgumentNullException(nameof(updater));
            if (!(updater.Body is MemberInitExpression entityUpdater))
                throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updater));

            _updateExpressions = new List<(IProperty, KnownExpressions)>();
            _updateValues = new List<(IProperty, object)>();
            foreach (MemberAssignment binding in entityUpdater.Bindings)
            {
                var property = _entityType.FindProperty(binding.Member.Name);
                if (property == null)
                    throw new InvalidOperationException("Unknown property " + binding.Member.Name);
                var value = binding.Expression.GetValue();
                if (value is KnownExpressions knownExp && typeof(TEntity).Equals(knownExp.SourceType) && knownExp.SourceProperty == binding.Member.Name)
                    _updateExpressions.Add((property, knownExp));
                else
                    _updateValues.Add((property, value));
            }

            return this;
        }

        private (string SqlCommand, IEnumerable<object> Arguments) PrepareCommand()
        {
            var arguments = new List<object>();
            var allColumns = new List<string>();
            foreach (var prop in _entityType.GetProperties())
            {
                if (prop.ValueGenerated != ValueGenerated.Never)
                    continue;
                var classProp = typeof(TEntity).GetProperty(prop.Name);
                if (classProp == null)
                    continue;
                allColumns.Add(prop.Relational().ColumnName);
                arguments.Add(classProp.GetValue(_entity));
            }

            var joinColumns = _joinColumns.Select(c => c.Relational().ColumnName).ToArray();

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
                    if (joinColumns.Contains(allColumns[i]))
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

            IUpsertSqlGenerator sqlGenerator = null;
            var dbProvider = _dbContext.GetService<IDatabaseProvider>();
            var generators = _dbContext.GetInfrastructure().GetServices<IUpsertSqlGenerator>().Concat(DefaultGenerators.Generators);
            foreach (var generator in generators)
                if (generator.Supports(dbProvider.Name))
                {
                    sqlGenerator = generator;
                    break;
                }
            if (sqlGenerator == null)
                throw new NotSupportedException("Database provider not supported yet!");

            var allArguments = arguments.Concat(updArguments).Concat(updExpressions.Select(e => e.Value.Value)).ToList();
            return (sqlGenerator.GenerateCommand(_entityType, allColumns, joinColumns, updColumns, updExpressions), allArguments);
        }

        public void Run()
        {
            var (sqlCommand, arguments) = PrepareCommand();
            _dbContext.Database.ExecuteSqlCommand(sqlCommand, arguments);
        }

        public Task RunAsync()
        {
            var (sqlCommand, arguments) = PrepareCommand();
            return _dbContext.Database.ExecuteSqlCommandAsync(sqlCommand, arguments);
        }
    }
}
