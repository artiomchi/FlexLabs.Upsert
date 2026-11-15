using System.Data.Common;
using System.Runtime.CompilerServices;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners;

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
        _dbContext.Model.Returns(_model);
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

    /// <summary>
    /// Checks the SQL handed to the raw SQL builder against <c>Snapshots/&lt;Engine&gt;.&lt;Scenario&gt;.sql</c>.
    /// The scenario is the test method name without its "...SyntaxRunner_" prefix.
    /// </summary>
    protected void AssertSql([CallerMemberName] string testName = "")
    {
        var sql = _rawSqlBuilder.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IRawSqlCommandBuilder.Build))
            .Select(call => (string)call.GetArguments()[0])
            .Single();

        const string TestMarker = "SyntaxRunner_";
        var markerIndex = testName.IndexOf(TestMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
            throw new InvalidOperationException($"Test method '{testName}' must contain '{TestMarker}' so AssertSql can name its snapshot.");
        var scenario = testName[(markerIndex + TestMarker.Length)..];

        const string ClassSuffix = "UpsertCommandRunnerTests";
        var className = GetType().Name;
        if (!className.EndsWith(ClassSuffix, StringComparison.Ordinal) || className.Length == ClassSuffix.Length)
            throw new InvalidOperationException($"Test class '{className}' must be named '<Engine>{ClassSuffix}' so AssertSql can name its snapshots.");
        var engine = className[..^ClassSuffix.Length];

        SqlSnapshot.Match(sql, $"{engine}.{scenario}");
    }

    protected virtual void InitializeModel(Model model)
    {
    }

    protected static EntityType AddEntity<TEntity>(Model model)
    {
        var clrType = typeof(TEntity);
        var entityType = model.AddEntityType(clrType, true, ConfigurationSource.Convention);
        foreach (var property in clrType.GetProperties())
        {
            entityType.AddProperty(property.Name, ConfigurationSource.Explicit);
        }
        var idProperty = entityType.FindProperty("ID")
            ?? throw new InvalidOperationException("ID property missing on entity " + typeof(TEntity).Name);
        entityType.AddKey(idProperty, ConfigurationSource.Convention);
        return entityType;
    }

    [Fact]
    public void SqlSyntaxRunner_NoUpdate()
    {
        _dbContext.Upsert(new TestEntity())
            .NoUpdate()
            .Run();

        AssertSql();
    }

    [Fact]
    public void SqlSyntaxRunner_NoUpdate_Multiple()
    {
        _dbContext.UpsertRange(new TestEntity(), new TestEntity())
            .NoUpdate()
            .Run();

        AssertSql();
    }

    [Fact]
    public void SqlSyntaxRunner_NoUpdate_WithNullable()
    {
        _dbContext.Upsert(new TestEntityWithNullableKey())
            .On(e => new { e.ID1, e.ID2 })
            .NoUpdate()
            .Run();

        AssertSql();
    }

    [Fact]
    public void SqlSyntaxRunner_Update_Constant()
    {
        _dbContext.Upsert(new TestEntity())
            .WhenMatched(e => new TestEntity
            {
                Name = "value"
            })
            .Run();

        AssertSql();
    }

    [Fact]
    public void SqlSyntaxRunner_Update_Constant_Multiple()
    {
        _dbContext.UpsertRange(new TestEntity(), new TestEntity())
            .WhenMatched(e => new TestEntity
            {
                Name = "value"
            })
            .Run();

        AssertSql();
    }

    [Fact]
    public void SqlSyntaxRunner_Update_Source()
    {
        _dbContext.Upsert(new TestEntity())
            .WhenMatched((ed, en) => new TestEntity
            {
                Name = en.Name
            })
            .Run();

        AssertSql();
    }

    [Fact]
    public void SqlSyntaxRunner_Update_BinaryAdd()
    {
        _dbContext.Upsert(new TestEntity())
            .WhenMatched(e => new TestEntity
            {
                Total = e.Total + 5
            })
            .Run();

        AssertSql();
    }

    [Fact]
    public void SqlSyntaxRunner_Update_Coalesce()
    {
        _dbContext.Upsert(new TestEntity())
            .WhenMatched(e => new TestEntity
            {
                Status = e.Status ?? "suffix"
            })
            .Run();

        AssertSql();
    }

    [Fact]
    public void SqlSyntaxRunner_Update_BinaryAddMultiply()
    {
        _dbContext.Upsert(new TestEntity())
            .WhenMatched((ed, en) => new TestEntity
            {
                Total = (ed.Total + 5) * en.Total
            })
            .Run();

        AssertSql();
    }

    [Fact]
    public void SqlSyntaxRunner_Update_BinaryAddMultiplyGroup()
    {
        _dbContext.Upsert(new TestEntity())
            .WhenMatched((ed, en) => new TestEntity
            {
                Total = ed.Total + 3 * en.Total
            })
            .Run();

        AssertSql();
    }

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

        AssertSql();
    }

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

        AssertSql();
    }

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

        AssertSql();
    }

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

        AssertSql();
    }

    [Fact]
    public void SqlSyntaxRunner_Update_WatchWithNullCheck()
    {
        _dbContext.Upsert(new TestEntity())
            .WhenMatched((e, en) => new TestEntity
            {
                Name = en.Name == null ? "new" : en.Name
            })
            .Run();

        AssertSql();
    }
}
