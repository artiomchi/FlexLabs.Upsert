# FlexLabs.Upsert

[![Build status](https://img.shields.io/github/actions/workflow/status/artiomchi/FlexLabs.Upsert/build.yml?branch=main)](https://github.com/artiomchi/FlexLabs.Upsert/actions/workflows/build.yml?query=branch%3Amain)
[![FlexLabs.EntityFrameworkCore.Upsert on NuGet](https://img.shields.io/nuget/v/FlexLabs.EntityFrameworkCore.Upsert.svg)](https://www.nuget.org/packages/FlexLabs.EntityFrameworkCore.Upsert)

Upsert support for EF Core: insert a row, or update it if it already exists, in a single database command.

EF Core has no built-in way to do this. The usual workaround is to query first and then insert or update, which costs
an extra round trip and breaks down when two requests race each other. This library generates the native upsert
statement for your database instead, so the check and the write happen atomically.

| Database | Generated statement |
|---|---|
| PostgreSQL, SQLite | `INSERT … ON CONFLICT DO UPDATE` |
| SQL Server, Oracle | `MERGE` |
| MySQL, MariaDB | `INSERT … ON DUPLICATE KEY UPDATE` |
| In-memory | Emulated, for tests |

## Getting started

```sh
dotnet add package FlexLabs.EntityFrameworkCore.Upsert
```

The package's major version follows EF Core's: 11.x is built for EF Core 11, 10.x for EF Core 10, and so on.
The provider is picked up from your `DbContext` configuration, so there is nothing to register.

Then, with `using Microsoft.EntityFrameworkCore;` in scope:

```csharp
await db.DailyVisits
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

If there is no visit for that user and date, a new row is inserted. If there is one, its `Visits` column is
incremented, in SQL, without reading the current value first.

## What you can do

```csharp
// Match on a unique key, then update everything else with the new values
db.Countries.Upsert(country).On(c => c.ISO).Run();

// Upsert many rows at once
db.Countries.UpsertRange(countries).On(c => c.ISO).Run();

// Keep some columns untouched when the row already exists
db.Countries.Upsert(country).On(c => c.ISO).Exclude(c => c.Created).Run();

// Insert only, leave existing rows alone
db.Countries.Upsert(country).On(c => c.ISO).NoUpdate().Run();

// Only update when a condition holds
db.Products.Upsert(product).On(p => p.Sku)
    .UpdateIf((existing, incoming) => incoming.Version > existing.Version)
    .Run();

// Get the resulting entities back (PostgreSQL, SQL Server, SQLite)
var saved = await db.Countries.Upsert(country).On(c => c.ISO).RunAndReturnAsync();
```

Things to know before you start:

* The `On()` columns must be a primary key or have a unique index, and can't be database-generated.
  If you leave `On()` out, the primary key is used.
* Upserts run immediately, straight against the database. `SaveChanges()` isn't involved, and nothing is added to
  the change tracker unless you use `RunAndReturn()`.
* Databases limit how many parameters a command can take, so a large `UpsertRange` is split into several commands.
  That keeps it working, but it is still one parameter per column per row, so it isn't a replacement for a bulk
  loader.

## Documentation

* [Usage guide](https://github.com/artiomchi/FlexLabs.Upsert/blob/main/docs/usage.md): every method, with examples
* [Matching rows](https://github.com/artiomchi/FlexLabs.Upsert/blob/main/docs/matching.md): choosing the `On()` columns
* [Update expressions](https://github.com/artiomchi/FlexLabs.Upsert/blob/main/docs/update-expressions.md): what you can write in `WhenMatched()` and `UpdateIf()`
* [Default values](https://github.com/artiomchi/FlexLabs.Upsert/blob/main/docs/default-values.md): how `HasDefaultValue()` and sentinels are handled
* [Owned types and JSON](https://github.com/artiomchi/FlexLabs.Upsert/blob/main/docs/owned-and-json.md)
* [Database providers](https://github.com/artiomchi/FlexLabs.Upsert/blob/main/docs/providers.md): per-database differences and limitations, custom runners

## Contributing

Issues and pull requests are welcome. See [test/README.md](https://github.com/artiomchi/FlexLabs.Upsert/blob/main/test/README.md)
for how to run the tests.

## License

[MIT](https://github.com/artiomchi/FlexLabs.Upsert/blob/main/LICENSE)
