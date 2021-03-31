using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Xunit;
using Xunit.Abstractions;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
#if !NOPOSTGRES
    public class DbTests_Postgres : DbTestsBase, IClassFixture<DbTests_Postgres.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            public DatabaseInitializer(IMessageSink diagnosticMessageSink)
                : base(diagnosticMessageSink, DbDriver.Postgres)
            { }
        }

        public DbTests_Postgres(DatabaseInitializer contexts)
            : base(contexts)
        { }
    }
#endif
}
