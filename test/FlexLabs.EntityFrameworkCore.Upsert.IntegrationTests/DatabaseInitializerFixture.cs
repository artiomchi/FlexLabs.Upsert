using System.Threading.Tasks;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public abstract class DatabaseInitializerFixture : IAsyncLifetime
    {
        public DbContextOptions<TestDbContext> DataContextOptions { get; private set; }

        public abstract DbDriver DbDriver { get; }

        protected abstract void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder);

        public virtual async Task InitializeAsync()
        {
            var builder = new DbContextOptionsBuilder<TestDbContext>();
            builder.EnableSensitiveDataLogging();
            ConfigureContextOptions(builder);
            DataContextOptions = builder.Options;

            using var context = new TestDbContext(DataContextOptions);
            await context.Database.EnsureCreatedAsync();
        }

        public virtual Task DisposeAsync() => Task.CompletedTask;
    }
}
