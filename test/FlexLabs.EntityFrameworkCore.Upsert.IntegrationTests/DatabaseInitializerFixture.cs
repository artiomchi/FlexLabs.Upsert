using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public abstract class DatabaseInitializerFixture : IAsyncLifetime
    {
        private DbContextOptions<TestDbContext> _dataContextOptions;
        private ExceptionDispatchInfo _exception;

        public DbContextOptions<TestDbContext> DataContextOptions
        {
            get
            {
                _exception?.Throw();
                return _dataContextOptions;
            }
        }

        public abstract DbDriver DbDriver { get; }

        protected abstract void ConfigureContextOptions(DbContextOptionsBuilder<TestDbContext> builder);

        public virtual async Task InitializeAsync()
        {
            var builder = new DbContextOptionsBuilder<TestDbContext>();
            builder.EnableSensitiveDataLogging();
            try
            {
                ConfigureContextOptions(builder);
            }
            catch (Exception exception)
            {
                _exception = ExceptionDispatchInfo.Capture(exception);
                return;
            }
            _dataContextOptions = builder.Options;

            await using var context = new TestDbContext(DataContextOptions);
            await context.Database.EnsureCreatedAsync();
        }

        public virtual Task DisposeAsync() => Task.CompletedTask;
    }
}
