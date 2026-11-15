using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners;

public class SqliteUpsertCommandRunnerTests : RelationalCommandRunnerTestsBase<SqliteUpsertCommandRunner>
{
    public SqliteUpsertCommandRunnerTests()
        : base("Microsoft.EntityFrameworkCore.Sqlite")
    { }

}
