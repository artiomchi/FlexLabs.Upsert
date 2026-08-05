using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

internal sealed class UpsertContextOptionsExtension<TRunner> : IDbContextOptionsExtension
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

    sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension)
            : base(extension)
        { }

        public override bool IsDatabaseProvider => false;
        public override string LogFragment => $"UpsertContextOptionsExtension (Runner: {typeof(TRunner).Name})";

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;
        public override int GetServiceProviderHashCode() => typeof(TRunner).GetHashCode();

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) { }
    }
}
