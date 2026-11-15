using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void ComputedColumn_Updates()
    {
        Assert.SkipWhen(_fixture.DbDriver == DbDriver.InMemory, "In memory db doesn't support sql computed columns");

        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var item = new ComputedColumn
        {
            Num1 = 1,
            Num2 = 9
        };

        dbContext.ComputedColumns.Upsert(item)
            .On(c => c.Num1)
            .Run();

        dbContext.ComputedColumns.OrderBy(c => c.Num1).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(item, num3: 10));
    }
}
