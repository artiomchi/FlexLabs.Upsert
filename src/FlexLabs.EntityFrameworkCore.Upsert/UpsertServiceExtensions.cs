using System;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to replace the currently registered default runner
    /// </summary>
    public static class UpsertServiceExtensions
    {
        /// <summary>
        /// Register a custom upsert command runner, to replace the built-in ones
        /// This method can only be used when EF is building and managing its internal service
        /// provider. If the service provider is being built externally and passed to Microsoft.EntityFrameworkCore.DbContextOptionsBuilder.UseInternalServiceProvider(System.IServiceProvider),
        /// then replacement services should be configured on that service provider before
        /// it is passed to EF.
        /// The replacement service gets the same scope as the EF service that it is replacing.
        /// </summary>
        /// <typeparam name="TRunner">Type of the upsert command runner class</typeparam>
        /// <param name="builder">The DbContextOptionsBuilder that is used to configure  the DbContext</param>
        /// <returns>The DbContextOptionsBuilder passed to this call</returns>
        public static DbContextOptionsBuilder ReplaceUpsertCommandRunner<TRunner>(this DbContextOptionsBuilder builder)
            where TRunner : class, IUpsertCommandRunner
        {
            ArgumentNullException.ThrowIfNull(builder);

            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(new UpsertContextOptionsExtension<TRunner>());
            return builder;
        }
    }
}
