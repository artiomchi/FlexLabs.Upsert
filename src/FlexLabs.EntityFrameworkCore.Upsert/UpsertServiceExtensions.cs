using FlexLabs.EntityFrameworkCore.Upsert.Generators;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class UpsertServiceExtensions
    {
        public static IServiceCollection AddUpsertCommandGenerator<TGenerator>(this IServiceCollection services)
            where TGenerator : class, IUpsertSqlGenerator
        {
            services.AddScoped<IUpsertSqlGenerator, TGenerator>();
            return services;
        }
    }
}
