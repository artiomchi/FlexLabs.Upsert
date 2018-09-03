namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Provides the default list of commannd runners
    /// </summary>
    internal static class DefaultRunners
    {
        static IUpsertCommandRunner[] _runners;

        /// <summary>
        /// Returns the list of the default command runners
        /// </summary>
        public static IUpsertCommandRunner[] Runners
            => _runners ?? (_runners = new IUpsertCommandRunner[]
            {
                new InMemoryUpsertCommandRunner(),
                new MySqlUpsertCommandRunner(),
                new PostgreSqlUpsertCommandRunner(),
                new SqlServerUpsertCommandRunner(),
                new SqliteUpsertCommandRunner(),
            });
    }
}
