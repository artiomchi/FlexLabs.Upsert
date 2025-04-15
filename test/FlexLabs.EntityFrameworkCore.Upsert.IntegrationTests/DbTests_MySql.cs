using System.Data.Common;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Testcontainers.MySql;
using Testcontainers.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
#if !NOMYSQL
    public class DbTests_MySql(DbTests_MySql.DatabaseInitializer contexts) : DbTestsBase(contexts), IClassFixture<DbTests_MySql.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer(IMessageSink messageSink) : ContainerisedDatabaseInitializerFixture<MySqlBuilder, MySqlContainer>(new MySqlFixture(messageSink))
        {
            public override DbDriver DbDriver => DbDriver.MySQL;

            protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
                => builder.UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString));

            private class MySqlFixture(IMessageSink messageSink) : DbContainerFixture<MySqlBuilder, MySqlContainer>(messageSink)
            {
                public override DbProviderFactory DbProviderFactory
                    => MySqlConnectorFactory.Instance;

                protected override MySqlBuilder Configure(MySqlBuilder builder)
                    => ConfigureContainer(builder);
            }
        }
    }
#endif
}
