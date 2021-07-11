using System;
using System.Collections.Generic;
using System.Text;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public static class BuildEnvironment
    {
        public static bool IsAppVeyor => Environment.GetEnvironmentVariable("APPVEYOR") != null;
        public static bool IsGitHub => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null;
        public const bool IsGitHubLocalPostgres =
#if POSTGRES_ONLY
            true;
#else
            false;
#endif
    }
}
