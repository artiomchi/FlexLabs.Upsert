namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

/// <summary>
/// Database features that not every provider supports. Tests declare what they need with
/// <c>Require(Capability.X)</c>; <see cref="ProviderCapabilities"/> is the single place that says who has what.
/// </summary>
public enum Capability
{
    /// <summary>RunAndReturn / RunAndReturnAsync.</summary>
    ReturningRecords,
    /// <summary>Owned entities mapped to columns/tables (the in-memory provider has no SQL mapping).</summary>
    OwnedEntities,
    /// <summary>EF complex properties mapped to columns.</summary>
    ComplexProperties,
    /// <summary>Owned entities / complex properties mapped to a JSON column.</summary>
    OwnedJson,
    /// <summary>JSON column with a child object and default value handling.</summary>
    JsonChildObjectDefaults,
    /// <summary>Composite keys containing a nullable column.</summary>
    NullableKeys,
    /// <summary><c>HasDefaultValueSql</c>.</summary>
    SqlDefaults,
    /// <summary>Default values on text columns.</summary>
    TextColumnDefaults,
    /// <summary>Computed columns.</summary>
    ComputedColumns,
    /// <summary>Needs a hand-written schema, so it is only exercised with the in-memory provider.</summary>
    CustomSchemaUnspecifiedDefault,
    /// <summary>Needs a hand-written schema, so it is only exercised with the in-memory provider.</summary>
    CustomSchemaPropertyWithoutSetter,
    /// <summary>Needs a hand-written schema, so it is only exercised with the in-memory provider.</summary>
    CustomSchemaDefaultMatchColumn,
}

/// <summary>
/// The provider support matrix: one row per <see cref="Capability"/>, listing the drivers that support it.
/// </summary>
public static class ProviderCapabilities
{
    private const DbDriver P = DbDriver.Postgres;
    private const DbDriver S = DbDriver.MSSQL;
    private const DbDriver M = DbDriver.MySQL;
    private const DbDriver O = DbDriver.Oracle;
    private const DbDriver L = DbDriver.Sqlite;
    private const DbDriver I = DbDriver.InMemory;

    private static readonly Dictionary<Capability, (DbDriver[] Drivers, string SkipReason)> Matrix = new()
    {
        //                                         Supported on            Skip reason (shown in the test report)
        [Capability.ReturningRecords]                  = ([P, S, L, I],    "Returning records is not implemented in MySQL and Oracle"),
        [Capability.OwnedEntities]                     = ([P, S, M, O, L], "db doesn't support sql owned entities"),
        [Capability.ComplexProperties]                 = ([P, S, M, O, L], "db doesn't support sql complex properties"),
        [Capability.OwnedJson]                         = ([P],             "Owned json is only supported for postgres"),
        [Capability.JsonChildObjectDefaults]           = ([P],             "Default values on text columns are only supported in Postgres"),
        [Capability.NullableKeys]                      = ([S, I],          "Nullable keys are not supported in this database"),
        [Capability.SqlDefaults]                       = ([P, S, M, O, L], "In memory db doesn't support default sql values"),
        [Capability.TextColumnDefaults]                = ([P, S, O, L, I], "Default values on text columns not supported in MySQL"),
        [Capability.ComputedColumns]                   = ([P, S, M, O, L], "In memory db doesn't support sql computed columns"),
        [Capability.CustomSchemaUnspecifiedDefault]    = ([I],             "Relational providers can't create a schema for a column with an unspecified default value"),
        [Capability.CustomSchemaPropertyWithoutSetter] = ([I],             "Properties without a setter need a custom schema, so it's only tested with the in-memory provider"),
        [Capability.CustomSchemaDefaultMatchColumn]    = ([I],             "Match columns with default values need a custom schema, so it's only tested with the in-memory provider"),
    };

    public static bool Supports(DbDriver driver, Capability capability)
        => Matrix[capability].Drivers.Contains(driver);

    public static string SkipReason(Capability capability)
        => Matrix[capability].SkipReason;
}
