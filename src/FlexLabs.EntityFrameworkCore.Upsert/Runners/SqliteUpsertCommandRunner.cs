using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the Microsoft.EntityFrameworkCore.Sqlite provider
    /// </summary>
    public class SqliteUpsertCommandRunner : PostgreSqlUpsertCommandRunner
    {
        /// <inheritdoc/>
        public override bool Supports(string name) => name == "Microsoft.EntityFrameworkCore.Sqlite";

        /// <summary>
        /// Sqlite doesn't support table schemas, so this method returns null
        /// </summary>
        /// <param name="entityType">The entity type of the table</param>
        /// <returns>null</returns>
        protected override string? GetSchema(IEntityType entityType) => null;
    }
}
