using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    // The WhenMatched expressions are the thing under test, so they are written out as plain C# lambdas here.
    // They're keyed by name (rather than passed as theory data) because expression trees can't be serialized,
    // and we want every case to be discovered and reported as its own test.
    private static readonly Dictionary<string, (Expression<Func<PageVisit, PageVisit>> Update, int ExpectedVisits)> VisitUpdates = BuildVisitUpdates();

    private static Dictionary<string, (Expression<Func<PageVisit, PageVisit>>, int)> BuildVisitUpdates()
    {
        var increment = 7; // captured local variable
        return new()
        {
            ["ValueAdd"] = (pv => new PageVisit { Visits = pv.Visits + 1, LastVisit = _now }, ExistingVisits + 1),
            ["ValueAdd_FromVar"] = (pv => new PageVisit { Visits = pv.Visits + increment, LastVisit = _now }, ExistingVisits + increment),
            ["ValueAdd_Reversed"] = (pv => new PageVisit { Visits = 1 + pv.Visits, LastVisit = _now }, 1 + ExistingVisits),
            ["ValueSubtract"] = (pv => new PageVisit { Visits = pv.Visits - 2, LastVisit = _now }, ExistingVisits - 2),
            ["ValueMultiply"] = (pv => new PageVisit { Visits = pv.Visits * 3, LastVisit = _now }, ExistingVisits * 3),
            ["ValueBitwiseOr"] = (pv => new PageVisit { Visits = pv.Visits | 3, LastVisit = _now }, ExistingVisits | 3),
            ["ValueBitwiseAnd"] = (pv => new PageVisit { Visits = pv.Visits & 3, LastVisit = _now }, ExistingVisits & 3),
            ["ValueDivide"] = (pv => new PageVisit { Visits = pv.Visits / 4, LastVisit = _now }, ExistingVisits / 4),
            ["ValueModulo"] = (pv => new PageVisit { Visits = pv.Visits % 4, LastVisit = _now }, ExistingVisits % 4),
        };
    }

    public static TheoryData<string> VisitUpdateCases => new(VisitUpdates.Keys);

    [Theory]
    [MemberData(nameof(VisitUpdateCases))]
    public void Upsert_PageVisit_Update_On_WhenMatched(string updateCase)
    {
        var (update, expectedVisits) = VisitUpdates[updateCase];
        using var dbContext = Arrange();

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
            .WhenMatched(update)
            .Run();

        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            visit => visit.Should().MatchModel(_dbVisitOld),
            visit => visit.Should().MatchModel(newVisit, compareFirstVisit: false, expectedVisits: expectedVisits));
    }

    [Fact]
    public void Upsert_PageVisit_PreComputedOn()
    {
        using var dbContext = Arrange();

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
        using var dbContext = Arrange();

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
    public void Upsert_PageVisit_Update_On_WhenMatched_ValueAdd_FromField()
    {
        using var dbContext = Arrange();

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
        using var dbContext = Arrange();

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
        using var dbContext = Arrange();

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
}
