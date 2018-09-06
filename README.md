FlexLabs.Upsert
==========

[![Build status](https://ci.appveyor.com/api/projects/status/a64hu4iyx7r4a3yo?svg=true)](https://ci.appveyor.com/project/artiomchi/flexlabs-upsert)
[![FlexLabs.EntityFrameworkCore.Upsert on NuGet](https://img.shields.io/nuget/v/FlexLabs.EntityFrameworkCore.Upsert.svg)](https://www.nuget.org/packages/FlexLabs.EntityFrameworkCore.Upsert)  
CI build: [![FlexLabs.EntityFrameworkCore.Upsert on MyGet](https://img.shields.io/myget/artiomchi/vpre/FlexLabs.EntityFrameworkCore.Upsert.svg)](https://github.com/artiomchi/FlexLabs.Upsert/wiki/CI-Builds)

Adds basic support for "Upsert" operations to EF Core.

Uses `INSERT … ON CONFLICT DO UPDATE` in PostgreSQL, `MERGE` in SqlServer and `INSERT INTO … ON DUPLICATE KEY UPDATE` in MySQL.

Also supports injecting sql command generators to add support for other providers

### Usage:

In it's simplest form, it can be used as follows:
```csharp
DataContext.Countries.Upsert(new Country
    {
        Name = "Australia",
        ISO = "AU",
    })
    .On(c => c.ISO)
    .RunAsync();
```

The first parameter will be used to insert new entries to the table, the second one is used to identify the columns used to find matching rows.  
If the entry already exists, the command will update the remaining columns to match the entity passed in the first argument.

In some cases, you don't want ALL the entities to be changed. An example field that you wouldn't want updated is the `Created` field. You can use a third parameter to select which columns and values to set in case the entity already exists:
```csharp
DataContext.Countries.Upsert(new Country
    {
        Name = "Australia",
        ISO = "AU",
        Created = DateTime.UtcNow,
    })
    .On(c => c.ISO)
    .WhenMatched(c => new Country
    {
        Name = "Australia"
        Updated = DateTime.UtcNow,
    })
    .RunAsync();
```

Finally, sometimes you might want to update a column based on the current value in the table. For example, if you want to increment a column. You can use the following syntax (basic support for incrementing and decrementing values is currently implemented):  
You can also see how to implement the multi column record matching:
```csharp
DataContext.DailyVisits.Upsert(new DailyVisit
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
