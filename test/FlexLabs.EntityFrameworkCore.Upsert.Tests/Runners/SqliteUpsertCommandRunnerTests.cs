using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    public class SqliteUpsertCommandRunnerTests : PostgreSqlUpsertCommandRunnerTests
    {
        protected override RelationalUpsertCommandRunner GetRunner() => new SqliteUpsertCommandRunner();
    }
}
