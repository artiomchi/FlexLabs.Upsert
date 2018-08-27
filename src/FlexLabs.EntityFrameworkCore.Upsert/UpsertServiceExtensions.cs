using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class UpsertServiceExtensions
    {
        public static IServiceCollection AddUpsertCommandGenerator<TGenerator>(this IServiceCollection services)
            where TGenerator : class, IUpsertCommandRunner
        {
            services.AddScoped<IUpsertCommandRunner, TGenerator>();
            return services;
        }
    }
}
