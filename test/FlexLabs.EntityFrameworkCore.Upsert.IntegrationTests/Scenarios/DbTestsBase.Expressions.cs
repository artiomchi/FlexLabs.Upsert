using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void Upsert_WithNullValues()
    {
        using var dbContext = Arrange();

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

        using var dbContext = Arrange(dbItem);

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
            .WhenMatched((old, ins) => new TestEntity
            {
                Text1 = ins.Text1 == null ? old.Text1 : ins.Text1,
            })
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Should().MatchModel(newItem));
    }

    [Fact]
    public void Upsert_HandleImplicitConversions()
    {
        using var dbContext = Arrange(new TestEntity
        {
            Num1 = 1,
            Num2 = 2,
            Short1 = 3,
        });

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
}
