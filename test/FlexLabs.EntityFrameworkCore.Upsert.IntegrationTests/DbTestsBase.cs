using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    protected readonly DatabaseInitializerFixture _fixture;

    public DbTestsBase(DatabaseInitializerFixture fixture)
    {
        _fixture = fixture;
    }

    protected int GeneratedAlwaysAsIdentity_NextId { get; private set; }

    private void ResetDb()
    {
        using var dbContext = NewContext();

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
        Reset(dbContext, e => e.SentinelEntities);
        Reset(dbContext, e => e.SqlDefaultEntities);
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

    /// <summary>Skips the current test unless the database under test supports <paramref name="capability"/>.</summary>
    protected void Require(Capability capability)
        => Assert.SkipUnless(ProviderCapabilities.Supports(_fixture.DbDriver, capability), ProviderCapabilities.SkipReason(capability));

    protected TestDbContext NewContext()
        => new(_fixture.DataContextOptions);

    /// <summary>
    /// Resets the database to the standard seed data, adds any extra <paramref name="seed"/> entities,
    /// and returns a fresh context to run the test against.
    /// </summary>
    protected TestDbContext Arrange(params object[] seed)
    {
        ResetDb();
        if (seed.Length > 0)
        {
            using var seedContext = NewContext();
            seedContext.AddRange(seed);
            seedContext.SaveChanges();
        }

        return NewContext();
    }
}
