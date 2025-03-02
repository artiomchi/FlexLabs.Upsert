using System.Collections.Generic;
using System.Linq;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NSubstitute;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    public class PostgreSqlUpsertCommandRunnerTests : RelationalCommandRunnerTestsBase<PostgreSqlUpsertCommandRunner>
    {
        private enum NpgsqlValueGenerationStrategy
        {
            None,
            SequenceHiLo,
            SerialColumn,
            IdentityAlwaysColumn,
            IdentityByDefaultColumn
        }

        public PostgreSqlUpsertCommandRunnerTests()
            : base("Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var sequenceProperty = AddEntity<TestEntityWithIdentity>(_model)
                .GetProperties().First(p => p.Name == "Sequence");
            sequenceProperty.SetAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);
        }

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
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p4";

        protected override string Update_Constant_Multiple_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3), (@p4, @p5, @p6, @p7) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p8";

        protected override string Update_Source_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = EXCLUDED.\"Name\"";

        protected override string Update_BinaryAdd_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Total\" = ( \"T\".\"Total\" + @p4 )";

        protected override string Update_Coalesce_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Status\" = ( COALESCE(\"T\".\"Status\", @p4) )";

        protected override string Update_BinaryAddMultiply_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Total\" = ( ( \"T\".\"Total\" + @p4 ) * EXCLUDED.\"Total\" )";

        protected override string Update_BinaryAddMultiplyGroup_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Total\" = ( \"T\".\"Total\" + ( @p4 * EXCLUDED.\"Total\" ) )";

        protected override string Update_Condition_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p4 " +
            "WHERE \"T\".\"Total\" > @p5";

        protected override string Update_Condition_UpdateConditionColumn_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p4, \"Total\" = ( \"T\".\"Total\" + @p5 ) " +
            "WHERE \"T\".\"Total\" > @p6";

        protected override string Update_Condition_AndCondition_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p4 " +
            "WHERE ( \"T\".\"Total\" > @p5 ) AND ( \"T\".\"Status\" != EXCLUDED.\"Status\" )";

        protected override string Update_Condition_NullCheck_AlsoNullValue_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p4 " +
            "WHERE \"T\".\"Status\" IS NOT NULL";

        protected override string Update_WatchWithNullCheck_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = ( CASE WHEN ( EXCLUDED.\"Name\" IS NULL ) THEN @p4 ELSE EXCLUDED.\"Name\" END )";

        protected string NoUpdate_WithSequence_Sql =>
            "INSERT INTO \"TestEntityWithIdentity\" AS \"T\" (\"ID\", \"Name\") " +
            "VALUES (@p0, @p1) ON CONFLICT (\"ID\") " +
            "DO NOTHING";

        [Fact]
        public void PostgresSyntaxRunner_NoUpdate_WithSequence()
        {
            _dbContext.Upsert(new TestEntityWithIdentity())
                .NoUpdate()
                .Run();

            _rawSqlBuilder.Received().Build(
                NoUpdate_WithSequence_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }
    }
}
