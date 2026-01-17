using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    protected readonly DatabaseInitializerFixture _fixture;

    public DbTestsBase(DatabaseInitializerFixture fixture)
    {
        _fixture = fixture;
    }

    readonly Country _dbCountry = new()
    {
        Name = "...loading...",
        ISO = "AU",
        Created = NewDateTime(1970, 1, 1),
    };

    readonly PageVisit _dbVisitOld = new()
    {
        UserID = 1,
        Date = _today.AddDays(-1),
        Visits = 10,
        FirstVisit = NewDateTime(1970, 1, 1),
        LastVisit = NewDateTime(1970, 1, 1),
    };

    readonly PageVisit _dbVisit = new()
    {
        UserID = 1,
        Date = _today,
        Visits = 12,
        FirstVisit = NewDateTime(1970, 1, 1),
        LastVisit = NewDateTime(1970, 1, 1),
    };

    readonly Status _dbStatus = new()
    {
        ID = 1,
        Name = "Created",
        LastChecked = NewDateTime(1970, 1, 1),
    };

    readonly Book _dbBook = new()
    {
        Name = "The Fellowship of the Ring",
        Genres = new[] { "Fantasy" },
    };

    readonly NullableCompositeKey _nullableKey1 = new()
    {
        ID1 = 1,
        ID2 = 2,
        Value = "First",
    };

    readonly NullableCompositeKey _nullableKey2 = new()
    {
        ID1 = 1,
        ID2 = null,
        Value = "Second",
    };

    readonly ComputedColumn _computedColumn = new()
    {
        Num1 = 1,
        Num2 = 7,
    };

    readonly static DateTime _now = NewDateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
        DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

    readonly static DateTime _today = NewDateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
    readonly int _increment = 8;

    private static DateTime NewDateTime(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        => new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

    protected int GeneratedAlwaysAsIdentity_NextId { get; private set; }

    protected void ResetDb()
    {
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        Reset(dbContext, e => e.Books);
        Reset(dbContext, e => e.Countries);
        Reset(dbContext, e => e.DashTable);
        Reset(dbContext, e => e.GuidKeys);
        Reset(dbContext, e => e.GuidKeysAutoGen);
        Reset(dbContext, e => e.JObjectDatas);
        Reset(dbContext, e => e.JsonDatas);
        Reset(dbContext, e => e.JsonDocumentDatas);
        Reset(dbContext, e => e.KeyOnlies);
        Reset(dbContext, e => e.NullableCompositeKeys);
        Reset(dbContext, e => e.NullableRequireds);
        Reset(dbContext, e => e.PageVisits);
        Reset(dbContext, e => e.SchemaTable);
        Reset(dbContext, e => e.Statuses);
        Reset(dbContext, e => e.StringKeys);
        Reset(dbContext, e => e.StringKeysAutoGen);
        Reset(dbContext, e => e.TestEntities);
        Reset(dbContext, e => e.TestEntitiesFiltered);
        Reset(dbContext, e => e.ULongEntities);
        Reset(dbContext, e => e.GeneratedAlwaysAsIdentity);
        Reset(dbContext, e => e.ComputedColumns);
        Reset(dbContext, e => e.Parents);
        Reset(dbContext, e => e.ParentComplexes);
        Reset(dbContext, e => e.CompanyOwnedJson);
        Reset(dbContext, e => e.CompanyComplexJson);

        dbContext.Add(_dbCountry);
        dbContext.Add(_dbVisitOld);
        dbContext.Add(_dbVisit);
        dbContext.Add(_dbStatus);
        dbContext.Add(_dbBook);
        dbContext.Add(_nullableKey1);
        dbContext.Add(_nullableKey2);
        dbContext.Add(_computedColumn);
        dbContext.Add(new GeneratedAlwaysAsIdentity());
        dbContext.Add(_dbParent);
        dbContext.Add(_dbParentComplex);
        dbContext.SaveChanges();

        GeneratedAlwaysAsIdentity_NextId = dbContext.GeneratedAlwaysAsIdentity.Max(e => e.ID) + 1;
        Reset(dbContext, e => e.GeneratedAlwaysAsIdentity);
        dbContext.SaveChanges();
    }

    private void Reset<T>(TestDbContext dbContext, Func<TestDbContext, DbSet<T>> selector) where T : class
    {
        var dbSet = selector(dbContext);
        if (_fixture.DbDriver == DbDriver.InMemory)
        {
            dbContext.RemoveRange(dbSet.IgnoreQueryFilters());
        }
        else
        {
            dbSet.IgnoreQueryFilters().ExecuteDelete();
        }
    }

    private void ResetDb<TEntity>(params TEntity[] seedValue)
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        dbContext.AddRange(seedValue.Cast<object>());
        dbContext.SaveChanges();
    }

    [Fact]
    public void Upsert_InitialDbState()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        dbContext.SchemaTable.Should().BeEmpty();
        dbContext.DashTable.Should().BeEmpty();
        dbContext.Countries.OrderBy(c => c.ID).Should().OnlyContain(a => a.ISO == "AU");
        dbContext.PageVisits.OrderBy(c => c.Date).Should().SatisfyRespectively(
            pv => pv.Should().MatchModel(_dbVisitOld),
            pv => pv.Should().MatchModel(_dbVisit));
    }

    [Fact]
    public void Upsert_EFCore_KeyAutoGen()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        dbContext.GuidKeysAutoGen.Add(new GuidKeyAutoGen { Name = "test" });
        dbContext.StringKeysAutoGen.Add(new StringKeyAutoGen { Name = "test" });
        dbContext.SaveChanges();

        // Ensuring EFCore generates empty values for Guid and string keys
        dbContext.GuidKeysAutoGen.Should().OnlyContain(e => e.ID != default);
        dbContext.StringKeysAutoGen.Should().OnlyContain(e => !string.IsNullOrEmpty(e.ID));
    }

    [Fact]
    public void Upsert_IdentityKey_NoOn_InvalidMatchColumn()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newCountry = new Country
        {
            Name = "Germany",
            ISO = "DE",
            Created = _now,
            Updated = _now,
        };

        Action action = () => dbContext.Countries.Upsert(newCountry).Run();
        action.Should().Throw<InvalidMatchColumnsException>();
    }

    [Fact]
    public void Upsert_IdentityKey_ExplicitOn_InvalidMatchColumn()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newCountry = new Country
        {
            Name = "Germany",
            ISO = "DE",
            Created = _now,
            Updated = _now,
        };

        Action action = () => dbContext.Countries.Upsert(newCountry)
            .On(c => c.ID)
            .Run();
        action.Should().Throw<InvalidMatchColumnsException>();
    }

    [Fact]
    public void Upsert_IdentityKey_NoOn_AllowWithOverride()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newCountry = new Country
        {
            Name = "Germany",
            ISO = "DE",
            Created = _now,
            Updated = _now,
        };

        dbContext.Countries.Upsert(newCountry)
            .AllowIdentityMatch()
            .Run();

        dbContext.Countries.Should().HaveCount(2);
    }

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
    public void Upsert_IdentityKey_ExplicitOn_AllowWithOverride()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newCountry = new Country
        {
            Name = "Germany",
            ISO = "DE",
            Created = _now,
            Updated = _now,
        };

        dbContext.Countries.Upsert(newCountry)
            .On(c => c.ID)
            .AllowIdentityMatch()
            .Run();

        dbContext.Countries.Should().HaveCount(2);
    }

    [Fact]
    public void Upsert_Country_Update_On()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newCountry = new Country
        {
            Name = "Australia",
            ISO = "AU",
            Created = _now,
            Updated = _now,
        };

        dbContext.Countries.Upsert(newCountry)
            .On(c => c.ISO)
            .Run();

        dbContext.Countries.OrderBy(c => c.ID).Should().SatisfyRespectively(
            country => country.Should().MatchModel(newCountry));
    }

    [Fact]
    public void Upsert_Country_Update_On_NoUpdate()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newCountry = new Country
        {
            Name = "Australia",
            ISO = "AU",
            Created = _now,
            Updated = _now,
        };

        dbContext.Countries.Upsert(newCountry)
            .On(c => c.ISO)
            .NoUpdate()
            .Run();

        dbContext.Countries.OrderBy(c => c.ID).Should().SatisfyRespectively(
            country => country.Should().MatchModel(_dbCountry));
    }

    [Fact]
    public void Upsert_Country_Update_On_WhenMatched_Values()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newCountry = new Country
        {
            Name = "Australia",
            ISO = "AU",
            Created = _now,
            Updated = _now,
        };

        dbContext.Countries.Upsert(newCountry)
            .On(c => c.ISO)
            .WhenMatched(c => new Country
            {
                Name = newCountry.Name,
                Updated = newCountry.Updated,
            })
            .Run();

        dbContext.Countries.OrderBy(c => c.ID).Should().SatisfyRespectively(
            country => country.Should().MatchModel(newCountry, compareCreated: false)
                .Subject.Created.Should().Be(_dbCountry.Created));
    }

    [Fact]
    public void Upsert_Country_Update_On_WhenMatched_Constants()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newCountry = new Country
        {
            Name = "Australia",
            ISO = "AU",
            Created = _now,
            Updated = _now,
        };

        dbContext.Countries.Upsert(newCountry)
            .On(c => c.ISO)
            .WhenMatched(c => new Country
            {
                Name = "Australia",
                Updated = _now,
            })
            .Run();

        dbContext.Countries.OrderBy(c => c.ID).Should().SatisfyRespectively(
            country => country.Should().MatchModel(newCountry, compareCreated: false)
                .Subject.Created.Should().Be(_dbCountry.Created));
    }

    [Fact]
    public void Upsert_Country_Insert_On_WhenMatched()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newCountry = new Country
        {
            Name = "United Kingdon",
            ISO = "GB",
            Created = _now,
            Updated = _now,
        };

        dbContext.Countries.Upsert(newCountry)
            .On(c => c.ISO)
            .WhenMatched(c => new Country
            {
                Name = newCountry.Name,
                Updated = newCountry.Updated,
            })
            .Run();

        dbContext.Countries.OrderBy(c => c.ID).Should().SatisfyRespectively(
            country => country.Should().MatchModel(_dbCountry),
            country => country.Should().MatchModel(newCountry));
    }

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

    [Fact]
    public void UpsertRange_PageVisit_Update_On_WhenMatched()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

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
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

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
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

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
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

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

    [Fact]
    public void Upsert_Status_Update_AutoMatched_New()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newStatus = new Status
        {
            ID = 2,
            Name = "Updated",
            LastChecked = _now,
        };

        dbContext.Statuses.Upsert(newStatus).Run();

        dbContext.Statuses.OrderBy(c => c.ID).Should().SatisfyRespectively(
            status => status.Should().MatchModel(_dbStatus),
            status => status.Should().MatchModel(newStatus));
    }

    [Fact]
    public void Upsert_Status_Update_AutoMatched_Existing()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newStatus = new Status
        {
            ID = _dbStatus.ID,
            Name = "Updated",
            LastChecked = _now,
        };

        dbContext.Statuses.Upsert(newStatus).Run();

        dbContext.Statuses.OrderBy(c => c.ID).Should().SatisfyRespectively(
            status => status.Should().MatchModel(newStatus));
    }

    [Fact]
    public void Upsert_DashedTable()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newRecord = new DashTable
        {
            DataSet = "test",
            Updated = _now,
        };

        dbContext.DashTable.Upsert(newRecord)
            .On(x => x.DataSet)
            .Run();

        dbContext.DashTable.OrderBy(c => c.ID).Should().SatisfyRespectively(
            r => r.DataSet.Should().Be(newRecord.DataSet));
    }

    [Fact]
    public void Upsert_SchemaTable()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newRecord = new SchemaTable
        {
            Name = 1,
            Updated = _now,
        };

        dbContext.SchemaTable.Upsert(newRecord)
            .On(x => x.Name)
            .Run();

        dbContext.SchemaTable.OrderBy(c => c.ID).Should().SatisfyRespectively(
            r => r.Name.Should().Be(newRecord.Name));
    }

    [Fact]
    public void Upsert_Book_On_Update()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newBook = new Book
        {
            Name = _dbBook.Name,
            Genres = new[] { "Fantasy", "Adventure" },
        };

        dbContext.Books.Upsert(newBook)
            .On(b => b.Name)
            .Run();

        dbContext.Books.OrderBy(c => c.ID).Should().SatisfyRespectively(
            book => book.Should().MatchModel(newBook));
    }

    [Fact]
    public void Upsert_Book_On_Insert()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newBook = new Book
        {
            Name = "The Two Towers",
            Genres = new[] { "Fantasy", "Adventure" },
        };

        dbContext.Books.Upsert(newBook)
            .On(p => p.Name)
            .Run();

        dbContext.Books.OrderBy(c => c.ID).Should().SatisfyRespectively(
            book => book.Should().MatchModel(_dbBook),
            book => book.Should().MatchModel(newBook));
    }

    [Fact]
    public void Upsert_JObjectData()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newJson = new JObjectData
        {
            Data = new JObject(new JProperty("hello", "world")),
        };

        dbContext.JObjectDatas.Upsert(newJson)
            .Run();

        dbContext.JObjectDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => JToken.DeepEquals(newJson.Data, j.Data).Should().BeTrue());
    }

    [Fact]
    public void Upsert_JObject_Update()
    {
        var existingJson = new JObjectData
        {
            Data = new JObject(new JProperty("hello", "world")),
        };

        ResetDb(existingJson);
        using (var testContext = new TestDbContext(_fixture.DataContextOptions))
        {
            testContext.JObjectDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
                j => JToken.DeepEquals(existingJson.Data, j.Data).Should().BeTrue());
        }

        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var updatedJson = new JObjectData
        {
            Data = new JObject(new JProperty("welcome", "world 2.0")),
        };

        dbContext.JObjectDatas.Upsert(updatedJson)
            .Run();

        dbContext.JObjectDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => JToken.DeepEquals(updatedJson.Data, j.Data).Should().BeTrue());
    }

    [Fact]
    public void Upsert_JsonData()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newJson = new JsonData
        {
            Data = JsonConvert.SerializeObject(new { hello = "world" }),
        };

        dbContext.JsonDatas.Upsert(newJson)
            .Run();

        dbContext.JsonDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => JToken.DeepEquals(JObject.Parse(newJson.Data), JObject.Parse(j.Data)).Should().BeTrue());
    }

    [Fact]
    public void Upsert_JsonData_Update()
    {
        var existingJson = new JsonData
        {
            Data = JsonConvert.SerializeObject(new { hello = "world" }),
        };

        ResetDb(existingJson);
        using (var testContext = new TestDbContext(_fixture.DataContextOptions))
        {
            testContext.JsonDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
                j => JToken.DeepEquals(JObject.Parse(existingJson.Data), JObject.Parse(j.Data)).Should().BeTrue());
        }

        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var updatedJson = new JsonData
        {
            Data = JsonConvert.SerializeObject(new { welcome = "world 2.0" }),
        };

        dbContext.JsonDatas.Upsert(updatedJson)
            .Run();

        dbContext.JsonDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => JToken.DeepEquals(JObject.Parse(updatedJson.Data), JObject.Parse(j.Data)).Should().BeTrue());
    }

    [Fact]
    public void Upsert_JsonData_Update_ComplexObject()
    {
        Assert.SkipWhen(_fixture.DbDriver is not DbDriver.Postgres, "Default values on text columns are only supported in Postgres");

        var existingJson = new JsonData
        {
            Data = JsonConvert.SerializeObject(new { hello = "world" }),
        };

        ResetDb(existingJson);
        using (var testContext = new TestDbContext(_fixture.DataContextOptions))
        {
            testContext.JsonDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
                j => JToken.DeepEquals(JObject.Parse(existingJson.Data), JObject.Parse(j.Data)).Should().BeTrue());
        }

        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var timestamp = new DateTime(2021, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        var updatedJson = new JsonData
        {
            Child = new ChildObject { Value = "test", Time = timestamp }
        };

        dbContext.JsonDatas.Upsert(updatedJson)
            .Run();

        dbContext.JsonDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => j.Child.Time.Should().Be(timestamp));
    }

    [Fact]
    public void Upsert_JsonDocumentData()
    {
        Assert.SkipWhen(_fixture.DbDriver is not DbDriver.Postgres, "Owned json is only supported for postgres");

        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newJson = new JsonDocumentData
        {
            Data = System.Text.Json.JsonSerializer.SerializeToDocument(new { hello = "world" }),
        };

        dbContext.JsonDocumentDatas.Upsert(newJson)
            .Run();

        dbContext.JsonDocumentDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => System.Text.Json.JsonSerializer.Serialize(newJson.Data).Should().Be(System.Text.Json.JsonSerializer.Serialize(j.Data)));
    }

    [Fact]
    public void Upsert_JsonDocumentData_Update()
    {
        Assert.SkipWhen(_fixture.DbDriver is not DbDriver.Postgres, "Owned json is only supported for postgres");

        var existingJson = new JsonDocumentData
        {
            Data = System.Text.Json.JsonSerializer.SerializeToDocument(new { hello = "world" }),
        };

        ResetDb(existingJson);
        using (var testContext = new TestDbContext(_fixture.DataContextOptions))
        {
            testContext.JsonDocumentDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
                j => System.Text.Json.JsonSerializer.Serialize(existingJson.Data).Should().Be(System.Text.Json.JsonSerializer.Serialize(j.Data)));
        }

        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var updatedJson = new JsonDocumentData
        {
            Data = System.Text.Json.JsonSerializer.SerializeToDocument(new { welcome = "world 2.0" }),
        };

        dbContext.JsonDocumentDatas.Upsert(updatedJson)
            .Run();

        dbContext.JsonDocumentDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => System.Text.Json.JsonSerializer.Serialize(updatedJson.Data).Should().Be(System.Text.Json.JsonSerializer.Serialize(j.Data)));
    }

    [Fact]
    public void Upsert_JsonDocumentData_Update_ComplexObject()
    {
        Assert.SkipWhen(_fixture.DbDriver is not DbDriver.Postgres, "Owned json is only supported for postgres");

        var existingJson = new JsonDocumentData
        {
            Data = System.Text.Json.JsonSerializer.SerializeToDocument(new { hello = "world" }),
        };

        ResetDb(existingJson);
        using (var testContext = new TestDbContext(_fixture.DataContextOptions))
        {
            testContext.JsonDocumentDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
                j => System.Text.Json.JsonSerializer.Serialize(existingJson.Data).Should().Be(System.Text.Json.JsonSerializer.Serialize(j.Data)));
        }

        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var updatedJson = new JsonDocumentData
        {
            Data = System.Text.Json.JsonSerializer.SerializeToDocument(new { hello = "world 2.0" }),
        };

        dbContext.JsonDocumentDatas.Upsert(updatedJson)
            .WhenMatched((a, b) => new JsonDocumentData
            {
                Data = b.Data,
            })
            .Run();

        dbContext.JsonDocumentDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => j.Data.RootElement.GetProperty("hello").GetString().Should().Be("world 2.0"));
    }

    [Fact]
    public void Upsert_GuidKey_AutoGenThrows()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new GuidKeyAutoGen
        {
            ID = Guid.NewGuid(),
            Name = "test",
        };

        Action action = () => dbContext.GuidKeysAutoGen.Upsert(newItem)
            .Run();
        action.Should().Throw<InvalidMatchColumnsException>();
    }

    [Fact]
    public void Upsert_StringKey_AutoGenThrows()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new StringKeyAutoGen
        {
            ID = Guid.NewGuid().ToString(),
            Name = "test",
        };

        Action action = () => dbContext.StringKeysAutoGen.Upsert(newItem)
            .Run();
        action.Should().Throw<InvalidMatchColumnsException>();
    }

    [Fact]
    public void Upsert_GuidKey()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new GuidKey
        {
            ID = Guid.NewGuid(),
            Name = "test",
        };

        dbContext.GuidKeys.Upsert(newItem)
            .Run();

        dbContext.GuidKeys.OrderBy(j => j.ID).Should().SatisfyRespectively(
            k => k.ID.Should().Be(newItem.ID));
    }

    [Fact]
    public void Upsert_StringKey()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new StringKey
        {
            ID = Guid.NewGuid().ToString(),
            Name = "test",
        };

        dbContext.StringKeys.Upsert(newItem)
            .Run();

        dbContext.StringKeys.OrderBy(j => j.ID).Should().SatisfyRespectively(
            k => k.ID.Should().Be(newItem.ID));
    }

    [Fact]
    public void Upsert_KeyOnly()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new KeyOnly
        {
            ID1 = 123,
            ID2 = 456,
        };

        dbContext.KeyOnlies.Upsert(newItem)
            .Run();

        dbContext.KeyOnlies.OrderBy(j => j.ID1).Should().SatisfyRespectively(
            k => (k.ID1, k.ID2).Should().Be((newItem.ID1, newItem.ID2)));
    }

    [Fact]
    public void Upsert_NullableKeys()
    {
        Assert.SkipWhen(
            _fixture.DbDriver is DbDriver.MySQL or DbDriver.Postgres or DbDriver.Sqlite or DbDriver.Oracle,
            "Nullable keys are not supported in this database");

        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem1 = new NullableCompositeKey
        {
            ID1 = 1,
            ID2 = 3,
            Value = "Third",
        };
        var newItem2 = new NullableCompositeKey
        {
            ID1 = 1,
            ID2 = null,
            Value = "Fourth",
        };

        dbContext.NullableCompositeKeys.UpsertRange(newItem1, newItem2)
            .On(j => new { j.ID1, j.ID2 })
            .Run();

        dbContext.NullableCompositeKeys.OrderBy(j => j.ID1).ThenBy(j => j.ID2).Should().SatisfyRespectively(
            k => (k.ID1, k.ID2, k.Value).Should().Be((1, null, "Fourth")),
            k => (k.ID1, k.ID2, k.Value).Should().Be((1, 2, "First")),
            k => (k.ID1, k.ID2, k.Value).Should().Be((1, 3, "Third")));
    }

    [Fact]
    public void Upsert_WithNullValues()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem2 = new TestEntity();
        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "Test",
            Text2 = "Value",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .WhenMatched((je, jn) => new TestEntity
            {
                Text1 = newItem2.Text1,
                Text2 = null,
            })
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem));
    }

    [Fact]
    public void Upsert_CompositeExpression_New()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .WhenMatched((je, jn) => new TestEntity
            {
                Num2 = je.Num2 * 2 + jn.Num2,
            })
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem));
    }

    [Fact]
    public void Upsert_CompositeExpression_Update()
    {
        var dbItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Text1 = "who",
            Text2 = "where",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .WhenMatched((je, jn) => new TestEntity
            {
                Num2 = je.Num2 * 2 + jn.Num2,
            })
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(dbItem, num2: dbItem.Num2 * 2 + newItem.Num2));
    }

    [Fact]
    public void Upsert_ConditionalExpression_New()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .WhenMatched((je, jn) => new TestEntity
            {
                Num2 = je.Num2 - jn.Num2 > 0 ? je.Num2 - jn.Num2 : 0,
            })
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem));
    }

    [Fact]
    public void Upsert_ConditionalExpression_UpdateTrue()
    {
        var dbItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Text1 = "who",
            Text2 = "where",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .WhenMatched((je, jn) => new TestEntity
            {
                Num2 = je.Num2 - jn.Num2 > 0 ? je.Num2 - jn.Num2 : 0,
            })
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(dbItem, num2: dbItem.Num2 - newItem.Num2));
    }

    [Fact]
    public void Upsert_ConditionalExpression_UpdateFalse()
    {
        var dbItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 22,
            Text1 = "who",
            Text2 = "where",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .WhenMatched((je, jn) => new TestEntity
            {
                Num2 = je.Num2 - jn.Num2 > 0 ? je.Num2 - jn.Num2 : 0,
            })
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(dbItem, num2: 0));
    }

    [Fact]
    public void Upsert_ConditionalExpression_CoalesceCheck()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .WhenMatched((old, ins) => new TestEntity
            {
                Text1 = ins.Text1 ?? old.Text1,
            })
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem));
    }

    [Fact]
    public void Upsert_ConditionalExpression_NullValueCheck()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .WhenMatched((old, ins) => new TestEntity
            {
                Text1 = ins.Text1 == null ? old.Text1 : ins.Text1,
            })
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem));
    }

    [Fact]
    public void Upsert_ExcludeExpression_New()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);
        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };
        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .Exclude(e => e.Text2)
            .Run();
        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem));
    }

    [Fact]
    public void Upsert_ExcludeExpression_Update()
    {
        var dbItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };
        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);
        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Text1 = "who",
            Text2 = "where",
        };
        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .Exclude(e => e.Text2)
            .Run();
        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem, text2: dbItem.Text2));
    }

    [Fact]
    public void Upsert_Exclude_WithUpdate_ThrowsInvalidOperationException()
    {
        var dbItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };
        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);
        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Text1 = "who",
            Text2 = "where",
        };
        var action = () => dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .Exclude(e => e.Text2)
            .WhenMatched((e1, e2) => new TestEntity
            {
                Text2 = e2.Text2,
            })
            .Run();
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Upsert_UpdateCondition_Constant()
    {
        var dbItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Text1 = "who",
            Text2 = "where",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .WhenMatched((e1, e2) => new TestEntity
            {
                Num2 = e2.Num2,
            })
            .UpdateIf((ed, en) => en.Num2 == 2)
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(dbItem, num2: newItem.Num2));
    }

    [Fact]
    public void Upsert_UpdateCondition_New()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .WhenMatched((e1, e2) => new TestEntity
            {
                Num2 = e2.Num2,
            })
            .UpdateIf((ed, en) => ed.Num2 != en.Num2)
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem));
    }

    [Fact]
    public void Upsert_UpdateCondition_New_AutoUpdate()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .UpdateIf((ed, en) => ed.Num2 != en.Num2)
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem));
    }

    [Fact]
    public void Upsert_UpdateCondition_Update()
    {
        var dbItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Text1 = "who",
            Text2 = "where",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .WhenMatched((e1, e2) => new TestEntity
            {
                Num2 = e2.Num2,
            })
            .UpdateIf((ed, en) => ed.Num2 != en.Num2 || ed.Text1 != en.Text1)
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(dbItem, num2: newItem.Num2));
    }

    [Fact]
    public void Upsert_UpdateCondition_AutoUpdate()
    {
        var dbItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Text1 = "who",
            Text2 = "where",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .UpdateIf((ed, en) => ed.Num2 != en.Num2)
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem));
    }

    [Fact]
    public void Upsert_UpdateCondition_NoUpdate()
    {
        var dbItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };

        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "who",
            Text2 = "where",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .UpdateIf((ed, en) => ed.Num2 != en.Num2)
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(dbItem));
    }

    [Fact]
    public void Upsert_UpdateCondition_NullCheck()
    {
        var dbItem1 = new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Text1 = "hello",
        };
        var dbItem2 = new TestEntity
        {
            Num1 = 2,
            Num2 = 3,
            Text1 = null
        };

        ResetDb(dbItem1, dbItem2);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        dbContext.TestEntities.UpsertRange(dbItem1, dbItem2)
            .On(j => j.Num1)
            .WhenMatched(j => new TestEntity
            {
                Num2 = j.Num2 + 1,
            })
            .UpdateIf(j => j.Text1 != null)
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(dbItem1, num2: dbItem1.Num2 + 1),
            test => test.Should().MatchModel(dbItem2));
    }

    [Fact]
    public void Upsert_UpdateCondition_ValueCheck()
    {
        var dbItem1 = new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Text1 = "hello",
        };
        var dbItem2 = new TestEntity
        {
            Num1 = 2,
            Num2 = 3,
            Text1 = null
        };

        ResetDb(dbItem1, dbItem2);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        dbContext.TestEntities.UpsertRange(dbItem1, dbItem2)
            .On(j => j.Num1)
            .WhenMatched((j, i) => new TestEntity
            {
                Num2 = j.Num2 + 1,
            })
            .UpdateIf(j => j.Text1 == "hello")
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(dbItem1, num2: dbItem1.Num2 + 1),
            test => test.Should().MatchModel(dbItem2));
    }

    [Fact]
    public void Upsert_UpdateCondition_ValueCheck_UpdateColumnFromCondition()
    {
        var dbItem1 = new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Text1 = "hello",
        };
        var dbItem2 = new TestEntity
        {
            Num1 = 2,
            Num2 = 3,
            Text1 = null
        };

        ResetDb(dbItem1, dbItem2);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        dbContext.TestEntities.UpsertRange(dbItem1, dbItem2)
            .On(j => j.Num1)
            .WhenMatched((j, i) => new TestEntity
            {
                Num2 = j.Num2 + 1,
                Text1 = "world",
            })
            .UpdateIf(j => j.Text1 == "hello")
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(dbItem1, num2: dbItem1.Num2 + 1, text1: "world"),
            test => test.Should().MatchModel(dbItem2));
    }

    [Fact]
    public void Upsert_WhenMatched_ULongCounter_Increment()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        var dbItem = new ULongEntity
        {
            Num1 = 1,
            Counter = 41UL,
        };

        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new ULongEntity
        {
            Num1 = 1,
            Counter = 0UL,
        };

        var result = dbContext.ULongEntities.Upsert(newItem)
            .On(e => e.Num1)
            .WhenMatched(e => new ULongEntity
            {
                Counter = (e.Counter * 2) + 1UL,
            })
            .RunAndReturn();

        result.Should().SatisfyRespectively(
            e =>
            {
                e.Num1.Should().Be(1);
                e.Counter.Should().Be((41UL * 2UL) + 1UL);
            });

        dbContext.ULongEntities.OrderBy(e => e.ID).Should().SatisfyRespectively(
            e =>
            {
                e.Num1.Should().Be(1);
                e.Counter.Should().Be((41UL * 2UL) + 1UL);
            });
    }

    [Fact]
    public void Upsert_WhenMatched_ULongCounter_SetConstant_RunAndReturn()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        var dbItem = new ULongEntity
        {
            Num1 = 1,
            Counter = 123UL,
        };

        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new ULongEntity
        {
            Num1 = 1,
            Counter = 0UL,
        };

        var result = dbContext.ULongEntities.Upsert(newItem)
            .On(e => e.Num1)
            .WhenMatched(e => new ULongEntity
            {
                Counter = 5UL,
            })
            .RunAndReturn();

        result.Should().SatisfyRespectively(
            e =>
            {
                e.Num1.Should().Be(1);
                e.Counter.Should().Be(5UL);
            });

        dbContext.ULongEntities.OrderBy(e => e.ID).Should().SatisfyRespectively(
            e =>
            {
                e.Num1.Should().Be(1);
                e.Counter.Should().Be(5UL);
            });
    }

    [Fact]
    public void Upsert_WhenMatched_ULongCounter_ConditionalConstants_RunAndReturn()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        var dbItem = new ULongEntity
        {
            Num1 = 1,
            Counter = 1UL,
        };

        ResetDb(dbItem);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new ULongEntity
        {
            Num1 = 1,
            Counter = 0UL,
        };

        var result = dbContext.ULongEntities.Upsert(newItem)
            .On(e => e.Num1)
            .WhenMatched(e => new ULongEntity
            {
                Counter = e.Num1 == 1 ? 10UL : 20UL,
            })
            .RunAndReturn();

        result.Should().SatisfyRespectively(
            e =>
            {
                e.Num1.Should().Be(1);
                e.Counter.Should().Be(10UL);
            });

        dbContext.ULongEntities.OrderBy(e => e.ID).Should().SatisfyRespectively(
            e =>
            {
                e.Num1.Should().Be(1);
                e.Counter.Should().Be(10UL);
            });
    }

    [Fact]
    public void UpsertRange_WhenMatched_ULongCounter_Increment_MixedInsertUpdate()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        var existing = new ULongEntity
        {
            Num1 = 1,
            Counter = 1UL,
        };

        ResetDb(existing);
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var upserts = new[]
        {
            new ULongEntity { Num1 = 1, Counter = 0UL },
            new ULongEntity { Num1 = 2, Counter = 5UL },
        };

        var result = dbContext.ULongEntities.UpsertRange(upserts)
            .On(e => e.Num1)
            .WhenMatched(e => new ULongEntity
            {
                Counter = e.Counter + 1UL,
            })
            .RunAndReturn();

        result.Should().SatisfyRespectively(
            e =>
            {
                e.Num1.Should().Be(1);
                e.Counter.Should().Be(2UL);
            },
            e =>
            {
                e.Num1.Should().Be(2);
                e.Counter.Should().Be(5UL);
            });

        dbContext.ULongEntities.OrderBy(e => e.Num1).Should().SatisfyRespectively(
            e1 =>
            {
                e1.Num1.Should().Be(1);
                e1.Counter.Should().Be(2UL);
            },
            e2 =>
            {
                e2.Num1.Should().Be(2);
                e2.Counter.Should().Be(5UL);
            });
    }

    [Fact]
    public void Upsert_HandleImplicitConversions()
    {
        ResetDb(new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Short1 = 3,
        });
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newEntity = new TestEntity
        {
            Num1 = 1,
            Short1 = 4,
        };

        dbContext.TestEntities.Upsert(newEntity)
            .On(c => c.Num1)
            .UpdateIf((a, b) => a.Short1 != b.Short1)
            .WhenMatched((a, b) => new TestEntity
            {
                Num2 = a.Short1 + b.Short1,
                Short1 = (short)(a.Short1 - b.Short1),
            })
            .Run();

        dbContext.TestEntities.OrderBy(c => c.ID).Should().SatisfyRespectively(
            r =>
            {
                r.Num2.Should().Be(7);
                r.Short1.Should().Be(-1);
            });
    }

    [Fact]
    public void Upsert_NullableRequired_Insert()
    {
        Assert.SkipWhen(_fixture.DbDriver == DbDriver.MySQL, "Default values on text columns not supported in MySQL");

        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newEntity = new NullableRequired
        {
            ID = 1
        };

        dbContext.NullableRequireds.Upsert(newEntity)
            .On(c => c.ID)
            .Run();

        dbContext.NullableRequireds.OrderBy(c => c.ID).Should().SatisfyRespectively(
            r => r.Text.Should().Be("B"));
    }

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

    [Fact]
    public async Task Upsert_WithQueryFilter_RunAndReturn_Inserts()
    {
        Assert.SkipWhen(_fixture.DbDriver is DbDriver.MySQL or DbDriver.Oracle, "Returning records is not implemented in MySQL and Oracle");

        ResetDb();

        await using var dbContext = new TestDbContext(_fixture.DataContextOptions);

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

        ResetDb(dbItem1);
        await using var dbContext = new TestDbContext(_fixture.DataContextOptions);

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
