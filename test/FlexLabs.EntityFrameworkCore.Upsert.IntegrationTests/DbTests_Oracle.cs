#if !NOORACLE
using System.Data.Common;
using DotNet.Testcontainers.Builders;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;
using Testcontainers.Xunit;
using Xunit.Sdk;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests;

public class DbTests_Oracle(DbTests_Oracle.DatabaseInitializer contexts) : DbTestsBase(contexts), IClassFixture<DbTests_Oracle.DatabaseInitializer>
{
    public sealed class DatabaseInitializer(IMessageSink messageSink) : ContainerisedDatabaseInitializerFixture<OracleBuilder, OracleContainer>(new OracleFixture(messageSink))
    {
        public override DbDriver DbDriver => DbDriver.Oracle;

        protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
            => builder.UseOracle(ConnectionString).UseUpperSnakeCaseNamingConvention();

        private class OracleFixture(IMessageSink messageSink) : DbContainerFixture<OracleBuilder, OracleContainer>(messageSink)
        {
            public override DbProviderFactory DbProviderFactory
                => OracleClientFactory.Instance;

            // https://hub.docker.com/r/gvenzl/oracle-free
            protected override OracleBuilder Configure()
                => ConfigureContainer(new OracleBuilder("gvenzl/oracle-free:23-slim-faststart"))
                .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("/opt/oracle/healthcheck.sh"));
        }
    }
}
#endif
