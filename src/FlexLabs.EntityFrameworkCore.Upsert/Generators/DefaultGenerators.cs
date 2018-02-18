namespace FlexLabs.EntityFrameworkCore.Upsert.Generators
{
    internal static class DefaultGenerators
    {
        static IUpsertSqlGenerator[] _generators;
        public static IUpsertSqlGenerator[] Generators
            => _generators ?? (_generators = new IUpsertSqlGenerator[] { new PostgreSQLUpsertSqlGenerator(), new SqlServerUpsertSqlGenerator() });
    }
}
