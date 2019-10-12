using System.Diagnostics.CodeAnalysis;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Options to configure the query behaviour
    /// </summary>
    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Only used to pass query options, so will never be compared to anything")]
    public struct RunnerQueryOptions
    {
        /// <summary>
        /// Specifies that if a match is found, no action will be taken on the entity
        /// </summary>
        public bool NoUpdate { get; internal set; }

        /// <summary>
        /// If true, will fallback to the (slower) expression compiler for unhandled update expressions
        /// </summary>
        public bool UseExpressionCompiler { get; internal set; }

        /// <summary>
        /// If true, allows matching entities on auto-generated columns
        /// </summary>
        public bool AllowIdentityMatch { get; internal set; }
    }
}
