using System;
using System.Collections.Generic;
using System.Data.Common;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    public abstract class RelationalCommandRunnerTestsBase<TRunner>
        where TRunner : RelationalUpsertCommandRunner
    {
        protected readonly DbContext _dbContext;
        protected readonly IRawSqlCommandBuilder _rawSqlBuilder;
        protected Model _model;

        public RelationalCommandRunnerTestsBase(string providerName)
        {
            _model = new Model();
            AddEntity<TestEntity>(_model);
            AddEntity<TestEntityWithNullableKey>(_model);
            // ReSharper disable once VirtualMemberCallInConstructor
            InitializeModel(_model);

            var dbProvider = Substitute.For<IDatabaseProvider>();
            dbProvider.Name.Returns(providerName);

            var services = new ServiceCollection();
            new EntityFrameworkRelationalServicesBuilder(services).TryAddCoreServices();
            services.AddSingleton(Substitute.For<ITypeMappingSource>());
            services.AddSingleton<IUpsertCommandRunner, TRunner>();
            services.AddSingleton<IModel>(_model);
            services.AddSingleton(dbProvider);
            services.AddSingleton(Substitute.For<IRelationalTypeMappingSource>());
            services.AddSingleton<ITypeMappingSource>(_ => _.GetRequiredService<IRelationalTypeMappingSource>());
            var serviceProvider = services.BuildServiceProvider();

            // initialize relational model:
            serviceProvider.GetRequiredService<IModelRuntimeInitializer>().Initialize(_model);

            _dbContext = Substitute.For<DbContext, IInfrastructure<IServiceProvider>>();
            ((IInfrastructure<IServiceProvider>)_dbContext).Instance.Returns(serviceProvider);

            var relationalConnection = Substitute.For<IRelationalConnection>();
            relationalConnection.DbConnection.Returns(Substitute.For<DbConnection>());

            _rawSqlBuilder = Substitute.For<IRawSqlCommandBuilder>();
            _rawSqlBuilder.Build(default, default, default).ReturnsForAnyArgs(
                new RawSqlCommand(Substitute.For<IRelationalCommand>(), new Dictionary<string, object>()));

            var concurrencyDetector = Substitute.For<IConcurrencyDetector>();
            var disposer = new ConcurrencyDetectorCriticalSectionDisposer(concurrencyDetector);
            concurrencyDetector.EnterCriticalSection().Returns(disposer);

            var dependencies = Substitute.For<IRelationalDatabaseFacadeDependencies>();
            dependencies.RelationalConnection.Returns(relationalConnection);
            dependencies.RawSqlCommandBuilder.Returns(_rawSqlBuilder);
            dependencies.ConcurrencyDetector.Returns(concurrencyDetector);

            var dbFacade = Substitute.For<DatabaseFacade, IDatabaseFacadeDependenciesAccessor>(_dbContext);
            ((IDatabaseFacadeDependenciesAccessor)dbFacade).Dependencies.Returns(dependencies);

            _dbContext.Database.Returns(dbFacade);
        }

        protected virtual void InitializeModel(Model model)
        {
        }

        protected static EntityType AddEntity<TEntity>(Model model)
        {
            var clrType = typeof(TEntity);
#if NET6_0_OR_GREATER
            var entityType = model.AddEntityType(clrType, true, ConfigurationSource.Convention);
#else
            var entityType = model.AddEntityType(clrType, ConfigurationSource.Convention);
#endif
            foreach (var property in clrType.GetProperties())
            {
                entityType.AddProperty(property.Name, ConfigurationSource.Explicit);
            }
            var idProperty = entityType.FindProperty("ID")
                ?? throw new InvalidOperationException("ID property missing on entity " + typeof(TEntity).Name);
            entityType.AddKey(idProperty, ConfigurationSource.Convention);
            return entityType;
        }

        protected abstract string NoUpdate_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_NoUpdate()
        {
            _dbContext.Upsert(new TestEntity())
                .NoUpdate()
                .Run();

            _rawSqlBuilder.Received().Build(
                NoUpdate_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string NoUpdate_Multiple_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_NoUpdate_Multiple()
        {
            _dbContext.UpsertRange(new TestEntity(), new TestEntity())
                .NoUpdate()
                .Run();

            _rawSqlBuilder.Received().Build(
                NoUpdate_Multiple_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string NoUpdate_WithNullable_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_NoUpdate_WithNullable()
        {
            _dbContext.Upsert(new TestEntityWithNullableKey())
                .On(e => new { e.ID1, e.ID2 })
                .NoUpdate()
                .Run();

            _rawSqlBuilder.Received().Build(
                NoUpdate_WithNullable_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_Constant_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Constant()
        {
            _dbContext.Upsert(new TestEntity())
                .WhenMatched(e => new TestEntity
                {
                    Name = "value"
                })
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_Constant_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_Constant_Multiple_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Constant_Multiple()
        {
            _dbContext.UpsertRange(new TestEntity(), new TestEntity())
                .WhenMatched(e => new TestEntity
                {
                    Name = "value"
                })
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_Constant_Multiple_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_Source_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Source()
        {
            _dbContext.Upsert(new TestEntity())
                .WhenMatched((ed, en) => new TestEntity
                {
                    Name = en.Name
                })
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_Source_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_BinaryAdd_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_BinaryAdd()
        {
            _dbContext.Upsert(new TestEntity())
                .WhenMatched(e => new TestEntity
                {
                    Total = e.Total + 5
                })
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_BinaryAdd_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_Coalesce_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Coalesce()
        {
            _dbContext.Upsert(new TestEntity())
                .WhenMatched(e => new TestEntity
                {
                    Status = e.Status ?? "suffix"
                })
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_Coalesce_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_BinaryAddMultiply_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_BinaryAddMultiply()
        {
            _dbContext.Upsert(new TestEntity())
                .WhenMatched((ed, en) => new TestEntity
                {
                    Total = (ed.Total + 5) * en.Total
                })
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_BinaryAddMultiply_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_BinaryAddMultiplyGroup_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_BinaryAddMultiplyGroup()
        {
            _dbContext.Upsert(new TestEntity())
                .WhenMatched((ed, en) => new TestEntity
                {
                    Total = ed.Total + 3 * en.Total
                })
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_BinaryAddMultiplyGroup_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_Condition_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Condition()
        {
            _dbContext.Upsert(new TestEntity())
                .WhenMatched(e => new TestEntity
                {
                    Name = "new"
                })
                .UpdateIf(e => e.Total > 5)
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_Condition_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_Condition_UpdateConditionColumn_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Condition_UpdateConditionColumn()
        {
            _dbContext.Upsert(new TestEntity())
                .WhenMatched(e => new TestEntity
                {
                    Name = "new",
                    Total = e.Total + 1
                })
                .UpdateIf(e => e.Total > 5)
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_Condition_UpdateConditionColumn_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_Condition_AndCondition_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Condition_AndCondition()
        {
            _dbContext.Upsert(new TestEntity())
                .WhenMatched(e => new TestEntity
                {
                    Name = "new"
                })
                .UpdateIf((ed, en) => ed.Total > 5 && ed.Status != en.Status)
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_Condition_AndCondition_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_Condition_NullCheck_AlsoNullValue_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Condition_NullCheck_AlsoNullValue()
        {
            var ent = new TestEntity
            {
                Name = null,
            };

            _dbContext.Upsert(new TestEntity())
                .WhenMatched(e => new TestEntity
                {
                    Name = ent.Name
                })
                .UpdateIf(e => e.Status != null)
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_Condition_NullCheck_AlsoNullValue_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }

        protected abstract string Update_WatchWithNullCheck_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_WatchWithNullCheck()
        {
            _dbContext.Upsert(new TestEntity())
                .WhenMatched((e, en) => new TestEntity
                {
                    Name = en.Name == null ? "new" : en.Name
                })
                .Run();

            _rawSqlBuilder.Received().Build(
                Update_WatchWithNullCheck_Sql,
                Arg.Any<IEnumerable<object>>(),
                Arg.Any<IModel>());
        }
    }
}
