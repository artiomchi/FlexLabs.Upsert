using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
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
}
