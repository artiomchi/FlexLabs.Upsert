using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NSubstitute;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners;

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
    }

    protected override void InitializeModel(Model model)
    {
        var sequenceProperty = AddEntity<TestEntityWithIdentity>(_model)
            .GetProperties().First(p => p.Name == "Sequence");
        sequenceProperty.SetAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);
    }

    [Fact]
    public void PostgresSyntaxRunner_NoUpdate_WithSequence()
    {
        _dbContext.Upsert(new TestEntityWithIdentity())
            .NoUpdate()
            .Run();

        AssertSql();
    }
}
