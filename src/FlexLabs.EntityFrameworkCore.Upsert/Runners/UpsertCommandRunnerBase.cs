using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners;

/// <summary>
/// Base class with common helper methods for upsert command runners
/// </summary>
public abstract class UpsertCommandRunnerBase : IUpsertCommandRunner
{
    /// <inheritdoc/>
    public abstract bool Supports(string providerName);

    /// <inheritdoc/>
    public abstract int Run<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities, UpsertCommandArgs<TEntity> commandArgs)
        where TEntity : class;

    /// <inheritdoc/>
    public abstract ICollection<TEntity> RunAndReturn<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
        UpsertCommandArgs<TEntity> commandArgs)
        where TEntity : class;

    /// <inheritdoc/>
    public abstract Task<int> RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
        UpsertCommandArgs<TEntity> commandArgs, CancellationToken cancellationToken) where TEntity : class;

    /// <inheritdoc/>
    public abstract Task<ICollection<TEntity>> RunAndReturnAsync<TEntity>(DbContext dbContext, IEntityType entityType, ICollection<TEntity> entities,
        UpsertCommandArgs<TEntity> commandArgs, CancellationToken cancellationToken)
        where TEntity : class;
}
