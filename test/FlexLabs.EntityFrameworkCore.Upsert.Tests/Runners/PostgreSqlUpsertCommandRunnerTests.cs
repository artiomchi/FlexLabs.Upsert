using System.Collections.Generic;
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
        public PostgreSqlUpsertCommandRunnerTests()
            : base("Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var clrType = typeof(TestEntityWithIdentity);
            var entityType = _model.AddEntityType(clrType, ConfigurationSource.Convention);

            var idProperty = entityType.AddProperty(nameof(TestEntityWithIdentity.ID), ConfigurationSource.Explicit);
            entityType.AddKey(idProperty, ConfigurationSource.Convention);

            entityType.AddProperty(nameof(TestEntityWithIdentity.Name), ConfigurationSource.Explicit);
            var seqProperty = entityType.AddProperty(nameof(TestEntityWithIdentity.Sequence), ConfigurationSource.Explicit);
            seqProperty.SetOrRemoveAnnotation("Npgsql:ValueGenerationStrategy", 3);
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

        protected override string Update_Condition_NullCheck_Sql =>
            "INSERT INTO \"TestEntity\" AS \"T\" (\"ID\", \"Name\", \"Status\", \"Total\") " +
            "VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (\"ID\") " +
            "DO UPDATE SET \"Name\" = @p4 " +
            "WHERE \"T\".\"Status\" IS NOT NULL";


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
                Arg.Any<IEnumerable<object>>());
        }
    }
}
