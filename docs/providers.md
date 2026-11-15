# Database providers

The library picks a runner based on the EF Core provider your `DbContext` uses. Nothing needs to be registered.

| Database | Provider package | Statement | `RunAndReturn` |
|---|---|---|---|
| PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` | `INSERT … ON CONFLICT DO UPDATE` | Yes |
| SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` | `MERGE` | Yes |
| SQLite | `Microsoft.EntityFrameworkCore.Sqlite` | `INSERT … ON CONFLICT DO UPDATE` | Yes |
| MySQL / MariaDB | `Pomelo.EntityFrameworkCore.MySql`, `Microting.EntityFrameworkCore.MySql`, `MySql.EntityFrameworkCore` | `INSERT … ON DUPLICATE KEY UPDATE` | No |
| Oracle | `Oracle.EntityFrameworkCore` | `MERGE` | No |
| In-memory | `Microsoft.EntityFrameworkCore.InMemory` | Emulated | Yes |

Any MySQL provider whose name contains `MySql` is picked up by the MySQL runner. For anything else, see
[custom runners](#custom-runners).

## PostgreSQL

Columns declared as `GENERATED ALWAYS AS IDENTITY` are left out of the insert, since PostgreSQL refuses explicit
values for them.

## SQL Server

Uses `MERGE` with a `HOLDLOCK` hint, to avoid the race conditions that `MERGE` is known for.

SQL Server is the only database where the match columns can include a nullable column.

## SQLite

Needs SQLite 3.24 (June 2018) or later. The SQLite that ships with current `Microsoft.Data.Sqlite` packages is
well past that. If you swap in a system SQLite library (through `SQLitePCLRaw.provider.sqlite3` or similar) and
see syntax errors near `ON CONFLICT`, check its version.

## MySQL and MariaDB

* **`On()` is ignored.** MySQL matches on whatever unique key conflicts. Make sure the unique index you want is
  on the table.
* **`NoUpdate()`** is generated as `INSERT IGNORE`, which ignores other errors too, not just duplicate keys.
* **`UpdateIf()`** is supported, using user variables to work around the way MySQL evaluates `ON DUPLICATE KEY UPDATE`
  assignments one at a time. Very old versions of this library threw an exception here.
* **`RunAndReturn()`** isn't supported.
* Default values on text columns aren't supported by MySQL itself.

## Oracle

* **`RunAndReturn()`** isn't supported.
* Sequence-based `HasDefaultValueSql()` defaults don't work when upserting several rows at once, see
  [Default values](default-values.md#limitations).

## In-memory

Meant for tests. The upsert is emulated: matching rows are looked up in the context, updated or added, and saved.
It uses the same update expressions as the relational providers. Differences:

* `HasDefaultValueSql()` isn't evaluated.
* Owned types and complex properties mapped to columns aren't supported.

## Custom runners

A runner is a class implementing `IUpsertCommandRunner`. For SQL databases, derive from `RelationalUpsertCommandRunner`
and implement `Supports()` and `GenerateCommand()`; the existing runners in the repository are good examples.

To use your runner, register it when configuring the context. It takes priority over the built-in ones:

```csharp
services.AddDbContext<MyContext>(options => options
    .UseMyDatabase(connectionString)
    .ReplaceUpsertCommandRunner<MyUpsertCommandRunner>());
```

This also lets you replace a built-in runner, for example to tweak the SQL for your setup. If you build your own
internal service provider for EF Core, register the runner on it instead.
