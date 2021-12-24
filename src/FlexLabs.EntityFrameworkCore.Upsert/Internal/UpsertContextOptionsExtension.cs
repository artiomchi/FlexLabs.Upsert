using System;
using System.Collections.Generic;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    internal class UpsertContextOptionsExtension<TRunner> : IDbContextOptionsExtension
        where TRunner : class, IUpsertCommandRunner
    {
        public UpsertContextOptionsExtension()
        {
            Info = new ExtensionInfo(this);
        }

        public DbContextOptionsExtensionInfo Info { get; }

        public void ApplyServices(IServiceCollection services)
        {
            services.AddScoped<IUpsertCommandRunner, TRunner>();
        }

        public void Validate(IDbContextOptions options) { }

        class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            { }

            public override bool IsDatabaseProvider => false;
            public override string LogFragment => "UpsertContextOptionsExtension";
            
#if NET6_0_OR_GREATER
            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            {
                return  string.Equals(LogFragment, other.LogFragment, StringComparison.InvariantCulture);
            }
            public override int GetServiceProviderHashCode() => 0;
            
#else
            public override long GetServiceProviderHashCode() => 0;
#endif
            
            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) { }
        }
    }
}
