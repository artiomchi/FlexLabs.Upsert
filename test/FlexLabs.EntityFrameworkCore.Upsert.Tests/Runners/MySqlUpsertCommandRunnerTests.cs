using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    public class MySqlUpsertCommandRunnerTests : RelationalCommandRunnerTestsBase
    {
        protected override RelationalUpsertCommandRunner GetRunner() => new MySqlUpsertCommandRunner();

        protected override string NoUpdate_Sql =>
            "INSERT IGNORE INTO myTable (`Name`, `Status`) VALUES (@p0, @p1)";

        protected override string Update_Constant_Sql =>
            "INSERT INTO myTable (`Name`, `Status`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `Name` = @p2";

        protected override string Update_Source_Sql =>
            "INSERT INTO myTable (`Name`, `Status`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `Name` = VALUES(`Name`)";

        protected override string Update_Source_RenamedCol_Sql =>
            "INSERT INTO myTable (`Name`, `Status`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `Name` = VALUES(`Name2`)";

        protected override string Update_BinaryAdd_Sql =>
            "INSERT INTO myTable (`Name`, `Status`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `Status` = `Status` + @p2";

        protected override string Update_Coalesce_Sql =>
            "INSERT INTO myTable (`Name`, `Status`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `Status` = COALESCE(`Status`, @p2)";
    }
}
