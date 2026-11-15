# Tests

| Project | What it covers | Needs a database? |
|---|---|---|
| `FlexLabs.EntityFrameworkCore.Upsert.Tests` | Unit tests: generated SQL per provider, expression analysis | No |
| `FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests` | The same upsert scenarios run against real databases | Yes (containers), except InMemory and Sqlite |

Tests use xUnit v3 on Microsoft.Testing.Platform, with AwesomeAssertions for assertions.

## Running

```sh
dotnet test                                                                        # everything
dotnet test --project test/FlexLabs.EntityFrameworkCore.Upsert.Tests               # unit tests only
dotnet test --project test/FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests --filter-class "*Sqlite*"
dotnet test --project test/FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests --filter-class "*Postgres*" --filter-method "*PageVisit*"
```

Provider classes are `DbTests_InMemory`, `DbTests_Sqlite`, `DbTests_Postgres`, `DbTests_SqlServer`,
`DbTests_MySql` and `DbTests_Oracle`. Every one of them inherits all scenarios from `DbTestsBase`.

**Containers.** Postgres, SQL Server, MySQL and Oracle start through Testcontainers and are reused between runs
(`WithReuse(true)`). Docker and Podman both work; with Podman make sure `DOCKER_HOST` points at its socket
(for example `unix://$XDG_RUNTIME_DIR/podman/podman.sock`). To use an existing server instead, set
`FLEXLABS_UPSERT_TESTS_<DRIVER>_CONNECTION_STRING` (`POSTGRESQL`, `MSSQL`, `MYSQL`, `ORACLE`).
Don't run two test processes against the same reused container at once: they reset the same tables.

`dotnet build /p:POSTGRES_ONLY=true` compiles out the SQL Server, MySQL and Oracle providers.

## Unit test SQL snapshots

The `Runners/*UpsertCommandRunnerTests` check the SQL each provider generates. The scenarios live once in
`RelationalCommandRunnerTestsBase`; the expected SQL for each provider is a file in `Runners/Snapshots/`, named
`<Engine>.<Scenario>.sql` (for example `SqlServer.Update_Condition_AndCondition.sql`). Snapshots are stored one
clause per line (`MERGE INTO`, `USING`, `ON`, `WHEN MATCHED`, ...) so changes read as a normal diff. The statement
itself is still a single line when it runs; `SqlSnapshot` only splits it for comparison.

After an intentional change to the generated SQL, run

```sh
UPDATE_SNAPSHOTS=1 dotnet test --project test/FlexLabs.EntityFrameworkCore.Upsert.Tests
```

and review `git diff test/FlexLabs.EntityFrameworkCore.Upsert.Tests/Runners/Snapshots`. When `CI=true` (GitHub Actions)
the switch is ignored, so CI fails on a missing or different snapshot instead of creating one.

## Integration test layout

```
IntegrationTests/
  DbTestsBase.cs            fixture plumbing, ResetDb, Arrange(), NewContext(), Require()
  DbTestsBase.Seed.cs       rows seeded before every test + small sample-entity builders
  DbTestsBase.Owned.cs      owned entities, owned JSON, complex properties, complex JSON
  Scenarios/                one partial class file per feature (Country, PageVisit, Json, Keys, ...)
  Base/Tables.cs            entity types          Base/TestDbContext.cs   the model
  Base/ProviderCapabilities.cs   which provider supports which feature
  DbTests_<Provider>.cs     container/connection setup, plus tests that only make sense on one provider
```

### Writing a test

```csharp
[Fact]
public void Upsert_Country_Update_On()
{
    using var dbContext = Arrange();            // resets the DB to the seed rows; Arrange(extraRow) adds more
    var incoming = NewCountry("Australia", "AU");

    dbContext.Countries.Upsert(incoming).On(c => c.ISO).Run();

    dbContext.Countries.OrderBy(c => c.ID).Should().SatisfyRespectively(
        country => country.Should().MatchModel(incoming));
}
```

- Tests that use a feature not every database has start with `Require(Capability.ReturningRecords);` (or another
  `Capability`) and are skipped elsewhere.
- Name tests `Upsert_<Entity>_<Scenario>` and put them in the `Scenarios/` file for that feature.
- `Arrange()` always starts from the seed rows in `DbTestsBase.Seed.cs` (`_dbCountry`, `_dbVisit`, ...).
  Express expectations relative to them (`ExistingVisits + 1`) rather than repeating numbers.
- Prefer one `BeEquivalentTo` against an explicit expected object over many property asserts.
  `MatchModel` (in `Base/ModelExtensions.cs`) is the older helper for Country, PageVisit, Status and TestEntity.
- The expression passed to `On`/`WhenMatched`/`UpdateIf` is usually what's being tested, so write it out in the
  test. `PageVisit.cs` shows the one case where lambdas live in a table (`VisitUpdates`), because expression trees
  can't be xUnit theory data.

### Provider differences

Never write `if (driver == ...)` or `Assert.SkipWhen(...)` in a test. Add a `Capability` and list the supporting
drivers in `Base/ProviderCapabilities.cs`, then call `Require(Capability.X)`. That file is the single place
that says which database can do what, and the reason text there is what the test report shows for skipped tests.
Tests that only apply to one provider go in that provider's `DbTests_<Provider>.cs`.

### Checking a refactor didn't lose coverage

Compare totals and skipped tests per provider before and after. `dotnet test ... --list-tests` lists test
names; the run summary gives total / succeeded / skipped. A `[Theory]` row counts as one test, so merging N tests
into a theory with N rows should leave the totals unchanged.
