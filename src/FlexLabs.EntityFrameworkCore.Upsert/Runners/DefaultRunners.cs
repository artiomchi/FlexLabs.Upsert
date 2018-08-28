namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    internal static class DefaultRunners
    {
        static IUpsertCommandRunner[] _generators;
        public static IUpsertCommandRunner[] Generators
            => _generators ?? (_generators = new IUpsertCommandRunner[]
            {
                new InMemoryUpsertCommandRunner(),
                new MySqlUpsertCommandRunner(),
                new PostgreSqlUpsertCommandRunner(),
                new SqlServerUpsertCommandRunner(),
            });
    }
}
