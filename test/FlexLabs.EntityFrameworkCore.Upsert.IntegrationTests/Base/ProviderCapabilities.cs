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
    /// <summary>A child object stored in a jsonb column (<c>JsonData.Child</c>).</summary>
    JsonbChildObject,
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
    private const DbDriver Post = DbDriver.Postgres;
    private const DbDriver MSSQ = DbDriver.MSSQL;
    private const DbDriver MySQ = DbDriver.MySQL;
    private const DbDriver Orac = DbDriver.Oracle;
    private const DbDriver Sqli = DbDriver.Sqlite;
    private const DbDriver InMe = DbDriver.InMemory;

#pragma warning disable format
    private static readonly Dictionary<Capability, (DbDriver[] Drivers, string SkipReason)> Matrix = new()
    {
        //                                             Supported on                         Skip reason (shown in the test report)
        [Capability.ReturningRecords]                  = ([Post, MSSQ, Sqli, InMe],         "Returning records is not implemented in MySQL and Oracle"),
        [Capability.OwnedEntities]                     = ([Post, MSSQ, MySQ, Orac, Sqli],   "db doesn't support sql owned entities"),
        [Capability.ComplexProperties]                 = ([Post, MSSQ, MySQ, Orac, Sqli],   "db doesn't support sql complex properties"),
        [Capability.OwnedJson]                         = ([Post],                           "Owned json is only supported for postgres"),
        [Capability.JsonbChildObject]                  = ([Post],                           "JsonData.Child is only mapped (as a jsonb column) for Postgres"),
        [Capability.NullableKeys]                      = ([MSSQ, InMe],                     "Nullable keys are not supported in this database"),
        [Capability.SqlDefaults]                       = ([Post, MSSQ, MySQ, Orac, Sqli],   "In memory db doesn't support default sql values"),
        [Capability.TextColumnDefaults]                = ([Post, MSSQ, Orac, Sqli, InMe],   "Default values on text columns not supported in MySQL"),
        [Capability.ComputedColumns]                   = ([Post, MSSQ, MySQ, Orac, Sqli],   "In memory db doesn't support sql computed columns"),
        [Capability.CustomSchemaUnspecifiedDefault]    = ([InMe],                           "Relational providers can't create a schema for a column with an unspecified default value"),
        [Capability.CustomSchemaPropertyWithoutSetter] = ([InMe],                           "Properties without a setter need a custom schema, so it's only tested with the in-memory provider"),
        [Capability.CustomSchemaDefaultMatchColumn]    = ([InMe],                           "Match columns with default values need a custom schema, so it's only tested with the in-memory provider"),
    };
#pragma warning restore format

    public static bool Supports(DbDriver driver, Capability capability)
        => Matrix[capability].Drivers.Contains(driver);

    public static string SkipReason(Capability capability)
        => Matrix[capability].SkipReason;
}
