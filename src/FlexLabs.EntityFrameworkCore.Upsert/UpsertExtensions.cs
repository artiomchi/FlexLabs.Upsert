using System;
using System.Collections.Generic;
using System.Linq;
using FlexLabs.EntityFrameworkCore.Upsert;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Extension methods that provide access to upsert commands on a DbContext
    /// </summary>
    public static class UpsertExtensions
    {
        /// <summary>
        /// Attempt to insert an entity to the database, or update it if one already exists
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbContext">The data context used to connect to the database</param>
        /// <param name="entity">The entity that is being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> Upsert<TEntity>(this DbContext dbContext, TEntity entity)
            where TEntity : class
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return UpsertRange(dbContext, typeof(TEntity), entity);
        }

        /// <summary>
        /// Attempt to insert an entity to the database, or update it if one already exists
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbContext">The data context used to connect to the database</param>
        /// <param name="type">The anonymous type of the DbSet to be upserted</param>
        /// <param name="entity">The entity that is being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> Upsert<TEntity>(this DbContext dbContext, Type type, TEntity entity)
            where TEntity : class
        {
            return UpsertRange(dbContext, type, entity);
        }

        /// <summary>
        /// Attempt to insert an array of entities to the database, or update them if they already exist
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbContext">The data context used to connect to the database</param>
        /// <param name="type">The anonymous type of the DbSet to be upserted</param>
        /// <param name="entities">The entities that are being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> UpsertRange<TEntity>(this DbContext dbContext, Type type, params TEntity[] entities)
            where TEntity : class
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            return new UpsertCommandBuilder<TEntity>(dbContext, entities, type);
        }

        /// <summary>
        /// Attempt to insert an array of entities to the database, or update them if they already exist
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbContext">The data context used to connect to the database</param>
        /// <param name="entities">The entities that are being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> UpsertRange<TEntity>(this DbContext dbContext, params TEntity[] entities)
            where TEntity : class
        {
            return UpsertRange(dbContext, typeof(TEntity), entities);
        }

        /// <summary>
        /// Attempt to insert an array of entities to the database, or update them if they already exist
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbContext">The data context used to connect to the database</param>
        /// <param name="type">The anonymous type of the DbSet to be upserted</param>
        /// <param name="entities">The entities that are being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> UpsertRange<TEntity>(this DbContext dbContext, Type type, IEnumerable<TEntity> entities)
            where TEntity : class
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            ICollection<TEntity> collection;
            if (entities is ICollection<TEntity> entityCollection)
                collection = entityCollection;
            else
                collection = entities.ToArray();
            return new UpsertCommandBuilder<TEntity>(dbContext, collection, type);
        }

        /// <summary>
        /// Attempt to insert an array of entities to the database, or update them if they already exist
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbContext">The data context used to connect to the database</param>
        /// <param name="entities">The entities that are being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> UpsertRange<TEntity>(this DbContext dbContext, IEnumerable<TEntity> entities)
            where TEntity : class
        {
            return UpsertRange(dbContext, typeof(TEntity), entities);
        }

        /// <summary>
        /// Attempt to insert an entity to the database, or update it if one already exists
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbSet">The db set where the item will be upserted</param>
        /// <param name="entity">The entity that is being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> Upsert<TEntity>(this DbSet<TEntity> dbSet, TEntity entity)
            where TEntity : class
        {
            return Upsert(dbSet, typeof(TEntity), entity);
        }

        /// <summary>
        /// Attempt to insert an entity to the database, or update it if one already exists
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbSet">The db set where the item will be upserted</param>
        /// <param name="type">The anonymous type of the DbSet to be upserted</param>
        /// <param name="entity">The entity that is being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> Upsert<TEntity>(this DbSet<TEntity> dbSet, Type type, TEntity entity)
            where TEntity : class
        {
            if (dbSet == null)
                throw new ArgumentNullException(nameof(dbSet));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var dbContext = dbSet.GetService<ICurrentDbContext>().Context;
            return Upsert(dbContext, type, entity);
        }

        /// <summary>
        /// Attempt to insert an array of entities to the database, or update them if they already exist
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbSet">The db set where the items will be upserted</param>
        /// <param name="entities">The entities that are being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> UpsertRange<TEntity>(this DbSet<TEntity> dbSet, params TEntity[] entities)
            where TEntity : class
        {
            return UpsertRange(dbSet, typeof(TEntity), entities);
        }

        /// <summary>
        /// Attempt to insert an array of entities to the database, or update them if they already exist
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbSet">The db set where the items will be upserted</param>
        /// <param name="entities">The entities that are being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> UpsertRange<TEntity>(this DbSet<TEntity> dbSet, IEnumerable<TEntity> entities)
            where TEntity : class
        {
            return UpsertRange(dbSet, typeof(TEntity), entities);
        }

        /// <summary>
        /// Attempt to insert an array of entities to the database, or update them if they already exist
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbSet">The db set where the items will be upserted</param>
        /// <param name="type">The anonymous type of the DbSet to be upserted</param>
        /// <param name="entities">The entities that are being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> UpsertRange<TEntity>(this DbSet<TEntity> dbSet, Type type, params TEntity[] entities)
            where TEntity : class
        {
            if (dbSet == null)
                throw new ArgumentNullException(nameof(dbSet));
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var dbContext = dbSet.GetService<ICurrentDbContext>().Context;
            return UpsertRange(dbContext, type, entities);
        }

        /// <summary>
        /// Attempt to insert an array of entities to the database, or update them if they already exist
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbSet">The db set where the items will be upserted</param>
        /// <param name="type">The anonymous type of the DbSet to be upserted</param>
        /// <param name="entities">The entities that are being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        public static UpsertCommandBuilder<TEntity> UpsertRange<TEntity>(this DbSet<TEntity> dbSet, Type type, IEnumerable<TEntity> entities)
            where TEntity : class
        {
            if (dbSet == null)
                throw new ArgumentNullException(nameof(dbSet));
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var dbContext = dbSet.GetService<ICurrentDbContext>().Context;
            return UpsertRange(dbContext, type, entities);
        }
    }
}
