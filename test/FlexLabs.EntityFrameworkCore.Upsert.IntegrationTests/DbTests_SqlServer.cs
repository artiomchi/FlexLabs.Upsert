using System.Data.Common;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Testcontainers.Xunit;
using Xunit;
using Xunit.Sdk;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
#if !NOMSSQL
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

                protected override MsSqlBuilder Configure(MsSqlBuilder builder)
                    => ConfigureContainer(builder);
            }
        }
    }
#endif
}
