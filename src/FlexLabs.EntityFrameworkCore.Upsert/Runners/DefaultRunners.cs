namespace FlexLabs.EntityFrameworkCore.Upsert.Runners;

/// <summary>
/// Provides the default list of command runners
/// </summary>
internal static class DefaultRunners
{
    private static readonly Lazy<IUpsertCommandRunner[]> Runners = new(() => [
        new InMemoryUpsertCommandRunner(),
        new MySqlUpsertCommandRunner(),
        new PostgreSqlUpsertCommandRunner(),
        new SqlServerUpsertCommandRunner(),
        new SqliteUpsertCommandRunner(),
        new OracleUpsertCommandRunner()
    ]);

    /// <summary>
    /// Returns the list of the default command runners
    /// </summary>
    public static IUpsertCommandRunner[] GetRunners() => Runners.Value;
}
