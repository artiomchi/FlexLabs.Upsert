using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void Upsert_IdentityKey_NoOn_InvalidMatchColumn()
    {
        using var dbContext = Arrange();

        var newCountry = NewCountry("Germany", "DE");

        Action action = () => dbContext.Countries.Upsert(newCountry).Run();
        action.Should().Throw<InvalidMatchColumnsException>();
    }

    [Fact]
    public void Upsert_IdentityKey_ExplicitOn_InvalidMatchColumn()
    {
        using var dbContext = Arrange();

        var newCountry = NewCountry("Germany", "DE");

        Action action = () => dbContext.Countries.Upsert(newCountry)
            .On(c => c.ID)
            .Run();
        action.Should().Throw<InvalidMatchColumnsException>();
    }

    [Fact]
    public void Upsert_IdentityKey_NoOn_AllowWithOverride()
    {
        using var dbContext = Arrange();

        var newCountry = NewCountry("Germany", "DE");

        dbContext.Countries.Upsert(newCountry)
            .AllowIdentityMatch()
            .Run();

        dbContext.Countries.Should().HaveCount(2);
    }

    [Fact]
    public void Upsert_IdentityKey_ExplicitOn_AllowWithOverride()
    {
        using var dbContext = Arrange();

        var newCountry = NewCountry("Germany", "DE");

        dbContext.Countries.Upsert(newCountry)
            .On(c => c.ID)
            .AllowIdentityMatch()
            .Run();

        dbContext.Countries.Should().HaveCount(2);
    }
}
