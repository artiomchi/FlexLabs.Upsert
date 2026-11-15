using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void UpsertRange_PageVisit_Update_On_WhenMatched()
    {
        using var dbContext = Arrange();

        var newVisit1 = new PageVisit
        {
            UserID = _dbVisit.UserID,
            Date = _dbVisit.Date,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };
        var newVisit2 = new PageVisit
        {
            UserID = _dbVisit.UserID,
            Date = _dbVisit.Date.AddDays(1),
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.UpsertRange(newVisit1, newVisit2)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits + 1,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit1, compareFirstVisit: false, expectedVisits: _dbVisit.Visits + 1),
            visit => visit.Should().MatchModel(newVisit2));
    }

    [Fact]
    public void UpsertRange_PageVisit_Update_On_WhenMatched_MultipleInsert()
    {
        using var dbContext = Arrange();

        var newVisit1 = new PageVisit
        {
            UserID = _dbVisit.UserID,
            Date = _dbVisit.Date.AddDays(1),
            Visits = 5,
            FirstVisit = _now,
            LastVisit = _now,
        };
        var newVisit2 = new PageVisit
        {
            UserID = _dbVisit.UserID,
            Date = newVisit1.Date.AddDays(2),
            Visits = newVisit1.Visits + 1,
            FirstVisit = newVisit1.FirstVisit.AddDays(1),
            LastVisit = newVisit1.LastVisit.AddDays(1),
        };

        dbContext.PageVisits.UpsertRange(newVisit1, newVisit2)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits + 1,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(_dbVisit),
            visit => visit.Should().MatchModel(newVisit1),
            visit => visit.Should().MatchModel(newVisit2));
    }

    [Fact]
    public void UpsertRange_PageVisit_Update_On_WhenMatched_MultipleUpdate()
    {
        using var dbContext = Arrange();

        var newVisit1 = new PageVisit
        {
            UserID = _dbVisitOld.UserID,
            Date = _dbVisitOld.Date,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };
        var newVisit2 = new PageVisit
        {
            UserID = _dbVisit.UserID,
            Date = _dbVisit.Date,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.UpsertRange(newVisit1, newVisit2)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits + 1,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(newVisit1, compareFirstVisit: false, expectedVisits: _dbVisitOld.Visits + 1),
            visit => visit.Should().MatchModel(newVisit2, compareFirstVisit: false, expectedVisits: _dbVisit.Visits + 1));
    }

    [Fact]
    public void UpsertRange_PageVisit_Update_On_WhenMatched_MultipleUpdate_FromSource()
    {
        using var dbContext = Arrange();

        var newVisit1 = new PageVisit
        {
            UserID = _dbVisitOld.UserID,
            Date = _dbVisitOld.Date,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };
        var newVisit2 = new PageVisit
        {
            UserID = _dbVisit.UserID,
            Date = _dbVisit.Date,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.UpsertRange(newVisit1, newVisit2)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched((pv, pvi) => new PageVisit
            {
                Visits = pv.Visits + 1,
                LastVisit = pvi.LastVisit,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(newVisit1, compareFirstVisit: false, expectedVisits: _dbVisitOld.Visits + 1),
            visit => visit.Should().MatchModel(newVisit2, compareFirstVisit: false, expectedVisits: _dbVisit.Visits + 1));
    }
}
