using DotNet.Testcontainers.Containers;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
#if !NOMSSQL
    public class DbTests_SqlServer : DbTestsBase, IClassFixture<DbTests_SqlServer.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            public override DbDriver DbDriver => DbDriver.MSSQL;

            protected override IContainer BuildContainer()
                => new MsSqlBuilder().Build();

            protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
            {
                var connectionString = (TestContainer as IDatabaseContainer)?.GetConnectionString()
                    ?? "Server=(localdb)\\MSSqlLocalDB;Integrated Security=SSPI;Initial Catalog=FlexLabsUpsertTests;";
                builder.UseSqlServer(connectionString);
            }
        }

        public DbTests_SqlServer(DatabaseInitializer contexts)
            : base(contexts)
        { }
    }
#endif
}
