using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners;

public class OracleUpsertCommandRunnerTests : RelationalCommandRunnerTestsBase<OracleUpsertCommandRunner>
{
    public OracleUpsertCommandRunnerTests()
        : base("Oracle.EntityFrameworkCore")
    {
    }

}
