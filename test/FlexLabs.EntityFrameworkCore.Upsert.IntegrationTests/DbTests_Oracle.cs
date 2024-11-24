using System;
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
        public sealed class DatabaseInitializer : ContainerisedDatabaseInitializerFixture<OracleContainer>
        {
            public override DbDriver DbDriver => DbDriver.Oracle;

            protected override OracleContainer BuildContainer()
                => new OracleBuilder().WithName("flexlabs_upsert_oracle").WithReuse(true).Build();

            protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
            {
                var connectionString = TestContainer?.GetConnectionString()
                    ?? throw new InvalidOperationException("Connection string was not initialised");
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
