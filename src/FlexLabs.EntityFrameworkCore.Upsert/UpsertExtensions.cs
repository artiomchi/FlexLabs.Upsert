using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    public static class UpsertExtensions
    {
        public static UpsertCommandBuilder<TEntity> Upsert<TEntity>(this DbContext dbContext, TEntity entity)
            where TEntity : class
        {
            return UpsertRange(dbContext, entity);
        }

        public static UpsertCommandBuilder<TEntity> UpsertRange<TEntity>(this DbContext dbContext, params TEntity[] entities)
            where TEntity : class
        {
            var entityType = dbContext.GetService<IModel>().FindEntityType(typeof(TEntity));
            return new UpsertCommandBuilder<TEntity>(dbContext, entityType, entities);
        }
    }
}
