using System;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
#if !NOMYSQL
    public class DbTests_MySql : DbTestsBase, IClassFixture<DbTests_MySql.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : ContainerisedDatabaseInitializerFixture<MySqlContainer>
        {
            public override DbDriver DbDriver => DbDriver.MySQL;

            protected override MySqlContainer BuildContainer()
                => new MySqlBuilder().Build();

            protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
            {
                var connectionString = TestContainer?.GetConnectionString()
                    ?? throw new InvalidOperationException("Connection string was not initialised");
                builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }

        public DbTests_MySql(DatabaseInitializer contexts)
            : base(contexts)
        { }
    }
#endif
}
