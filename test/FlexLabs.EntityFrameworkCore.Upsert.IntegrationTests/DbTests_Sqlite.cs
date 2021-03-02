using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Xunit;
using Xunit.Abstractions;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
#if !NOSQLITE
    public class DbTests_Sqlite : DbTestsBase, IClassFixture<DbTests_Sqlite.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            public DatabaseInitializer(IMessageSink diagnosticMessageSink)
                : base(diagnosticMessageSink, DbDriver.Sqlite)
            { }
        }

        public DbTests_Sqlite(DatabaseInitializer contexts)
            : base(contexts)
        { }
    }
#endif
}
