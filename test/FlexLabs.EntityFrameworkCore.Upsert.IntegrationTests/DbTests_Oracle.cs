using DotNet.Testcontainers.Containers;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Microsoft.EntityFrameworkCore;
using Testcontainers.Oracle;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
#if !NOORACLE
    public class DbTests_Oracle : DbTestsBase, IClassFixture<DbTests_Oracle.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            public override DbDriver DbDriver => DbDriver.Oracle;

            protected override IContainer BuildContainer()
                => new OracleBuilder().Build();

            protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
            {
                var connectionString = (TestContainer as IDatabaseContainer)?.GetConnectionString();
                builder
                    .UseOracle(connectionString)
                    .UseUpperSnakeCaseNamingConvention();
            }
        }

        public DbTests_Oracle(DatabaseInitializer contexts)
            : base(contexts)
        {
        }
    }
#endif
}
