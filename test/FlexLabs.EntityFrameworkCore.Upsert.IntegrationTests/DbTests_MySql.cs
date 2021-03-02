using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Xunit;
using Xunit.Abstractions;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public class DbTests_MySql : DbTestsBase, IClassFixture<DbTests_MySql.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            public DatabaseInitializer(IMessageSink diagnosticMessageSink)
                : base(diagnosticMessageSink, DbDriver.MySQL)
            { }
        }

        public DbTests_MySql(DatabaseInitializer contexts)
            : base(contexts)
        { }
    }
}
