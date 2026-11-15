using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void ComputedColumn_Updates()
    {
        Require(Capability.ComputedColumns);

        using var dbContext = Arrange();

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
