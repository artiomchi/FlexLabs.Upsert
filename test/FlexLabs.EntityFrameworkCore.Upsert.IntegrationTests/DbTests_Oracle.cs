using System.Data.Common;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;
using Testcontainers.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
#if !NOORACLE
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

                protected override OracleBuilder Configure(OracleBuilder builder)
                    => ConfigureContainer(builder).WithImage("gvenzl/oracle-free:23-slim-faststart");
            }
        }
    }
#endif
}
