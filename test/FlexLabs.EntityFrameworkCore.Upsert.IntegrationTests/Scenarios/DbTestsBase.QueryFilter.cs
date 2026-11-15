using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public async Task Upsert_WithQueryFilter_RunAndReturn_Inserts()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        await using var dbContext = Arrange();

        var dbItem1 = new TestEntityFiltered
        {
            IsDeleted = true,
            Counter = 1,
        };

        var resultItems = await dbContext.TestEntitiesFiltered.Upsert(dbItem1)
            .On(j => j.Key)
            .NoUpdate()
            .RunAndReturnAsync(TestContext.Current.CancellationToken);

        resultItems.Should().SatisfyRespectively(
            item => item.Should().BeEquivalentTo(dbItem1, o => o.Excluding(x => x.ID)));

        dbContext.TestEntitiesFiltered
            .IgnoreQueryFilters()
            .OrderBy(t => t.ID)
            .Should()
            .SatisfyRespectively(
                test => test.Should().BeEquivalentTo(dbItem1, o => o.Excluding(x => x.ID)));
    }

    [Fact]
    public async Task Upsert_WithQueryFilter_RunAndReturn_Updates()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        var dbItem1 = new TestEntityFiltered
        {
            Key = "key1",
            IsDeleted = true,
            Counter = 1,
        };

        await using var dbContext = Arrange(dbItem1);

        var dbItem1Updated = new TestEntityFiltered
        {
            Key = "key1",
            IsDeleted = true,
            Counter = 2,
        };

        var resultItems = await dbContext.TestEntitiesFiltered.Upsert(dbItem1Updated)
            .On(j => j.Key)
            .WhenMatched((a, b) => new TestEntityFiltered
            {
                Counter = a.Counter + b.Counter,
            })
            .RunAndReturnAsync(TestContext.Current.CancellationToken);

        resultItems.Should().SatisfyRespectively(
            item => item.Counter.Should().Be(3));

        dbContext.TestEntitiesFiltered
            .IgnoreQueryFilters()
            .OrderBy(t => t.ID)
            .Should()
            .SatisfyRespectively(
                test => test.Counter.Should().Be(3));
    }
}
