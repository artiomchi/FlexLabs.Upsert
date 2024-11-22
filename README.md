FlexLabs.Upsert
==========

[![Build status](https://ci.appveyor.com/api/projects/status/a64hu4iyx7r4a3yo?svg=true)](https://ci.appveyor.com/project/artiomchi/flexlabs-upsert)
[![FlexLabs.EntityFrameworkCore.Upsert on NuGet](https://img.shields.io/nuget/v/FlexLabs.EntityFrameworkCore.Upsert.svg)](https://www.nuget.org/packages/FlexLabs.EntityFrameworkCore.Upsert)  
CI build: [![FlexLabs.EntityFrameworkCore.Upsert on MyGet](https://img.shields.io/myget/artiomchi/vpre/FlexLabs.EntityFrameworkCore.Upsert.svg)](https://github.com/artiomchi/FlexLabs.Upsert/wiki/CI-Builds)

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