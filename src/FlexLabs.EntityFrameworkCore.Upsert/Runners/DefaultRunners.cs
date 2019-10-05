namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Provides the default list of commannd runners
    /// </summary>
    internal static class DefaultRunners
    {
        static IUpsertCommandRunner[] Runners;

        /// <summary>
        /// Returns the list of the default command runners
        /// </summary>
        public static IUpsertCommandRunner[] GetRunners()
        {
            if (Runners == null)
                Runners = new IUpsertCommandRunner[]
                {
                    new InMemoryUpsertCommandRunner(),
                    new MySqlUpsertCommandRunner(),
                    new PostgreSqlUpsertCommandRunner(),
                    new SqlServerUpsertCommandRunner(),
                    new SqliteUpsertCommandRunner(),
                };
            return Runners;
        }
    }
}
