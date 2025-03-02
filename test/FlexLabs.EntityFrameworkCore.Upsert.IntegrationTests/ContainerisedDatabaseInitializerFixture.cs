﻿using System;
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

        private readonly string _connectionString = Environment.GetEnvironmentVariable($"FLEXLABS_UPSERT_TESTS_{DbDriverName.ToUpperInvariant()}_CONNECTION_STRING");

        protected static TBuilder ConfigureContainer(TBuilder builder)
            => builder.WithName($"flexlabs_upsert_{DbDriverName.ToLowerInvariant()}");

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
    }
}
