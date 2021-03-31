using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;
using Xunit;
using Xunit.Abstractions;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    [Trait("Category", "in-memory")]
    [Collection("InMemory")]
    public class DbTests_InMemory : DbTestsBase, IClassFixture<DbTests_InMemory.DatabaseInitializer>
    {
        public sealed class DatabaseInitializer : DatabaseInitializerFixture
        {
            public DatabaseInitializer(IMessageSink diagnosticMessageSink)
                : base(diagnosticMessageSink, DbDriver.InMemory)
            { }
        }

        public DbTests_InMemory(DatabaseInitializer contexts)
            : base(contexts)
        { }

        [Fact(Skip = "Owned entities are not yet supported for InMemory.")]
        public override void Upsert_Owned_Entity()
        {
        }

        [Fact(Skip = "Owned entities are not yet supported for InMemory.")]
        public override void Upsert_Owned_Entity_NoUpdate()
        {
        }

        [Fact(Skip = "Owned entities are not yet supported for InMemory.")]
        public override void Upsert_Owned_Entity_WhenMatched()
        {
        }
    }
}
