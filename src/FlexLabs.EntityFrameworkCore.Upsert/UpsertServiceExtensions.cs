using System;
using FlexLabs.EntityFrameworkCore.Upsert.Generators;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class UpsertServiceExtensions
    {
        [Obsolete("Marking as obsolete to replace with a new, working one in v2")]
        public static IServiceCollection AddUpsertCommandGenerator<TGenerator>(this IServiceCollection services)
            where TGenerator : class, IUpsertSqlGenerator
        {
            services.AddScoped<IUpsertSqlGenerator, TGenerator>();
            return services;
        }
    }
}
