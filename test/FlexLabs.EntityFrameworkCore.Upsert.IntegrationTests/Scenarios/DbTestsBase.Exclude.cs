using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void Upsert_ExcludeExpression_New()
    {
        using var dbContext = Arrange();
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
        using var dbContext = Arrange(dbItem);
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
        using var dbContext = Arrange(dbItem);
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
    public void Upsert_ExcludeMultiplePropertiesExpression_Update()
    {
        var dbItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 7,
            Text1 = "hello",
            Text2 = "world",
        };
        using var dbContext = Arrange(dbItem);
        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Text1 = "who",
            Text2 = "where",
        };
        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .Exclude(e => e.Text1)
            .Exclude(e => e.Text2)
            .Run();
        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem, text1: dbItem.Text1, text2: dbItem.Text2));
    }

    [Fact]
    public void Upsert_ExcludeSamePropertyTwice_Throws()
    {
        using var dbContext = Arrange();
        var action = () => dbContext.TestEntities.Upsert(new())
            .On(j => j.Num1)
            .Exclude(e => e.Text1)
            .Exclude(e => e.Text1)
            .Run();
        action.Should().Throw<InvalidOperationException>();
    }
}
