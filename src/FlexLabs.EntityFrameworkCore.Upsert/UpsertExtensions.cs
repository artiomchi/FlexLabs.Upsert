using FlexLabs.EntityFrameworkCore.Upsert;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore
{
    public static class UpsertExtensions
    {
        public static UpsertCommandBuilder<TEntity> Upsert<TEntity>(this DbContext dbContext, TEntity entity)
            where TEntity : class
        {
            var entityType = dbContext.GetService<IModel>().FindEntityType(typeof(TEntity));
            return new UpsertCommandBuilder<TEntity>(dbContext, entityType, entity);
        }

        public static UpsertCommandBuilder<TEntity> Upsert<TEntity>(this DbSet<TEntity> dbSet, TEntity entity)
            where TEntity : class
        {
            var dbContext = dbSet.GetService<ICurrentDbContext>().Context;
            return Upsert(dbContext, entity);
        }
    }
}
