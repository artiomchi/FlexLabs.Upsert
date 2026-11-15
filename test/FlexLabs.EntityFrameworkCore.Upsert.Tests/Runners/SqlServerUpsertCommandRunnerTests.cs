using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners;

public class SqlServerUpsertCommandRunnerTests : RelationalCommandRunnerTestsBase<SqlServerUpsertCommandRunner>
{
    public SqlServerUpsertCommandRunnerTests()
        : base("Microsoft.EntityFrameworkCore.SqlServer")
    { }

}
