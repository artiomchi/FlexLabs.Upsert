using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests
{
    public class ReplaceRunnerTests
    {
        public class CustomSqlCommandRunner : RelationalUpsertCommandRunner
        {
            protected override string SourcePrefix => null;
            protected override string TargetPrefix => null;
            protected override string EscapeName(string name) => name;
            public override bool Supports(string name) => name == "Microsoft.EntityFrameworkCore.SqlServer";

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

        [Fact]
        public void Test()
        {
            var services = new ServiceCollection();

            services
                .AddDbContext<TestContext>(builder => builder
                    .UseSqlServer("Server=(localdb)\\MSSqlLocalDB;User=bad")
                    .ReplaceUpsertCommandRunner<CustomSqlCommandRunner>());

            var provider = services.BuildServiceProvider();

            using var context = provider.GetRequiredService<TestContext>();

            CustomSqlCommandRunner.GenerateCalled = 0;

            try
            {
                context.Entities.Upsert(new TestEntity())
                    .On(e => e.Value)
                    .Run();
            }
            // Not a real connection string, so it should throw a sql exception, but we don't care about it here
            catch (Microsoft.Data.SqlClient.SqlException) { }

            Assert.True(CustomSqlCommandRunner.GenerateCalled > 0);
        }
    }
}
