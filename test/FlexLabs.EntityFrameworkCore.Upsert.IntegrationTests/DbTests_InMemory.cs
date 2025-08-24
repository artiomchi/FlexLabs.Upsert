using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    [Trait("Category", "in-memory")]
    [Collection("InMemory")]
    public class DbTests_InMemory(DbTests_InMemory.DatabaseInitializer contexts) : DbTestsBase(contexts), IClassFixture<DbTests_InMemory.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            public override DbDriver DbDriver => DbDriver.InMemory;

            protected override void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder)
                => builder.UseInMemoryDatabase("Upsert_TestDbContext_Tests");
        }
    }
}
