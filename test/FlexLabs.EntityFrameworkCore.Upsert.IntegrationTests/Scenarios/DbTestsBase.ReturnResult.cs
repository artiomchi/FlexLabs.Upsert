using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void Upsert_ReturnResult_Single()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var dashTable = new DashTable
        {
            DataSet = "Test",
            Updated = _now,
        };

        var result = dbContext.DashTable.Upsert(dashTable)
            .On(c => c.DataSet)
            .RunAndReturn();

        result.Should().ContainEquivalentOf(new DashTable
        {
            ID = result.First().ID,
            DataSet = "Test",
            Updated = _now,
        });
    }

    [Fact]
    public void Upsert_ReturnResult_Multiple()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        ResetDb(new DashTable { DataSet = "Test1" });
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var dashTables = new[]
        {
            new DashTable
            {
                DataSet = "Test1",
                Updated = _now,
            },
            new DashTable
            {
                DataSet = "Test2",
                Updated = _now,
            }
        };

        var result = dbContext.DashTable.UpsertRange(dashTables)
            .On(c => c.DataSet)
            .RunAndReturn();

        result.Should().HaveCount(2);

        dbContext.DashTable.Should().HaveCount(2);
    }

    [Fact]
    public void Upsert_ReturnResult_TracksChanges()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        ResetDb(new DashTable { DataSet = "Test" });
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var dashTable = new DashTable
        {
            DataSet = "Test",
            Updated = _now,
        };

        var result = dbContext.DashTable.Upsert(dashTable)
            .On(c => c.DataSet)
            .RunAndReturn();

        result.Single().Updated = _now.AddYears(1);
        dbContext.SaveChanges();

        var updatedResult = dbContext.DashTable.Single();
        updatedResult.Updated.Should().Be(_now.AddYears(1));
    }

    [Fact]
    public async Task Upsert_WithUpdate_RunAndReturnAsync_ReturnsUpdatedValuesNotTrackedValues()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        // Arrange - Insert initial record with Visits = 10
        ResetDb();
        await using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var initialVisit = new PageVisit
        {
            UserID = 99,
            Date = _today,
            Visits = 10,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Add(initialVisit);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - UPSERT with delta +5 (should result in Visits = 15)
        var upsertEntity = new PageVisit
        {
            UserID = 99,
            Date = _today,
            Visits = 5,  // Delta to add
            FirstVisit = _now,
            LastVisit = _now.AddHours(1),
        };

        var result = await dbContext.PageVisits.Upsert(upsertEntity)
            .On(v => new { v.UserID, v.Date })
            .WhenMatched((existing, inserted) => new PageVisit
            {
                Visits = existing.Visits + inserted.Visits,  // 10 + 5 = 15
                LastVisit = inserted.LastVisit,
            })
            .RunAndReturnAsync(TestContext.Current.CancellationToken);

        // Assert - Should return the updated value from database (15), not the input value (5)
        result.Should().SatisfyRespectively(
            item => item.Visits.Should().Be(15, "RunAndReturnAsync should return fresh values from database after UPSERT, not stale tracked entities"));

        // Verify database actually has correct value
        var dbValue = await dbContext.PageVisits
            .AsNoTracking()
            .FirstAsync(v => v.UserID == 99 && v.Date == _today, TestContext.Current.CancellationToken);
        dbValue.Visits.Should().Be(15, "the database should have the correct value");
    }

    [Fact]
    public void Upsert_WithUpdate_RunAndReturn_ReturnsUpdatedValuesNotTrackedValues()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        // Arrange - Insert initial record with Visits = 10
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var initialVisit = new PageVisit
        {
            UserID = 99,
            Date = _today,
            Visits = 10,
            FirstVisit = _now,
            LastVisit = _now,
        };

        dbContext.PageVisits.Add(initialVisit);
        dbContext.SaveChanges();

        // Act - UPSERT with delta +5 (should result in Visits = 15)
        var upsertEntity = new PageVisit
        {
            UserID = 99,
            Date = _today,
            Visits = 5,  // Delta to add
            FirstVisit = _now,
            LastVisit = _now.AddHours(1),
        };

        var result = dbContext.PageVisits.Upsert(upsertEntity)
            .On(v => new { v.UserID, v.Date })
            .WhenMatched((existing, inserted) => new PageVisit
            {
                Visits = existing.Visits + inserted.Visits,  // 10 + 5 = 15
                LastVisit = inserted.LastVisit,
            })
            .RunAndReturn();

        // Assert - Should return the updated value from database (15), not the input value (5)
        result.Should().SatisfyRespectively(
            item => item.Visits.Should().Be(15, "RunAndReturn should return fresh values from database after UPSERT, not stale tracked entities"));

        // Verify database actually has correct value
        var dbValue = dbContext.PageVisits
            .AsNoTracking()
            .First(v => v.UserID == 99 && v.Date == _today);
        dbValue.Visits.Should().Be(15, "the database should have the correct value");
    }
}
