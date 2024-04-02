using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    public class SqliteUpsertCommandRunnerTests : RelationalCommandRunnerTestsBase<SqliteUpsertCommandRunner>
    {
        public SqliteUpsertCommandRunnerTests()
            : base("Microsoft.EntityFrameworkCore.Sqlite")
        { }

        protected override string NoUpdate_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO NOTHING";

        protected override string NoUpdate_Multiple_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3), (@p4, @p5, @p6, @p7) ON CONFLICT (\"ID\") " +
            "DO NOTHING";

        protected override string NoUpdate_WithNullable_Sql =>
            "INSERT INTO \"TestEntityWithNullableKey\" AS \"T\" (\"ID\", \"ID1\", \"ID2\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3, @p4, @p5) ON CONFLICT (\"ID1\", \"ID2\") " +
            "DO NOTHING";

        protected override string Update_Constant_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p1, @p2, @p3, @p4) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p0";

        protected override string Update_Constant_Multiple_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p1, @p2, @p3, @p4), (@p5, @p6, @p7, @p8) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p0";

        protected override string Update_Source_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = EXCLUDED.\"Name\"";

        protected override string Update_BinaryAdd_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p1, @p2, @p3, @p4) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Total\" = ( \"T\".\"Total\" + @p0 )";

        protected override string Update_Coalesce_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p1, @p2, @p3, @p4) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Status\" = ( COALESCE(\"T\".\"Status\", @p0) )";

        protected override string Update_BinaryAddMultiply_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p1, @p2, @p3, @p4) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Total\" = ( ( \"T\".\"Total\" + @p0 ) * EXCLUDED.\"Total\" )";

        protected override string Update_BinaryAddMultiplyGroup_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p1, @p2, @p3, @p4) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Total\" = ( \"T\".\"Total\" + ( @p0 * EXCLUDED.\"Total\" ) )";

        protected override string Update_Condition_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p2, @p3, @p4, @p5) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p0 " +
            "WHERE \"T\".\"Total\" > @p1";

        protected override string Update_Condition_UpdateConditionColumn_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p3, @p4, @p5, @p6) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p0, \"Total\" = ( \"T\".\"Total\" + @p1 ) " +
            "WHERE \"T\".\"Total\" > @p2";

        protected override string Update_Condition_AndCondition_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p2, @p3, @p4, @p5) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p0 " +
            "WHERE ( \"T\".\"Total\" > @p1 ) AND ( \"T\".\"Status\" != EXCLUDED.\"Status\" )";

        protected override string Update_Condition_NullCheck_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p1, @p2, @p3, @p4) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p0 " +
            "WHERE \"T\".\"Status\" IS NOT NULL";
    }
}
