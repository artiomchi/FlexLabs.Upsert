using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    public class MySqlUpsertCommandRunnerTests : RelationalCommandRunnerTestsBase<MySqlUpsertCommandRunner>
    {
        public MySqlUpsertCommandRunnerTests()
            : base("MySql.Data.EntityFrameworkCore")
        { }

        protected override string NoUpdate_Sql =>
            "INSERT IGNORE " +
            "INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3)";

        protected override string NoUpdate_Multiple_Sql =>
            "INSERT IGNORE " +
            "INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3), (@p4, @p5, @p6, @p7)";

        protected override string NoUpdate_WithNullable_Sql =>
            "INSERT IGNORE " +
            "INTO `TestEntityWithNullableKey` (`ID`, `ID1`, `ID2`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3, @p4, @p5)";

        protected override string Update_Constant_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3) " +
            "ON DUPLICATE KEY UPDATE `Name` = @p4";

        protected override string Update_Constant_Multiple_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3), (@p4, @p5, @p6, @p7) " +
            "ON DUPLICATE KEY UPDATE `Name` = @p8";

        protected override string Update_Source_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3) " +
            "ON DUPLICATE KEY UPDATE `Name` = VALUES(`Name`)";

        protected override string Update_BinaryAdd_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3) " +
            "ON DUPLICATE KEY UPDATE `Total` = ( `Total` + @p4 )";

        protected override string Update_Coalesce_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3) " +
            "ON DUPLICATE KEY UPDATE `Status` = ( COALESCE(`Status`, @p4) )";

        protected override string Update_BinaryAddMultiply_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3) " +
            "ON DUPLICATE KEY UPDATE `Total` = ( ( `Total` + @p4 ) * VALUES(`Total`) )";

        protected override string Update_BinaryAddMultiplyGroup_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3) " +
            "ON DUPLICATE KEY UPDATE `Total` = ( `Total` + ( @p4 * VALUES(`Total`) ) )";

        protected override string Update_Condition_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3) " +
            "ON DUPLICATE KEY UPDATE `Name` = IF (`Total` > @p5, @p4, `Name`)";

        protected override string Update_Condition_UpdateConditionColumn_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3) " +
            "ON DUPLICATE KEY UPDATE " +
                "`Name` = COALESCE (IF ((@xTotal := `Total`) IS NOT NULL, NULL, NULL), IF (@xTotal > @p6, @p4, `Name`)), " +
                "`Total` = IF (@xTotal > @p6, ( `Total` + @p5 ), `Total`)";

        protected override string Update_Condition_AndCondition_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3) " +
            "ON DUPLICATE KEY UPDATE `Name` = IF (( `Total` > @p5 ) AND ( `Status` != VALUES(`Status`) ), @p4, `Name`)";

        protected override string Update_Condition_NullCheck_AlsoNullValue_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3) " +
            "ON DUPLICATE KEY UPDATE `Name` = IF (`Status` IS NOT NULL, @p4, `Name`)";

        protected override string Update_WatchWithNullCheck_Sql =>
            "INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`) " +
            "VALUES (@p0, @p1, @p2, @p3) " +
            "ON DUPLICATE KEY UPDATE `Name` = ( CASE WHEN ( VALUES(`Name`) IS NULL ) THEN @p4 ELSE VALUES(`Name`) END )";
    }
}
