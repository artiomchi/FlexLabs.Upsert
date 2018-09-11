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
        public abstract string GenerateCommand(IEntityType entityType, ICollection<ICollection<(string ColumnName, ConstantValue Value)>> entities,
            ICollection<string> joinColumns, List<(string ColumnName, KnownExpression Value)> updateExpressions);
        protected abstract string EscapeName(string name);
        protected virtual string Parameter(int index) => "@p" + index;
        protected virtual string GetSchema(IEntityType entityType)
        {
            var schema = entityType.Relational().Schema;
            return schema != null
                ? EscapeName(schema) + "."
                : null;
        }
        protected abstract string SourcePrefix { get; }
        protected virtual string SourceSuffix => null;
        protected abstract string TargetPrefix { get; }
        protected virtual string TargetSuffix => null;

        private (string SqlCommand, IEnumerable<object> Arguments) PrepareCommand<TEntity>(IEntityType entityType, ICollection<TEntity> entities,
            Expression<Func<TEntity, object>> match, Expression<Func<TEntity, TEntity, TEntity>> updater, bool noUpdate)
        {
            var joinColumns = ProcessMatchExpression(entityType, match);
            var joinColumnNames = joinColumns.Select(c => c.Relational().ColumnName).ToArray();

            var properties = entityType.GetProperties()
                .Where(p => p.ValueGenerated == ValueGenerated.Never)
                .Select(p => (MetaProperty: p, PropertyInfo: typeof(TEntity).GetProperty(p.Name)))
                .Where(x => x.PropertyInfo != null)
                .ToList();
            var allColumns = properties.Select(x => x.MetaProperty.Relational().ColumnName).ToList();

            List<(IProperty Property, KnownExpression Value)> updateExpressions = null;
            if (updater != null)
            {
                if (!(updater.Body is MemberInitExpression entityUpdater))
                    throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updater));

                updateExpressions = new List<(IProperty Property, KnownExpression Value)>();
                foreach (MemberAssignment binding in entityUpdater.Bindings)
                {
                    var property = entityType.FindProperty(binding.Member.Name);
                    if (property == null)
                        throw new InvalidOperationException("Unknown property " + binding.Member.Name);

                    var value = binding.Expression.GetValue<TEntity>(updater);
                    if (!(value is KnownExpression knownExp))
                        knownExp = new KnownExpression(ExpressionType.Constant, new ConstantValue(value));

                    if (knownExp.Value1 is ParameterProperty epp1)
                        epp1.Property = entityType.FindProperty(epp1.PropertyName);
                    if (knownExp.Value2 is ParameterProperty epp2)
                        epp2.Property = entityType.FindProperty(epp2.PropertyName);
                    updateExpressions.Add((property, knownExp));
                }
            }
            else if (!noUpdate)
            {
                updateExpressions = new List<(IProperty Property, KnownExpression Value)>();
                foreach (var (MetaProperty, PropertyInfo) in properties)
                {
                    if (joinColumnNames.Contains(MetaProperty.Relational().ColumnName))
                        continue;

                    var propertyAccess = new ParameterProperty(MetaProperty.Name, false) { Property = MetaProperty };
                    var updateExpression = new KnownExpression(ExpressionType.MemberAccess, propertyAccess);
                    updateExpressions.Add((MetaProperty, updateExpression));
                }
            }

            var newEntities = entities
                .Select(e => properties
                    .Select(p =>
                    {
                        var columnName = p.MetaProperty.Relational().ColumnName;
                        var value = new ConstantValue(p.PropertyInfo.GetValue(e));
                        return (columnName, value);
                    })
                    .ToList() as ICollection<(string ColumnName, ConstantValue Value)>)
                .ToList();

            var arguments = newEntities.SelectMany(e => e.Select(p => p.Value)).ToList();
            if (updateExpressions != null)
                arguments.AddRange(updateExpressions.SelectMany(e => new[] { e.Value.Value1, e.Value.Value2 }).OfType<ConstantValue>());
            int i = 0;
            foreach (var arg in arguments)
                arg.ArgumentIndex = i++;

            var columnUpdateExpressions = updateExpressions?.Select(x => (x.Property.Relational().ColumnName, x.Value)).ToList();
            var sqlCommand = GenerateCommand(entityType, newEntities, joinColumnNames, columnUpdateExpressions);
            return (sqlCommand, arguments.Select(a => a.Value));
        }

        private string ExpandValue(IKnownValue value)
        {
            switch (value)
            {
                case ParameterProperty prop:
                    var prefix = prop.IsLeftParameter ? TargetPrefix : SourcePrefix;
                    var suffix = prop.IsLeftParameter ? TargetSuffix : SourceSuffix;
                    return prefix + EscapeName(prop.Property.Relational().ColumnName) + suffix;

                case ConstantValue constVal:
                    return Parameter(constVal.ArgumentIndex);

                default:
                    throw new InvalidOperationException();
            }
        }

        protected virtual string ExpandExpression(KnownExpression expression)
        {
            switch (expression.ExpressionType)
            {
                case ExpressionType.Add:
                case ExpressionType.Divide:
                case ExpressionType.Multiply:
                case ExpressionType.Subtract:
                    var left = ExpandValue(expression.Value1);
                    var right = ExpandValue(expression.Value2);
                    var op = GetSimpleOperator(expression.ExpressionType);
                    return $"{left} {op} {right}";

                case ExpressionType.MemberAccess:
                case ExpressionType.Constant:
                    return ExpandValue(expression.Value1);

                default: throw new NotSupportedException("Don't know how to process operation: " + expression.ExpressionType);
            }
        }

        protected virtual string GetSimpleOperator(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Add: return "+";
                case ExpressionType.Divide: return "/";
                case ExpressionType.Multiply: return "*";
                case ExpressionType.Subtract: return "-";
                default: throw new InvalidOperationException($"{expressionType} is not a simple arithmetic operation");
            }
        }

        public override void Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>> updateExpression, bool noUpdate)
        {
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities, matchExpression, updateExpression, noUpdate);
            dbContext.Database.ExecuteSqlCommand(sqlCommand, arguments);
        }

        public override Task RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, Expression<Func<TEntity, object>> matchExpression,
            Expression<Func<TEntity, TEntity, TEntity>> updateExpression, bool noUpdate, CancellationToken cancellationToken)
        {
            var (sqlCommand, arguments) = PrepareCommand(entityType, entities, matchExpression, updateExpression, noUpdate);
            return dbContext.Database.ExecuteSqlCommandAsync(sqlCommand, arguments);
        }
    }
}
