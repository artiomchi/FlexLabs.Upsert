using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests;

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

    /// <summary>
    /// Sqlite, like Postgres, only resolves a partial index when the command repeats its predicate.
    /// </summary>
    [Fact]
    public void PartialUniqueIndex_Update_Matches()
    {
        ResetDb();
        SeedPartialIndexEntity(isDeleted: false);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        dbContext.TestEntitiesPartialIndex.Upsert(new TestEntityPartialIndex { Key = "key", Counter = 2 })
            .On(e => e.Key)
            .WhenMatched((e, en) => new TestEntityPartialIndex { Counter = en.Counter })
            .Run();

        dbContext.TestEntitiesPartialIndex.OrderBy(e => e.ID).Should().SatisfyRespectively(
            entity =>
            {
                entity.Key.Should().Be("key");
                entity.Counter.Should().Be(2);
            });
    }

    /// <summary>
    /// The rows the index excludes don't take part in the match, which is the whole reason to make
    /// an index partial - a soft deleted key stays reusable.
    /// </summary>
    [Fact]
    public void PartialUniqueIndex_Update_IgnoresRowsOutsideTheIndex()
    {
        ResetDb();
        SeedPartialIndexEntity(isDeleted: true);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        dbContext.TestEntitiesPartialIndex.Upsert(new TestEntityPartialIndex { Key = "key", Counter = 2 })
            .On(e => e.Key)
            .WhenMatched((e, en) => new TestEntityPartialIndex { Counter = en.Counter })
            .Run();

        dbContext.TestEntitiesPartialIndex.OrderBy(e => e.ID).Should().SatisfyRespectively(
            deleted =>
            {
                deleted.Counter.Should().Be(1);
                deleted.IsDeleted.Should().BeTrue();
            },
            inserted =>
            {
                inserted.Counter.Should().Be(2);
                inserted.IsDeleted.Should().BeFalse();
            });
    }

    private void SeedPartialIndexEntity(bool isDeleted)
    {
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);
        dbContext.TestEntitiesPartialIndex.Add(new TestEntityPartialIndex
        {
            Key = "key",
            Counter = 1,
            IsDeleted = isDeleted,
        });
        dbContext.SaveChanges();
    }
}
