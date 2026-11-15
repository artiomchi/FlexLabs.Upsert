FlexLabs.Upsert
==========

[![Build status](https://img.shields.io/github/check-runs/artiomchi/FlexLabs.Upsert/main?logo=github&label=build)](https://github.com/artiomchi/FlexLabs.Upsert/actions/workflows/build.yml?query=branch%3Amain)
[![FlexLabs.EntityFrameworkCore.Upsert on NuGet](https://img.shields.io/nuget/v/FlexLabs.EntityFrameworkCore.Upsert.svg)](https://www.nuget.org/packages/FlexLabs.EntityFrameworkCore.Upsert)  
<!-- CI build: [![FlexLabs.EntityFrameworkCore.Upsert on MyGet](https://img.shields.io/myget/artiomchi/vpre/FlexLabs.EntityFrameworkCore.Upsert.svg)](https://github.com/artiomchi/FlexLabs.Upsert/wiki/CI-Builds) -->

This library adds basic support for "Upsert" operations to EF Core.

Uses `INSERT … ON CONFLICT DO UPDATE` in PostgreSQL/Sqlite, `MERGE` in SqlServer & Oracle and `INSERT INTO … ON DUPLICATE KEY UPDATE` in MySQL.

Also supports injecting sql command runners to add support for other providers

A typical upsert command could look something like this:

```csharp
DataContext.DailyVisits
    .Upsert(new DailyVisit
    {
        UserID = userID,
        Date = DateTime.UtcNow.Date,
        Visits = 1,
    })
    .On(v => new { v.UserID, v.Date })
    .WhenMatched(v => new DailyVisit
    {
        Visits = v.Visits + 1,
    })
    .RunAsync();
```

In this case, the upsert command will ensure that a new `DailyVisit` will be added to the database. If a visit with the same `UserID` and `Date` already exists, it will be updated by incrementing it's `Visits` value by 1.

Please read our [Usage](https://github.com/artiomchi/FlexLabs.Upsert/wiki/Usage) page for more examples

## Default values

If a property has a default configured (`HasDefaultValue()` or `HasDefaultValueSql()`), the default is used whenever the entity's value is `null` or equal to the property's sentinel. By default the sentinel is the CLR default of the type (`0` for `int`, `false` for `bool`, `default(DateTime)`, etc.), which matches how EF Core itself decides that a value is "not set". This applies both when a row is inserted and when an existing row is updated.

If `HasDefaultValue()` is called without a value (meaning that the database has a default, but the model doesn't know what it is), there is nothing to substitute, so the entity's value is always used as it is.

This means that explicitly upserting `0` into an `int` column with `HasDefaultValue(27)` will store `27`. If you need to store the CLR default, change the sentinel so that it's a value you won't use:

```csharp
modelBuilder.Entity<MyEntity>()
    .Property(e => e.MyProperty)
    .HasDefaultValue(27)
    .HasSentinel(-1); // only -1 now means "not set", so 0 is stored as 0
```

**In-memory limitation:** the in-memory provider doesn't evaluate `HasDefaultValueSql()` expressions, so a sentinel value is not replaced by the result of the SQL expression there. `HasDefaultValue()` defaults are applied.

**Oracle limitation:** `HasDefaultValueSql()` expressions are placed in the `SELECT ... FROM dual` queries that make up the `MERGE` source, and these are joined with `UNION ALL` when more than one entity is upserted. Oracle doesn't allow sequence pseudocolumns (`NEXTVAL`, `CURRVAL`) in queries combined this way, so a sequence-based default (e.g. `HasDefaultValueSql("my_seq.NEXTVAL")`) is not supported when upserting multiple entities at once. Constants and ordinary functions (e.g. `SYSTIMESTAMP`) are not affected.
