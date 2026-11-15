using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void Upsert_InitialDbState()
    {
        using var dbContext = Arrange();

        dbContext.SchemaTable.Should().BeEmpty();
        dbContext.DashTable.Should().BeEmpty();
        dbContext.Countries.OrderBy(c => c.ID).Should().OnlyContain(a => a.ISO == "AU");
        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            pv => pv.Should().MatchModel(_dbVisitOld),
            pv => pv.Should().MatchModel(_dbVisit));
    }
}
