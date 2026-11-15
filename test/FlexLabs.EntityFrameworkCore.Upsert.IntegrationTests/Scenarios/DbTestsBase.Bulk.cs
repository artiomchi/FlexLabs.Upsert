using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void Upsert_100k_Insert_MultiQuery()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newEntities = Enumerable.Range(1, 100_000)
            .Select(i => new NullableRequired
            {
                ID = i,
                Text = i.ToString(),
            });
        dbContext.NullableRequireds.UpsertRange(newEntities)
            .On(c => c.ID)
            .Run();

        dbContext.NullableRequireds.Should().HaveCount(100_000);
    }
}
