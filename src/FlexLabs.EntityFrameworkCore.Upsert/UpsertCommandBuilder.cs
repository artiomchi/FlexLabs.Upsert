using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    public class UpsertCommandBuilder<TEntity> where TEntity : class
    {
        private readonly DbContext _dbContext;
        private readonly IEntityType _entityType;
        private readonly TEntity _entity;
        private IList<IProperty> _joinColumns;
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

            _updateValues = new List<(IProperty, object)>();
            foreach (MemberAssignment binding in entityUpdater.Bindings)
            {
                var property = _entityType.FindProperty(binding.Member.Name);
                if (property == null)
                    throw new InvalidOperationException("Unknown property " + binding.Member.Name);
                var value = binding.Expression.GetValue();
                _updateValues.Add((property, value));
            }

            return this;
        }

        public void Run() => RunAsync().GetAwaiter().GetResult();

        public async Task RunAsync()
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
            foreach (var (Property, Value) in _updateValues)
            {
                updColumns.Add(Property.Relational().ColumnName);
                updArguments.Add(Value);
            }

            var allArguments = arguments.Concat(updArguments).ToList();
            var dbProvider = _dbContext.GetService<IDatabaseProvider>();
            string sqlCommand;
            if (dbProvider.Name.EndsWith("PostgreSQL"))
                sqlCommand = CreateCommand_PostgreSQL(allColumns, joinColumns, updColumns, allArguments);
            else if (dbProvider.Name.EndsWith("SqlServer"))
                sqlCommand = CreateCommand_MSSQL(allColumns, joinColumns, updColumns, allArguments);
            else
                throw new NotSupportedException("Database provider not supported yet!");
            await _dbContext.Database.ExecuteSqlCommandAsync(sqlCommand, allArguments);
        }

        private string CreateCommand_PostgreSQL(ICollection<string> columns, ICollection<string> joinColumns, ICollection<string> updateColumns, ICollection<object> arguments)
        {
            var result = new StringBuilder();
            result.Append($"INSERT INTO {_entityType.Relational().Schema ?? "public"}.\"{_entityType.Relational().TableName}\" AS \"T\" (");
            result.AppendJoin(", ", columns.Select(c => $"\"{c}\""));
            result.Append(") VALUES (");
            result.AppendJoin(", ", arguments.Take(columns.Count).Select((v, i) => $"@p{i}"));
            result.Append(") ON CONFLICT (");
            result.AppendJoin(", ", joinColumns.Select(c => $"\"{c}\""));
            result.Append(") DO UPDATE SET ");
            result.AppendJoin(", ", updateColumns.Select((c, i) => $"\"{c}\" = @p{i + columns.Count}"));
            return result.ToString();
        }

        private string CreateCommand_MSSQL(ICollection<string> columns, ICollection<string> joinColumns, ICollection<string> updateColumns, ICollection<object> arguments)
        {
            var result = new StringBuilder();
            result.Append($"MERGE INTO {_entityType.Relational().Schema ?? "dbo"}.[{_entityType.Relational().TableName}] AS [T] USING ( VALUES (");
            result.AppendJoin(", ", arguments.Take(columns.Count).Select((v, i) => $"@p{i}"));
            result.Append($") ) AS [S] (");
            result.AppendJoin(", ", columns.Select(c => $"[{c}]"));
            result.Append(") ON ");
            result.AppendJoin(" AND ", joinColumns.Select(c => $"[T].[{c}] = [S].[{c}]"));
            result.Append(" WHEN NOT MATCHED BY TARGET THEN INSERT (");
            result.AppendJoin(", ", columns.Select(c => $"[{c}]"));
            result.Append(") VALUES (");
            result.AppendJoin(", ", columns.Select(c => $"[{c}]"));
            result.Append(") WHEN MATCHED THEN UPDATE SET ");
            result.AppendJoin(", ", updateColumns.Select((c, i) => $"[{c}] = @p{i + columns.Count}"));
            return result.ToString();
        }
    }
}
