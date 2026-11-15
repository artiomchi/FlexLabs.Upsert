# Owned types and JSON

## Owned types and complex properties

Owned entity types and complex properties that are mapped to columns in the same table are upserted along with the
owner, and are treated like ordinary columns. In `WhenMatched()` you can assign a whole owned object, or
individual properties of it:

```csharp
.WhenMatched((existing, incoming) => new Company
{
    Name = incoming.Name,
    Address = incoming.Address,
})
```

This works on every relational provider. The in-memory provider doesn't map owned types to columns.

## JSON columns

Owned types and complex properties mapped to a JSON column (`ToJson()`) are supported on the relational providers
that EF Core can map them for. The library serialises the value the same way EF Core does, so SQL Server and SQLite
(which expect already-serialised JSON) work too. You can include the property in an upsert, or assign the whole
object in `WhenMatched()`. You can't read or modify members inside the JSON document in an update expression:

```csharp
// works: replaces the whole document with the incoming one
.WhenMatched((existing, incoming) => new Company { Meta = incoming.Meta })

// throws UnsupportedExpressionException: reads a member inside the JSON
.WhenMatched((existing, incoming) => new Company
{
    Meta = new CompanyMeta { Required = incoming.Meta.Required },
})
```

Properties that are just a value stored in a JSON column, such as a `string` or `JsonObject`, are upserted like any
other column. Support for types like `JsonDocument` depends on the EF Core provider (it's tested with Npgsql only).
