using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public abstract class ContainerisedDatabaseInitializerFixture<TContainer> : DatabaseInitializerFixture
        where TContainer : IContainer, IDatabaseContainer
    {
        public TContainer TestContainer { get; }

        public ContainerisedDatabaseInitializerFixture()
        {
            if (!BuildEnvironment.UseLocalService)
            {
                TestContainer = BuildContainer();
            }
        }

        protected abstract TContainer BuildContainer();

        public override async Task InitializeAsync()
        {
            if (TestContainer is not null)
            {
                await TestContainer.StartAsync();
            }

            await base.InitializeAsync();
        }

        // Some containers don't start up properly if they're stopped and started again, so we will leave them running
        // In CI environments, they will be cleared up automatically, when developing locally - you may need to clean up manually
        //public override async Task DisposeAsync()
        //{
        //    if (TestContainer is not null)
        //    {
        //        await TestContainer.StopAsync();
        //    }
        //}
    }
}
