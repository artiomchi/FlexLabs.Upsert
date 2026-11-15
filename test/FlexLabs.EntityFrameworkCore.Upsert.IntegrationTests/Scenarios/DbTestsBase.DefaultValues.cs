using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
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

    [Fact(DisplayName = "Insert with CLR default value (0) should use HasDefaultValue(27)")]
    public void Upsert_ValueType_DefaultValue_Insert()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 0,
            Text1 = "Test",
            Text2 = "Value",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Num2.Should().Be(27));
    }

    [Fact(DisplayName = "Insert with explicit non-default value (42) should preserve it despite HasDefaultValue(27)")]
    public void Upsert_ValueType_ExplicitValue_Insert()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new TestEntity
        {
            Num1 = 1,
            Num2 = 42,
            Text1 = "Test",
            Text2 = "Value",
        };

        dbContext.TestEntities.Upsert(newItem)
            .On(j => j.Num1)
            .Run();

        dbContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Num2.Should().Be(newItem.Num2));
    }

    [Fact(DisplayName = "With HasSentinel(-1), inserting Num2=0 should preserve 0 (not sentinel)")]
    public void Upsert_ValueType_CustomSentinel_Insert()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new SentinelEntity
        {
            Num1 = 1,
            Num2 = 0,
        };

        dbContext.SentinelEntities.Upsert(newItem)
            .On(j => j.Num1)
            .Run();

        dbContext.SentinelEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Num2.Should().Be(0));
    }

    [Fact(DisplayName = "With HasSentinel(-1), inserting Num2=-1 should use HasDefaultValue(27)")]
    public void Upsert_ValueType_CustomSentinel_UsesDefault()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new SentinelEntity
        {
            Num1 = 1,
            Num2 = -1,
        };

        dbContext.SentinelEntities.Upsert(newItem)
            .On(j => j.Num1)
            .Run();

        dbContext.SentinelEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Num2.Should().Be(27));
    }

    [Fact(DisplayName = "Update with CLR default value (0) on existing row should update to HasDefaultValue(27)")]
    public void Upsert_ValueType_DefaultValue_Update()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        dbContext.TestEntities.Add(new TestEntity { Num1 = 1, Num2 = 50, Text1 = "Old", Text2 = "Old" });
        dbContext.SaveChanges();

        dbContext.TestEntities.Upsert(new TestEntity { Num1 = 1, Num2 = 0, Text1 = "New", Text2 = "New" })
            .On(j => j.Num1)
            .Run();

        using var verifyContext = new TestDbContext(_fixture.DataContextOptions);
        verifyContext.TestEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test =>
            {
                test.Text1.Should().Be("New");
                test.Num2.Should().Be(27);
            });
    }

    [Fact(DisplayName = "Mixed batch: sentinel values use the default on both insert and update")]
    public void Upsert_ValueType_DefaultValue_MixedBatch()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        dbContext.TestEntities.AddRange(
            new TestEntity { Num1 = 1, Num2 = 50, Text1 = "Old", Text2 = "Old" },
            new TestEntity { Num1 = 2, Num2 = 60, Text1 = "Old", Text2 = "Old" },
            new TestEntity { Num1 = 4, Num2 = 80, Text1 = "Old", Text2 = "Old" });
        dbContext.SaveChanges();

        dbContext.TestEntities.UpsertRange(
                new TestEntity { Num1 = 1, Num2 = 0, Text1 = "New", Text2 = "New" },
                new TestEntity { Num1 = 2, Num2 = 70, Text1 = "New", Text2 = "New" },
                new TestEntity { Num1 = 3, Num2 = 0, Text1 = "New", Text2 = "New" },
                new TestEntity { Num1 = 4, Num2 = 0, Text1 = "New", Text2 = "New" },
                new TestEntity { Num1 = 5, Num2 = 90, Text1 = "New", Text2 = "New" })
            .On(j => j.Num1)
            .Run();

        using var verifyContext = new TestDbContext(_fixture.DataContextOptions);
        verifyContext.TestEntities.OrderBy(t => t.Num1).Should().SatisfyRespectively(
            test => { test.Num1.Should().Be(1); test.Num2.Should().Be(27); test.Text1.Should().Be("New"); },
            test => { test.Num1.Should().Be(2); test.Num2.Should().Be(70); test.Text1.Should().Be("New"); },
            test => { test.Num1.Should().Be(3); test.Num2.Should().Be(27); test.Text1.Should().Be("New"); },
            test => { test.Num1.Should().Be(4); test.Num2.Should().Be(27); test.Text1.Should().Be("New"); },
            test => { test.Num1.Should().Be(5); test.Num2.Should().Be(90); test.Text1.Should().Be("New"); });
    }

    [Fact(DisplayName = "Update with CLR default value (0) on existing row should update to HasDefaultValueSql")]
    public void Upsert_ValueType_DefaultValueSql_Update()
    {
        Assert.SkipWhen(_fixture.DbDriver == DbDriver.InMemory, "In memory db doesn't support default sql values");
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var created = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        dbContext.SqlDefaultEntities.Add(new SqlDefaultEntity { Num1 = 1, Num2 = 50, Created = created });
        dbContext.SaveChanges();

        dbContext.SqlDefaultEntities.Upsert(new SqlDefaultEntity { Num1 = 1, Num2 = 0, Created = created.AddDays(1) })
            .On(j => j.Num1)
            .Run();

        using var verifyContext = new TestDbContext(_fixture.DataContextOptions);
        verifyContext.SqlDefaultEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test =>
            {
                test.Num2.Should().Be(33);
                test.Created.Should().Be(created.AddDays(1));
            });
    }

    [Fact(DisplayName = "WhenMatched expression should see the default value in place of a sentinel value")]
    public void Upsert_ValueType_DefaultValue_WhenMatched_UsesDefault()
    {
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        dbContext.SentinelEntities.Add(new SentinelEntity { Num1 = 1, Num2 = 5 });
        dbContext.SaveChanges();

        dbContext.SentinelEntities.Upsert(new SentinelEntity { Num1 = 1, Num2 = -1 })
            .On(j => j.Num1)
            .WhenMatched((existing, incoming) => new SentinelEntity { Num2 = existing.Num2 + incoming.Num2 })
            .Run();

        using var verifyContext = new TestDbContext(_fixture.DataContextOptions);
        verifyContext.SentinelEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test => test.Num2.Should().Be(5 + 27));
    }

    [Fact(DisplayName = "Match column with a default value should match using the resolved default")]
    public void Upsert_ValueType_DefaultValue_MatchColumn()
    {
        Assert.SkipWhen(_fixture.DbDriver != DbDriver.InMemory, "Match columns with default values need a custom schema, so it's only tested with the in-memory provider");
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);
        dbContext.InMemoryDefaultsEntities.RemoveRange(dbContext.InMemoryDefaultsEntities);
        dbContext.InMemoryDefaultsEntities.Add(new InMemoryDefaultsEntity { Num1 = 27, Text = "Old" });
        dbContext.SaveChanges();

        dbContext.InMemoryDefaultsEntities.Upsert(new InMemoryDefaultsEntity { Num1 = 0, Text = "New" })
            .On(j => j.Num1)
            .WhenMatched(e => new InMemoryDefaultsEntity { Text = "New" })
            .Run();

        using var verifyContext = new TestDbContext(_fixture.DataContextOptions);
        verifyContext.InMemoryDefaultsEntities.Should().SatisfyRespectively(
            test =>
            {
                test.Num1.Should().Be(27);
                test.Text.Should().Be("New");
            });
    }

    [Fact(DisplayName = "Default values should be applied to properties without a setter")]
    public void Upsert_ValueType_DefaultValue_PropertyWithoutSetter()
    {
        Assert.SkipWhen(_fixture.DbDriver != DbDriver.InMemory, "Properties without a setter need a custom schema, so it's only tested with the in-memory provider");
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);
        dbContext.InMemoryDefaultsEntities.RemoveRange(dbContext.InMemoryDefaultsEntities);
        dbContext.InMemoryDefaultsEntities.Add(new InMemoryDefaultsEntity { Num1 = 1, Text = "Old" });
        dbContext.SaveChanges();

        // Update a different property, for an entity that has a defaulted property without a setter
        dbContext.InMemoryDefaultsEntities.Upsert(new InMemoryDefaultsEntity { Num1 = 1, Text = "New" })
            .On(j => j.Num1)
            .WhenMatched(e => new InMemoryDefaultsEntity { Text = "New" })
            .Run();

        // Insert
        dbContext.InMemoryDefaultsEntities.Upsert(new InMemoryDefaultsEntity { Num1 = 2, Text = "Insert" })
            .On(j => j.Num1)
            .Run();

        using var verifyContext = new TestDbContext(_fixture.DataContextOptions);
        verifyContext.InMemoryDefaultsEntities.OrderBy(t => t.Num1).Should().SatisfyRespectively(
            test =>
            {
                test.Num1.Should().Be(1);
                test.Text.Should().Be("New");
            },
            test =>
            {
                test.Num1.Should().Be(2);
                test.Text.Should().Be("Insert");
                test.ReadOnly.Should().Be(27);
            });
    }

    [Fact(DisplayName = "Insert with CLR default value (0) should be kept when HasDefaultValue() has no value specified")]
    public void Upsert_ValueType_UnspecifiedDefaultValue_Insert()
    {
        Assert.SkipWhen(_fixture.DbDriver != DbDriver.InMemory, "Relational providers can't create a schema for a column with an unspecified default value");
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);
        dbContext.UnspecifiedDefaultEntities.RemoveRange(dbContext.UnspecifiedDefaultEntities);
        dbContext.SaveChanges();

        dbContext.UnspecifiedDefaultEntities.UpsertRange(
                new UnspecifiedDefaultEntity { Num1 = 1, Num2 = 0 },
                new UnspecifiedDefaultEntity { Num1 = 2, Num2 = 5 })
            .On(j => j.Num1)
            .Run();

        using var verifyContext = new TestDbContext(_fixture.DataContextOptions);
        verifyContext.UnspecifiedDefaultEntities.OrderBy(t => t.Num1).Should().SatisfyRespectively(
            test => { test.Num1.Should().Be(1); test.Num2.Should().Be(0); },
            test => { test.Num1.Should().Be(2); test.Num2.Should().Be(5); });
    }

    [Fact(DisplayName = "Insert with CLR default value (0) should use HasDefaultValueSql")]
    public void Upsert_ValueType_DefaultValueSql_Insert()
    {
        Assert.SkipWhen(_fixture.DbDriver == DbDriver.InMemory, "In memory db doesn't support default sql values");
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var newItem = new SqlDefaultEntity
        {
            Num1 = 1,
            Num2 = 0,
            Created = default,
            Text = "after-defaults",
        };

        dbContext.SqlDefaultEntities.Upsert(newItem)
            .On(j => j.Num1)
            .Run();

        dbContext.SqlDefaultEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test =>
            {
                test.Num2.Should().Be(33);
                test.Created.Should().BeCloseTo(DateTime.Now, TimeSpan.FromDays(1));
                test.Text.Should().Be("after-defaults");
            });
    }

    [Fact(DisplayName = "Insert with explicit values should preserve them despite HasDefaultValueSql")]
    public void Upsert_ValueType_DefaultValueSql_ExplicitValue_Insert()
    {
        Assert.SkipWhen(_fixture.DbDriver == DbDriver.InMemory, "In memory db doesn't support default sql values");
        ResetDb();
        using var dbContext = new TestDbContext(_fixture.DataContextOptions);

        var created = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var newItem = new SqlDefaultEntity
        {
            Num1 = 1,
            Num2 = 42,
            Created = created,
        };

        dbContext.SqlDefaultEntities.Upsert(newItem)
            .On(j => j.Num1)
            .Run();

        dbContext.SqlDefaultEntities.OrderBy(t => t.ID).Should().SatisfyRespectively(
            test =>
            {
                test.Num2.Should().Be(42);
                test.Created.Should().Be(created);
            });
    }
}
