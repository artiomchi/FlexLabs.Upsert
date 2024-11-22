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

        public override async Task DisposeAsync()
        {
            if (TestContainer is not null)
            {
                await TestContainer.StopAsync();
            }
        }
    }
}
