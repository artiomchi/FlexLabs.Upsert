using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert;

/// <summary>
/// Represents the arguments for an upsert command.
/// </summary>
public record class UpsertCommandArgs<TEntity>
{
    /// <summary>
    /// Specifies that if a match is found, no action will be taken on the entity
    /// </summary>
    public bool NoUpdate { get; init; }

    /// <summary>
    /// If true, will fallback to the (slower) expression compiler for unhandled update expressions
    /// </summary>
    public bool UseExpressionCompiler { get; init; }

    /// <summary>
    /// If true, allows matching entities on auto-generated columns
    /// </summary>
    public bool AllowIdentityMatch { get; init; }

    /// <summary>
    /// Original expression to use for matching existing entities (used to generate the match properties)
    /// </summary>
    public Expression<Func<TEntity, object>>? MatchExpressions { get; init; }

    /// <summary>
    /// Columns to use for matching existing entities
    /// </summary>
    public required IReadOnlyCollection<IProperty> MatchProperties { get; init; }

    /// <summary>
    /// Columns to exclude from update operations
    /// </summary>
    public required IReadOnlyCollection<IProperty> ExcludeProperties { get; init; }

    /// <summary>
    /// Expression that represents which properties will be updated, and what values will be set
    /// </summary>
    public required Expression<Func<TEntity, TEntity, TEntity>>? UpdateExpression { get; init; }

    /// <summary>
    /// Expression that checks whether the database entry should be updated
    /// </summary>
    public required Expression<Func<TEntity, TEntity, bool>>? UpdateCondition { get; init; }
}
