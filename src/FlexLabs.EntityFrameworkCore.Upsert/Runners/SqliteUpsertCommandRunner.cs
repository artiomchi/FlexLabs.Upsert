using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the Microsoft.EntityFrameworkCore.Sqlite provider
    /// </summary>
    public class SqliteUpsertCommandRunner : PostgreSqlUpsertCommandRunner
    {
        public override bool Supports(string name) => name == "Microsoft.EntityFrameworkCore.Sqlite";
        protected override string GetSchema(IEntityType entityType) => null;
    }
}
