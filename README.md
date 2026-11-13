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

Partial (filtered) unique indexes
---------------------------------

PostgreSQL and Sqlite will only match against a partial index if the command repeats the index's predicate, so
matching on columns whose only unique index is partial used to fail with `42P10: there is no unique or exclusion
constraint matching the ON CONFLICT specification`.

When the model declares the filter, it is now used automatically:

```csharp
modelBuilder.Entity<DailyVisit>()
    .HasIndex(v => new { v.UserID, v.Date })
    .IsUnique()
    .HasFilter("deleted_at IS NULL");
```

A key or an unfiltered unique index over the same columns is always preferred, so this only affects commands that
could not run before. For an index the model doesn't describe, pass the predicate yourself:

```csharp
    .On(v => new { v.UserID, v.Date })
    .WithIndexPredicate("deleted_at IS NULL")
```

The predicate is written into the command as it is given, so it must be a trusted, static predicate - never user
input.

Providers that match with an explicit condition rather than by inferring an index (SqlServer, Oracle, MySQL) ignore
the predicate.

Please read our [Usage](https://github.com/artiomchi/FlexLabs.Upsert/wiki/Usage) page for more examples