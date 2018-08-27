namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    internal static class DefaultRunners
    {
        static IUpsertCommandRunner[] _generators;
        public static IUpsertCommandRunner[] Generators
            => _generators ?? (_generators = new IUpsertCommandRunner[]
            {
                new MySqlUpsertCommandRunner(),
                new PostgreSqlUpsertCommandRunner(),
                new SqlServerUpsertCommandRunner(),
            });
    }
}
