#if !NOMSSQL
using System.Data.Common;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Testcontainers.Xunit;
using Xunit.Sdk;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests;

public class DbTests_SqlServer(DbTests_SqlServer.DatabaseInitializer contexts) : DbTestsBase(contexts), IClassFixture<DbTests_SqlServer.DatabaseInitializer>
{
    public sealed class DatabaseInitializer(IMessageSink messageSink) : ContainerisedDatabaseInitializerFixture<MsSqlBuilder, MsSqlContainer>(new MsSqlFixture(messageSink))
    {
        public override DbDriver DbDriver => DbDriver.MSSQL;

        protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
            => builder.UseSqlServer(ConnectionString);

        private class MsSqlFixture(IMessageSink messageSink) : DbContainerFixture<MsSqlBuilder, MsSqlContainer>(messageSink)
        {
            public override DbProviderFactory DbProviderFactory
                => SqlClientFactory.Instance;

            // https://mcr.microsoft.com/en-us/artifact/mar/mssql/rhel/server/tags
            protected override MsSqlBuilder Configure()
                => ConfigureContainer(new MsSqlBuilder("mcr.microsoft.com/mssql/rhel/server:2025-latest"));
        }
    }
}
#endif
