using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
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
}
