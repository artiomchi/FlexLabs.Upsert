using System.Collections.Generic;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests
{
    public class ReplaceRunnerTests
    {
        public class CustomSqliteCommandRunner : RelationalUpsertCommandRunner
        {
            protected override string SourcePrefix => null;
            protected override string TargetPrefix => null;
            protected override string EscapeName(string name) => name;
            public override bool Supports(string name) => name == "Microsoft.EntityFrameworkCore.Sqlite";

            public static int GenerateCalled;
            public override string GenerateCommand(string tableName, ICollection<ICollection<(string ColumnName, ConstantValue Value, string DefaultSql, bool AllowInserts)>> entities,
                ICollection<(string ColumnName, bool IsNullable)> joinColumns, ICollection<(string ColumnName, IKnownValue Value)> updateExpressions, KnownExpression updateCondition)
            {
                GenerateCalled++;
                return "sql";
            }
        }

        public class TestEntity
        {
            public int ID { get; set; }
            public string Value { get; set; }
        }

        public class TestContext : DbContext
        {
            public TestContext(DbContextOptions<TestContext> options)
                : base(options)
            { }

            public DbSet<TestEntity> Entities { get; set; }
        }

#if !NOSQLITE
        [Fact]
#endif
        public void ReplaceRunner_FakeSqliteRunner()
        {
            var services = new ServiceCollection();

            services
                .AddDbContext<TestContext>(builder => builder
                    .UseSqlite("Data Source={Username}.db")
                    .ReplaceUpsertCommandRunner<CustomSqliteCommandRunner>());

            var provider = services.BuildServiceProvider();

            using var context = provider.GetRequiredService<TestContext>();

            CustomSqliteCommandRunner.GenerateCalled = 0;

            Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() =>
            {
                context.Entities.Upsert(new TestEntity())
                    .On(e => e.Value)
                    .Run();
            });
            Assert.True(CustomSqliteCommandRunner.GenerateCalled > 0);
        }
    }
}
