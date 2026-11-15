using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void Upsert_EFCore_KeyAutoGen()
    {
        using var dbContext = Arrange();

        dbContext.GuidKeysAutoGen.Add(new GuidKeyAutoGen { Name = "test" });
        dbContext.StringKeysAutoGen.Add(new StringKeyAutoGen { Name = "test" });
        dbContext.SaveChanges();

        // Ensuring EFCore generates empty values for Guid and string keys
        dbContext.GuidKeysAutoGen.Should().OnlyContain(e => e.ID != default);
        dbContext.StringKeysAutoGen.Should().OnlyContain(e => !string.IsNullOrEmpty(e.ID));
    }

    [Fact]
    public void Upsert_GuidKey_AutoGenThrows()
    {
        using var dbContext = Arrange();

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
        using var dbContext = Arrange();

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
        using var dbContext = Arrange();

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
        using var dbContext = Arrange();

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
        using var dbContext = Arrange();

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
        Require(Capability.NullableKeys);

        using var dbContext = Arrange();

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
}
