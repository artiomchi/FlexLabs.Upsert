using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners;

/// <summary>
/// Upsert command runner base interface
/// </summary>
public interface IUpsertCommandRunner
{
    /// <summary>
    /// Specifies whether this command runner supports a specific database provider
    /// </summary>
    /// <param name="providerName">Name of the database provider</param>
    /// <returns>true if this runner supports the database provider specified; otherwise false</returns>
    bool Supports(string providerName);

    /// <summary>
    /// Run the upsert command for the entities passed
    /// </summary>
    /// <typeparam name="TEntity">Entity type of the entities</typeparam>
    /// <param name="dbContext">Data context to be used</param>
    /// <param name="entityType">Metadata for the entity</param>
    /// <param name="entities">Array of entities to be upserted</param>
    /// <param name="commandArgs">Arguments for the upsert command</param>
    int Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, UpsertCommandArgs<TEntity> commandArgs)
        where TEntity : class;

    /// <summary>
    /// Run the upsert command for the entities passed and return new or updated entities
    /// </summary>
    /// <typeparam name="TEntity">Entity type of the entities</typeparam>
    /// <param name="dbContext">Data context to be used</param>
    /// <param name="entityType">Metadata for the entity</param>
    /// <param name="entities">Array of entities to be upserted</param>
    /// <param name="commandArgs">Arguments for the upsert command</param>
    ICollection<TEntity> RunAndReturn<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, UpsertCommandArgs<TEntity> commandArgs)
        where TEntity : class;

    /// <summary>
    /// Run the upsert command for the entities passed
    /// </summary>
    /// <typeparam name="TEntity">Entity type of the entities</typeparam>
    /// <param name="dbContext">Data context to be used</param>
    /// <param name="entityType">Metadata for the entity</param>
    /// <param name="entities">Array of entities to be upserted</param>
    /// <param name="commandArgs">Arguments for the upsert command</param>
    /// <param name="cancellationToken">The CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>The task that represents the asynchronous upsert operation</returns>
    Task<int> RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, UpsertCommandArgs<TEntity> commandArgs, CancellationToken cancellationToken)
        where TEntity : class;

    /// <summary>
    /// Run the upsert command for the entities passed and return new or updated entities
    /// </summary>
    /// <typeparam name="TEntity">Entity type of the entities</typeparam>
    /// <param name="dbContext">Data context to be used</param>
    /// <param name="entityType">Metadata for the entity</param>
    /// <param name="entities">Array of entities to be upserted</param>
    /// <param name="commandArgs">Arguments for the upsert command</param>
    /// <param name="cancellationToken">The CancellationToken to observe while waiting for the task to complete.</param>
    Task<ICollection<TEntity>> RunAndReturnAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, UpsertCommandArgs<TEntity> commandArgs, CancellationToken cancellationToken)
        where TEntity : class;
}
