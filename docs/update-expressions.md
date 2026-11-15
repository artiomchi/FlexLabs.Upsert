# Update expressions

`WhenMatched()` and `UpdateIf()` take expressions that the library translates to SQL. They look like ordinary C#,
but they don't run in .NET, so only a subset of the language is supported.

## WhenMatched

`WhenMatched()` takes an expression that creates a new instance of your entity. Only the properties you
initialise are updated:

```csharp
.WhenMatched(v => new DailyVisit { Visits = v.Visits + 1 })
```

It has two forms. The first parameter is the row that's currently in the database. The optional second one is the
row you tried to insert:

```csharp
.WhenMatched((existing, incoming) => new Country { Name = incoming.Name })
```

Using `incoming.Name` instead of copying the value in yourself has two advantages: the parameter that was already
sent for the insert is reused, and it's the only way to give each row its own value when you upsert several rows
in one call.

## What's supported

Inside the expression, you can use:

* Property values from `existing` and `incoming`
* Arithmetic: `+`, `-`, `*`, `/`, `%`
* Comparisons and logic, in conditions and conditional expressions: `==`, `!=`, `<`, `<=`, `>`, `>=`, `&&`, `||`
* Null coalescing: `existing.Name ?? incoming.Name`
* Conditional expressions: `existing.Visits > 10 ? 10 : existing.Visits + 1`
* Constants and variables from your code
* Method calls, properties and static members that **don't involve database columns**, such as `DateTime.UtcNow`
  or `name.Trim()`. These are evaluated in .NET and sent as parameters.

So `existing.Updated = DateTime.UtcNow` works, while `Name = existing.Name.Trim()` doesn't: it would need the
database to run `Trim()` on the column.

## UpdateIf

`UpdateIf()` takes a condition in the same language. It becomes the `WHERE` part of the update (or the
`WHEN MATCHED AND` part for SQL Server and Oracle):

```csharp
.UpdateIf((existing, incoming) => incoming.Version > existing.Version)
```

The expression has to be a comparison. A bare `bool` property isn't enough, write `x.IsActive == true`.

## Unsupported expressions

If the library finds something it can't translate, it throws an `UnsupportedExpressionException`, showing the part
of the expression it got stuck on. There are two ways forward:

1. Rewrite the expression to something simpler. Often that means working out a value before the call and
   passing it in as a variable.
2. If the unsupported part doesn't depend on any column, add `.WithFallbackExpressionCompiler()` to the command.
   It compiles and runs that part of the expression in .NET. This is slower, so it's off by default, but it's
   fine for anything that doesn't run in a hot path.

If you think something should be supported natively, please open an issue.
