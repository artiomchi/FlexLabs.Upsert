﻿using System.Linq;
using DotNet.Testcontainers.Containers;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
#if !NOPOSTGRES
    public class DbTests_Postgres : DbTestsBase, IClassFixture<DbTests_Postgres.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            public override DbDriver DbDriver => DbDriver.Postgres;

            protected override IContainer BuildContainer()
                => new PostgreSqlBuilder().Build();

            protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
            {
                var connectionString = (TestContainer as IDatabaseContainer)?.GetConnectionString()
                    ?? (BuildEnvironment.IsGitHub ? "Server=localhost;Port=5432;Database=testuser;Username=postgres;Password=root" : null);
                builder.UseNpgsql(new NpgsqlDataSourceBuilder(connectionString)
                    .EnableDynamicJsonMappings()
                    .Build());
            }
        }

        public DbTests_Postgres(DatabaseInitializer contexts)
            : base(contexts)
        { }

        [Fact]
        public void GeneratedAlwaysAsIdentity_NoUpdate_New()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new GeneratedAlwaysAsIdentity
            {
                Num1 = 1,
            };

            dbContext.GeneratedAlwaysAsIdentity.Upsert(newItem)
                .On(j => j.Num1)
                .NoUpdate()
                .Run();

            dbContext.GeneratedAlwaysAsIdentity.OrderBy(t => t.ID).Should().SatisfyRespectively(
                test => test.Should().MatchModel(new GeneratedAlwaysAsIdentity
                {
                    Num1 = 1,
                    Num2 = 1, // autogenerated
                }));
        }
    }
#endif
}
