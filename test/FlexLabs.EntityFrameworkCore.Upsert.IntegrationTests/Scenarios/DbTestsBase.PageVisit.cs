using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void Upsert_PageVisit_PreComputedOn()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(PageVisit.MatchKey)
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_ValueAdd()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits + 1,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits + 1));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_ValueAdd_FromVar()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };
        var increment = 7;

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits + increment,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits + increment));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_ValueAdd_FromField()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits + _increment,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits + _increment));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_FromSource_ValueAdd()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched((pv, pvi) => new PageVisit
            {
                Visits = pv.Visits + 1,
                LastVisit = pvi.LastVisit,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits + 1));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_FromSource_ColumnAdd()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 5,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched((pv, pvi) => new PageVisit
            {
                Visits = pv.Visits + pvi.Visits,
                LastVisit = pvi.LastVisit,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits + newVisit.Visits));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_ValueAdd_Reversed()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = 1 + pv.Visits,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits + 1));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_ValueSubtract()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits - 2,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits - 2));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_ValueMultiply()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits * 3,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits * 3));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_ValueBitwiseOr()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits | 3,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits | 3));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_ValueBitwiseAnd()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits & 3,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits & 3));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_ValueDivide()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits / 4,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits / 4));
    }

    [Fact]
    public void Upsert_PageVisit_Update_On_WhenMatched_ValueModulo()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newVisit = new PageVisit
        {
            UserID = 1,
            Date = _today,
            Visits = 1,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Upsert(newVisit)
            .On(pv => new { pv.UserID, pv.Date })
            .WhenMatched(pv => new PageVisit
            {
                Visits = pv.Visits % 4,
                LastVisit = _now,
            })
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: _dbVisit.Visits % 4));
    }
}
