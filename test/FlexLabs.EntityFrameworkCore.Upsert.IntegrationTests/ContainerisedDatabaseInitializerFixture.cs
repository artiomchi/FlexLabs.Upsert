using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
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

        private readonly bool _useContainer = !BuildEnvironment.UseLocalService;

        protected static TBuilder ConfigureContainer(TBuilder builder)
            => builder.WithName($"flexlabs_upsert_{DbDriverName.ToLowerInvariant()}");

        protected string ConnectionString
            => _useContainer ? dbContainerFixture.Container.GetConnectionString() : LocalServiceConnectionString;

        protected virtual string LocalServiceConnectionString
            => throw new InvalidOperationException($"{DbDriverName} tests don't support the USE_LOCAL_SERVICE environment variable.");

        public override async Task InitializeAsync()
        {
            if (_useContainer)
            {
                await ((IAsyncLifetime)dbContainerFixture).InitializeAsync();
            }

            await base.InitializeAsync();
        }

        public override async Task DisposeAsync()
        {
            if (_useContainer)
            {
                await ((IAsyncLifetime)dbContainerFixture).DisposeAsync();
            }

            await base.DisposeAsync();
        }
    }
}
