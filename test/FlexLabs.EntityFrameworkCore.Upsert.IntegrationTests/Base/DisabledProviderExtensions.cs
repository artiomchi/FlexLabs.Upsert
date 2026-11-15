using Microsoft.EntityFrameworkCore.Infrastructure;

// Stand-ins for the provider-specific IsXxx() extension methods, used when the provider package
// is not referenced (the matching NOxxx constant is defined). They always return false, so
// shared test code like TestDbContext compiles without the provider package.
namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

internal static class DisabledProviderExtensions
{
    extension(DatabaseFacade database)
    {
#if NOMSSQL
        public bool IsSqlServer() => false;
#endif

#if NOMYSQL
        public bool IsMySql() => false;
#endif

#if NOORACLE
        public bool IsOracle() => false;
#endif

#if NOPOSTGRES
        public bool IsNpgsql() => false;
#endif
    }
}
