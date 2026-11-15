# Usage

The examples on this page use these entities:

```csharp
public class Country
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string ISO { get; set; }      // unique index
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}

public class DailyVisit
{
    public int ID { get; set; }
    public int UserID { get; set; }
    public DateTime Date { get; set; }
    public int Visits { get; set; }      // (UserID, Date) has a unique index
}
```

The extension methods live in the `Microsoft.EntityFrameworkCore` namespace, so you only need:

```csharp
using Microsoft.EntityFrameworkCore;
```

## The basics

```csharp
await db.Countries
    .Upsert(new Country { Name = "Australia", ISO = "AU" })
    .On(c => c.ISO)
    .RunAsync();
```

* `Upsert()` takes the entity to insert.
* `On()` says which column(s) identify an existing row. See [Matching rows](matching.md).
* `RunAsync()` sends the command. Nothing happens until you call a `Run` method.

With no `WhenMatched()`, every column except the match columns and database-generated columns is overwritten with
the new values when the row exists. `Upsert()` is available on both `DbSet<T>` and `DbContext`.

`On()` can be left out if your primary key isn't database-generated; the primary key is used for matching.
MySQL ignores `On()` completely and matches on whatever unique key triggers the conflict, see [providers](providers.md#mysql-and-mariadb).

## Upserting several rows

```csharp
await db.Countries
    .UpsertRange(countries)   // params array or IEnumerable
    .On(c => c.ISO)
    .RunAsync();
```

Every row goes into one statement where the database allows it. When a batch would exceed the database's parameter
limit, it is split into several statements.

## Choosing what gets updated

There are three ways to control the update. They can't be combined, apart from `UpdateIf()`, which works with
any of them.

### `WhenMatched()`: say what to update

```csharp
.WhenMatched(c => new Country { Updated = DateTime.UtcNow })
```

You write a `new TEntity { ... }` expression, and only the properties you initialise are updated. The expression
isn't run in .NET: it is translated to SQL. That is what lets it refer to the value currently in the database:

```csharp
.WhenMatched(v => new DailyVisit { Visits = v.Visits + 1 })
```

A second parameter gives you the row you tried to insert, which matters when upserting several rows at once:

```csharp
.WhenMatched((existing, incoming) => new Country
{
    Name = incoming.Name,
    Updated = DateTime.UtcNow,
})
```

More about what you can write in these expressions in [Update expressions](update-expressions.md).

### `Exclude()`: update everything except

```csharp
.Exclude(c => c.Created)
.Exclude(c => new { c.Created, c.Name })   // or several at once
```

All other columns are updated with the new values. `Exclude()` can't be used together with `WhenMatched()`
(listing the columns you want in `WhenMatched()` already does the same job).

### `NoUpdate()`: insert only

```csharp
.NoUpdate()
```

Existing rows are left alone. Handy for seeding reference data.

### `UpdateIf()`: only update when a condition holds

```csharp
.UpdateIf(existing => existing.Visits < 100)

.UpdateIf((existing, incoming) => incoming.Version > existing.Version)
```

Rows where the condition is false are left as they are. This can be combined with `WhenMatched()` or `Exclude()`.

## Running the command

| Method | Returns |
|---|---|
| `Run()` / `RunAsync()` | The number of affected rows, as reported by the database |
| `RunAndReturn()` / `RunAndReturnAsync()` | The upserted entities, as stored in the database |

`RunAndReturn` is available for PostgreSQL, SQL Server, SQLite and the in-memory provider. The returned entities are
attached to the `DbContext`, with values read back from the database (including generated IDs and defaults). Global
query filters and auto-includes are ignored when reading them.

Both async methods accept a `CancellationToken`.

## Other options

* `AllowIdentityMatch()`: lets `On()` use database-generated columns. Rarely what you want, see [Matching rows](matching.md#generated-columns).
* `WithFallbackExpressionCompiler()`: see [Update expressions](update-expressions.md#unsupported-expressions).
