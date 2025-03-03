using System;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Xunit;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public abstract class ContainerisedDatabaseInitializerFixture<TBuilder, TContainer>(DbContainerFixture<TBuilder, TContainer> dbContainerFixture)
        : DatabaseInitializerFixture, IClassFixture<DbContainerFixture<TBuilder, TContainer>>
        where TBuilder : IContainerBuilder<TBuilder, TContainer>, new()
        where TContainer : IContainer, IDatabaseContainer
    {
        private static readonly string DbDriverName = typeof(TContainer).Name.Replace("Container", "");

        private readonly string _connectionString = Environment.GetEnvironmentVariable($"FLEXLABS_UPSERT_TESTS_{DbDriverName.ToUpperInvariant()}_CONNECTION_STRING");

        protected static TBuilder ConfigureContainer(TBuilder builder, DbProviderFactory dbProviderFactory)
            => builder
                .WithName($"flexlabs_upsert_{DbDriverName.ToLowerInvariant()}")
                .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new UntilDatabaseIsAvailable(dbProviderFactory)))
                .WithReuse(true);

        protected string ConnectionString
            => _connectionString ?? dbContainerFixture.Container.GetConnectionString();

        public override async Task InitializeAsync()
        {
            if (_connectionString == null)
            {
                await ((IAsyncLifetime)dbContainerFixture).InitializeAsync();
            }

            await base.InitializeAsync();
        }

        public override async Task DisposeAsync()
        {
            if (_connectionString == null)
            {
                await ((IAsyncLifetime)dbContainerFixture).DisposeAsync();
            }

            await base.DisposeAsync();
        }

        // This custom wait strategy can be removed once Testcontainers 4.4.0 is released, thanks to https://github.com/testcontainers/testcontainers-dotnet/pull/1384
        private class UntilDatabaseIsAvailable(DbProviderFactory dbProviderFactory) : IWaitUntil
        {
            public async Task<bool> UntilAsync(IContainer container)
            {
                var stopwatch = Stopwatch.StartNew();
                var connectionString = ((IDatabaseContainer)container).GetConnectionString();
                while (!await IsAvailableAsync(connectionString, stopwatch))
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                return true;
            }

            private async Task<bool> IsAvailableAsync(string connectionString, Stopwatch stopwatch)
            {
                await using var connection = dbProviderFactory.CreateConnection() ?? throw new InvalidOperationException($"{dbProviderFactory.GetType().FullName}.CreateConnection() returned null.");
                connection.ConnectionString = connectionString;
                try
                {
                    await connection.OpenAsync();
                    return true;
                }
                catch (Exception exception)
                {
                    var timeout = TimeSpan.FromMinutes(5);
                    if (stopwatch.Elapsed > timeout)
                    {
                        throw new TimeoutException($"The database was not available at \"{connectionString}\" after waiting for {timeout.TotalMinutes:F0} minutes.", exception);
                    }
                    return false;
                }
            }
        }
    }
}
