using DotNet.Testcontainers.Containers;
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
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            public override DbDriver DbDriver => DbDriver.MySQL;

            protected override IContainer BuildContainer()
                => new MySqlBuilder().Build();

            protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
            {
                var connectionString = (TestContainer as IDatabaseContainer).GetConnectionString();
                builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }

        public DbTests_MySql(DatabaseInitializer contexts)
            : base(contexts)
        { }
    }
#endif
}
