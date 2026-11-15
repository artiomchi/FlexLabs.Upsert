using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void Upsert_WhenMatched_ULongCounter_Increment()
    {
        Require(Capability.ReturningRecords);

        var dbItem = new ULongEntity
        {
            Num1 = 1,
            Counter = 41UL,
        };

        using var dbContext = Arrange(dbItem);

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
        Require(Capability.ReturningRecords);

        var dbItem = new ULongEntity
        {
            Num1 = 1,
            Counter = 123UL,
        };

        using var dbContext = Arrange(dbItem);

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
        Require(Capability.ReturningRecords);

        var dbItem = new ULongEntity
        {
            Num1 = 1,
            Counter = 1UL,
        };

        using var dbContext = Arrange(dbItem);

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
        Require(Capability.ReturningRecords);

        var existing = new ULongEntity
        {
            Num1 = 1,
            Counter = 1UL,
        };

        using var dbContext = Arrange(existing);

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
}
