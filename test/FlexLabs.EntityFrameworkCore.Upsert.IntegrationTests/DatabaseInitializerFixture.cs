using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public abstract class DatabaseInitializerFixture : IAsyncLifetime
    {
        public IContainer TestContainer { get; }
        public DbDriver DbDriver { get; }
        public DbContextOptions<TestDbContext> DataContextOptions { get; private set; }

        public DatabaseInitializerFixture(DbDriver dbDriver)
        {
            DbDriver = dbDriver;
        }

        public DatabaseInitializerFixture(DbDriver dbDriver, IContainer testContainer)
            : this(dbDriver)
        {
            TestContainer = testContainer;
        }

        protected abstract void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder);

        public async Task InitializeAsync()
        {
            if (TestContainer is not null)
            {
                await TestContainer.StartAsync();
            }

            var builder = new DbContextOptionsBuilder<TestDbContext>();
            ConfigureContextOptions(builder);
            DataContextOptions = builder.Options;

            using var context = new TestDbContext(DataContextOptions);
            await context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            if (TestContainer is not null)
            {
                await TestContainer.StopAsync();
            }
        }
    }
}
