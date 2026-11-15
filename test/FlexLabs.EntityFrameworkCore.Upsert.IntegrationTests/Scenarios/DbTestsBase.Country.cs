using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
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
}
