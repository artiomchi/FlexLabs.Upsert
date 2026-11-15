# Default values

If a property has a default configured (`HasDefaultValue()` or `HasDefaultValueSql()`), the default is used
whenever the entity's value is `null` or equal to the property's sentinel. By default the sentinel is the CLR
default of the type (`0` for `int`, `false` for `bool`, `default(DateTime)`, and so on). This matches how EF Core
itself decides that a value is "not set". It applies both when a row is inserted and when an existing row is updated.

If `HasDefaultValue()` is called without a value (the database has a default, but the model doesn't know what it
is), there is nothing to substitute, so the entity's value is always used as it is.

## Storing the CLR default

Upserting `0` into an `int` column with `HasDefaultValue(27)` stores `27`. If you need to store `0`, change the
sentinel to a value you won't use:

```csharp
modelBuilder.Entity<MyEntity>()
    .Property(e => e.MyProperty)
    .HasDefaultValue(27)
    .HasSentinel(-1); // only -1 means "not set", so 0 is stored as 0
```

## Limitations

**In-memory provider.** It doesn't evaluate `HasDefaultValueSql()` expressions, so a sentinel value isn't replaced
by the result of the SQL expression. `HasDefaultValue()` defaults are applied.

**Oracle.** `HasDefaultValueSql()` expressions are placed in the `SELECT ... FROM dual` queries that make up the
`MERGE` source, and these are joined with `UNION ALL` when more than one entity is upserted. Oracle doesn't allow
sequence pseudocolumns (`NEXTVAL`, `CURRVAL`) in queries combined this way, so a sequence-based default such as
`HasDefaultValueSql("my_seq.NEXTVAL")` isn't supported when upserting several entities at once. Constants and
ordinary functions (`SYSTIMESTAMP`, for example) work fine.

**MySQL.** Default values on text columns aren't supported.
