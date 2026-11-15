using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

// Seed rows inserted by every Arrange() call, plus small builders for the entities most tests create.
public abstract partial class DbTestsBase
{
    /// <summary>Visit count of the seeded <c>_dbVisit</c> row; WhenMatched expectations are written relative to it.</summary>
    private const int ExistingVisits = 12;

    readonly Country _dbCountry = new()
    {
        Name = "...loading...",
        ISO = "AU",
        Created = NewDateTime(1970, 1, 1),
    };

    readonly PageVisit _dbVisitOld = new()
    {
        UserID = 1,
        Date = _today.AddDays(-1),
        Visits = 10,
        FirstVisit = NewDateTime(1970, 1, 1),
        LastVisit = NewDateTime(1970, 1, 1),
    };

    readonly PageVisit _dbVisit = new()
    {
        UserID = 1,
        Date = _today,
        Visits = ExistingVisits,
        FirstVisit = NewDateTime(1970, 1, 1),
        LastVisit = NewDateTime(1970, 1, 1),
    };

    readonly Status _dbStatus = new()
    {
        ID = 1,
        Name = "Created",
        LastChecked = NewDateTime(1970, 1, 1),
    };

    readonly Book _dbBook = new()
    {
        Name = "The Fellowship of the Ring",
        Genres = new[] { "Fantasy" },
    };

    readonly NullableCompositeKey _nullableKey1 = new()
    {
        ID1 = 1,
        ID2 = 2,
        Value = "First",
    };

    readonly NullableCompositeKey _nullableKey2 = new()
    {
        ID1 = 1,
        ID2 = null,
        Value = "Second",
    };

    readonly ComputedColumn _computedColumn = new()
    {
        Num1 = 1,
        Num2 = 7,
    };

    readonly static DateTime _now = NewDateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
        DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

    readonly static DateTime _today = NewDateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
    readonly int _increment = 8;

    private static DateTime NewDateTime(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        => new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

    // ---- Sample entities: only the values that tests commonly share. Tests override anything that matters to them.

    /// <summary>TestEntity { Num1 = 1, Num2 = 7, "hello", "world" } - typically the row already in the database.</summary>
    private static TestEntity HelloWorldEntity() => new()
    {
        Num1 = 1,
        Num2 = 7,
        Text1 = "hello",
        Text2 = "world",
    };

    /// <summary>TestEntity { Num1 = 1, Num2 = 2, "who", "where" } - typically the incoming row that matches <see cref="HelloWorldEntity"/>.</summary>
    private static TestEntity WhoWhereEntity() => new()
    {
        Num1 = 1,
        Num2 = 2,
        Text1 = "who",
        Text2 = "where",
    };

    /// <summary>A country created and updated "now".</summary>
    private static Country NewCountry(string name, string iso) => new()
    {
        Name = name,
        ISO = iso,
        Created = _now,
        Updated = _now,
    };

    /// <summary>Today's visit for user 1, matching the seeded <c>_dbVisit</c> key, first and last visited "now".</summary>
    private static PageVisit NewVisit(int visits = 1) => new()
    {
        UserID = 1,
        Date = _today,
        Visits = visits,
        FirstVisit = _now,
        LastVisit = _now,
    };
}
