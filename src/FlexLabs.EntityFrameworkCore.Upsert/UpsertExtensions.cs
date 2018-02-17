using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    public static class UpsertExtensions
    {
        private static string GetColumnName(PropertyInfo property)
        {
            var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
            if (columnAttribute == null)
                return property.Name;
            return columnAttribute.Name;
        }

        public static void Upsert<TEntity>(this DbSet<TEntity> dataSet, Expression<Func<TEntity>> initialiser, Expression<Func<TEntity, object>> match, Expression<Func<TEntity>> updater)
            where TEntity : class
        {
            if (initialiser == null)
                throw new ArgumentNullException(nameof(initialiser));
            if (match == null)
                throw new ArgumentNullException(nameof(initialiser));
            if (!(initialiser.Body is MemberInitExpression entityInitialiser))
                throw new ArgumentException("initialiser must be an Initialiser of the TEntity type", nameof(initialiser));
            if (!(match.Body is NewExpression matchExpression))
                throw new ArgumentException("match must be an anonymous object initialiser", nameof(match));
            MemberInitExpression entityUpdater = null;
            if (updater != null)
                if (!(updater.Body is MemberInitExpression entityUpdaterInner))
                    throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updater));
                else
                    entityUpdater = entityUpdaterInner;


            var initColumns = new Dictionary<string, Object>();
            var updColumns = new Dictionary<string, Object>();
            var joinColumns = new List<string>();

            foreach (MemberAssignment binding in entityInitialiser.Bindings)
            {
                var columnName = GetColumnName(binding.Member as PropertyInfo);
                var value = binding.Expression.GetValue();
                initColumns[columnName] = value;
            }

            if (entityUpdater != null)
                foreach (MemberAssignment binding in entityUpdater.Bindings)
                {
                    var columnName = GetColumnName(binding.Member as PropertyInfo);
                    var value = binding.Expression.GetValue();
                    updColumns[columnName] = value;
                }

            foreach (MemberExpression arg in matchExpression.Arguments)
            {
                if (arg == null || arg.Member is PropertyInfo || typeof(TEntity).Equals(arg.Type))
                    throw new InvalidOperationException("Match columns have to be properties of the TEntity class");
                joinColumns.Add(GetColumnName(arg.Member as PropertyInfo));
            }
        }
    }
}
