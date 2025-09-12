using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.Xunit;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public abstract class ContainerisedDatabaseInitializerFixture<TBuilder, TContainer>(DbContainerFixture<TBuilder, TContainer> dbContainerFixture) : DatabaseInitializerFixture
        where TBuilder : IContainerBuilder<TBuilder, TContainer>, new()
        where TContainer : IContainer, IDatabaseContainer
    {
        private static readonly string DbDriverName = typeof(TContainer).Name.Replace("Container", "");

        private readonly string _connectionString = Environment.GetEnvironmentVariable($"FLEXLABS_UPSERT_TESTS_{DbDriverName.ToUpperInvariant()}_CONNECTION_STRING");

        protected static TBuilder ConfigureContainer(TBuilder builder)
            => builder
                .WithName($"flexlabs_upsert_{DbDriverName.ToLowerInvariant()}_{Environment.Version}")
                .WithReuse(true);

        protected string ConnectionString
            => _connectionString ?? dbContainerFixture.Container.GetConnectionString();

        public override async ValueTask InitializeAsync()
        {
            if (_connectionString == null)
            {
                await ((IAsyncLifetime)dbContainerFixture).InitializeAsync();
            }

            await base.InitializeAsync();
        }

        public override async ValueTask DisposeAsync()
        {
            if (_connectionString == null)
            {
                await ((IAsyncLifetime)dbContainerFixture).DisposeAsync();
            }

            await base.DisposeAsync();
        }
    }
}
