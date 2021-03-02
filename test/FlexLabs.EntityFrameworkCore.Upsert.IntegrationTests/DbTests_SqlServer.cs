using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Xunit;
using Xunit.Abstractions;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public class DbTests_SqlServer : DbTestsBase, IClassFixture<DbTests_SqlServer.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            public DatabaseInitializer(IMessageSink diagnosticMessageSink)
                : base(diagnosticMessageSink, DbDriver.MSSQL)
            { }
        }

        public DbTests_SqlServer(DatabaseInitializer contexts)
            : base(contexts)
        { }
    }
}
