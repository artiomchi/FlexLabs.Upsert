using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public class DbTests_Sqlite : DbTestsBase, IClassFixture<DbTests_Sqlite.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            public DatabaseInitializer()
                : base(DbDriver.Sqlite)
            { }

            protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
                => builder.UseSqlite("Data Source=testdb.db");
        }

        public DbTests_Sqlite(DatabaseInitializer contexts)
            : base(contexts)
        { }
    }
}
