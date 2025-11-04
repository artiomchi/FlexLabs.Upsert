using System.IO;
using System.Threading.Tasks;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public class DbTests_Sqlite(DbTests_Sqlite.DatabaseInitializer contexts) : DbTestsBase(contexts), IClassFixture<DbTests_Sqlite.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            private const string DbFilePath = "testdb.db";
            public override DbDriver DbDriver => DbDriver.Sqlite;

            protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
                => builder.UseSqlite($"Data Source={DbFilePath}");

            public override ValueTask InitializeAsync()
            {
                File.Delete(DbFilePath);
                return base.InitializeAsync();
            }
        }
    }
}
