using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners;

public class MySqlUpsertCommandRunnerTests : RelationalCommandRunnerTestsBase<MySqlUpsertCommandRunner>
{
    public MySqlUpsertCommandRunnerTests()
        : base("MySql.Data.EntityFrameworkCore")
    { }

}
