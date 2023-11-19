using System;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public static class BuildEnvironment
    {
        public static bool IsGitHub => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null;
        public static bool UseLocalService => Environment.GetEnvironmentVariable("USE_LOCAL_SERVICE") != null;
    }
}
