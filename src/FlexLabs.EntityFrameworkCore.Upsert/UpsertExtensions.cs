using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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

        public static TEntity Upsert<TEntity>(this DbSet<TEntity> dataSet, Expression<Func<TEntity>> initialiser, Expression<Func<TEntity>> match, Expression<Func<TEntity>> updater)
            where TEntity : class
        {
            if (initialiser == null)
                throw new ArgumentNullException(nameof(initialiser));
            if (match == null)
                throw new ArgumentNullException(nameof(initialiser));
            if (!(initialiser.Body is MemberInitExpression entityInitialiser))
                throw new ArgumentException("initialiser must be an Initialiser of the TEntity type", nameof(initialiser));
            if (!(match.Body is MemberInitExpression matchInitialiser))
                throw new ArgumentException("match must be an Initialiser of the TEntity type", nameof(match));
            MemberInitExpression entityUpdater = null;
            if (updater != null)
                if (!(updater.Body is MemberInitExpression entityUpdaterInner))
                    throw new ArgumentException("updater must be an Initialiser of the TEntity type", nameof(updater));
                else
                    entityUpdater = entityUpdaterInner;


            var initColumns = new Dictionary<string, Object>();
            var matchColumns = new Dictionary<string, Object>();
            var updColumns = new Dictionary<string, Object>();

            foreach (MemberAssignment binding in entityInitialiser.Bindings)
            {
                var columnName = GetColumnName(binding.Member as PropertyInfo);
                var value = binding.Expression.GetValue();
                initColumns[columnName] = value;
            }

            foreach (MemberAssignment binding in matchInitialiser.Bindings)
            {
                var columnName = GetColumnName(binding.Member as PropertyInfo);
                var value = binding.Expression.GetValue();
                matchColumns[columnName] = value;
            }

            if (entityUpdater != null)
                foreach (MemberAssignment binding in entityUpdater.Bindings)
                {
                    var columnName = GetColumnName(binding.Member as PropertyInfo);
                    var value = binding.Expression.GetValue();
                    updColumns[columnName] = value;
                }

            var commandGenerator = dataSet.GetService<IUpsertSqlGenerator>();
            if (commandGenerator == null)
            {
                var query = dataSet.AsQueryable();
                foreach (var matchEntry in matchColumns)
                    query = query.WhereEquals(matchEntry.Key, matchEntry.Value);
                var entity = query.FirstOrDefault();
                if (entity != null)
                {
                    if (updColumns != null)
                    {
                        foreach (var col in updColumns)
                        {
                            var prop = typeof(TEntity).GetProperty(col.Key);
                            prop.SetValue(entity, col.Value);
                        }
                        dataSet.Update(entity);
                    }
                    return entity;
                }

                entity = initialiser.Compile().Invoke();
                if (updColumns != null)
                {
                    foreach (var col in updColumns.Where(c => !initColumns.ContainsKey(c.Key)))
                    {
                        var prop = typeof(TEntity).GetProperty(col.Key);
                        prop.SetValue(entity, col.Value);
                    }
                    dataSet.Add(entity);
                }
                return entity;
            }

            throw new NotImplementedException();
        }
    }
}
