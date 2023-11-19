using System;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public static class BuildEnvironment
    {
        public static bool IsAppVeyor => Environment.GetEnvironmentVariable("APPVEYOR") != null;
        public static bool IsGitHub => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null;
    }
}
