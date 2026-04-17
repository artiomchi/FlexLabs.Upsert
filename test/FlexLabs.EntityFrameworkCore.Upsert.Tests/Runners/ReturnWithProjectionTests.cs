using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners;

/// <summary>
/// Tests for GenerateCommand with returnColumns (deleted/inserted projection) for SQL Server,
/// and tests that unsupported providers throw the expected exceptions.
/// </summary>
public class ReturnWithProjectionTests
{
    private readonly SqlServerUpsertCommandRunner _sqlServerRunner = new();
    private readonly PostgreSqlUpsertCommandRunner _postgresRunner = new();
    private readonly MySqlUpsertCommandRunner _mysqlRunner = new();
    private readonly OracleUpsertCommandRunner _oracleRunner = new();

    // Minimal entity data for GenerateCommand
    private static ICollection<ICollection<(string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts)>> MakeEntities()
    {
        var cv = new ConstantValue(1); cv.ArgumentIndex = 0;
        return [[(ColumnName: "ID", Value: cv, DefaultSql: null, AllowInserts: true),]];
    }

    private static ICollection<(string ColumnName, bool IsNullable)> MakeJoinColumns()
        => [("ID", false)];

    private static IEntityType BuildEntityType()
    {
        var model = new Model();
        var clrType = typeof(TestEntity);
        var entityType = model.AddEntityType(clrType, true, ConfigurationSource.Convention);
        foreach (var prop in clrType.GetProperties())
            entityType.AddProperty(prop.Name, ConfigurationSource.Explicit);
        var idProp = entityType.FindProperty("ID")!;
        entityType.AddKey(idProp, ConfigurationSource.Convention);
        return entityType;
    }

    // ── SQL Server GenerateCommand with returnColumns ──────────────────────────

    [Fact]
    public void SqlServer_GenerateCommand_ReturnColumns_InsertedOnly()
    {
        var returnColumns = new List<(string Alias, bool IsDeletedParam, string ColumnName)>
        {
            ("Name", false, "Name"),
            ("Total", false, "Total"),
        };

        var sql = _sqlServerRunner.GenerateCommand(
            "[TestEntity]",
            MakeEntities(),
            MakeJoinColumns(),
            updateExpressions: null,
            updateCondition: null,
            returnColumns: returnColumns);

        Assert.Contains("OUTPUT inserted.[Name] AS [Name], inserted.[Total] AS [Total]", sql);
        Assert.DoesNotContain("inserted.*", sql);
    }

    [Fact]
    public void SqlServer_GenerateCommand_ReturnColumns_DeletedAndInserted()
    {
        var returnColumns = new List<(string Alias, bool IsDeletedParam, string ColumnName)>
        {
            ("OldName", true, "Name"),
            ("NewName", false, "Name"),
        };

        var sql = _sqlServerRunner.GenerateCommand(
            "[TestEntity]",
            MakeEntities(),
            MakeJoinColumns(),
            updateExpressions: null,
            updateCondition: null,
            returnColumns: returnColumns);

        Assert.Contains("OUTPUT deleted.[Name] AS [OldName], inserted.[Name] AS [NewName]", sql);
    }

    [Fact]
    public void SqlServer_GenerateCommand_EmptyReturnColumns_ReturnsAll()
    {
        // empty collection = returnResult=true (all columns via OUTPUT inserted.*)
        var sql = _sqlServerRunner.GenerateCommand(
            "[TestEntity]",
            MakeEntities(),
            MakeJoinColumns(),
            updateExpressions: null,
            updateCondition: null,
            returnColumns: []);

        Assert.Contains("OUTPUT inserted.*", sql);
    }

    [Fact]
    public void SqlServer_GenerateCommand_NullReturnColumns_NoOutput()
    {
        var sql = _sqlServerRunner.GenerateCommand(
            "[TestEntity]",
            MakeEntities(),
            MakeJoinColumns(),
            updateExpressions: null,
            updateCondition: null,
            returnColumns: null);

        Assert.DoesNotContain("OUTPUT", sql);
    }

    // ── PostgreSQL ────────────────────────────────────────────────────────────

    [Fact]
    public void PostgreSql_GenerateCommand_EmptyReturnColumns_ReturnsAll()
    {
        var sql = _postgresRunner.GenerateCommand(
            "\"TestEntity\"",
            MakeEntities(),
            MakeJoinColumns(),
            updateExpressions: null,
            updateCondition: null,
            returnColumns: []);

        Assert.Contains("RETURNING *", sql);
    }

    [Fact]
    public void PostgreSql_GenerateCommand_NonEmptyReturnColumns_Throws()
    {
        var returnColumns = new List<(string Alias, bool IsDeletedParam, string ColumnName)>
        {
            ("Name", false, "Name"),
        };

        Assert.Throws<NotSupportedException>(() => _postgresRunner.GenerateCommand(
            "\"TestEntity\"",
            MakeEntities(),
            MakeJoinColumns(),
            updateExpressions: null,
            updateCondition: null,
            returnColumns: returnColumns));
    }

    [Fact]
    public void MySql_GenerateCommand_ReturnColumns_Throws()
    {
        var returnColumns = new List<(string Alias, bool IsDeletedParam, string ColumnName)>
        {
            ("Name", false, "Name"),
        };

        Assert.Throws<NotImplementedException>(() => _mysqlRunner.GenerateCommand(
            "`TestEntity`",
            MakeEntities(),
            MakeJoinColumns(),
            updateExpressions: null,
            updateCondition: null,
            returnColumns: returnColumns));
    }

    [Fact]
    public void MySql_GenerateCommand_EmptyReturnColumns_Throws()
    {
        // Even empty collection (= returnResult=true) is not supported by MySQL
        Assert.Throws<NotImplementedException>(() => _mysqlRunner.GenerateCommand(
            "`TestEntity`",
            MakeEntities(),
            MakeJoinColumns(),
            updateExpressions: null,
            updateCondition: null,
            returnColumns: []));
    }

    [Fact]
    public void Oracle_GenerateCommand_ReturnColumns_Throws()
    {
        var returnColumns = new List<(string Alias, bool IsDeletedParam, string ColumnName)>
        {
            ("Name", false, "Name"),
        };

        Assert.Throws<NotImplementedException>(() => _oracleRunner.GenerateCommand(
            "\"TestEntity\"",
            MakeEntities(),
            MakeJoinColumns(),
            updateExpressions: null,
            updateCondition: null,
            returnColumns: returnColumns));
    }

    // ── ParseReturnExpression ─────────────────────────────────────────────────

    [Fact]
    public void ParseReturnExpression_MemberInit_Parses()
    {
        var entityType = BuildEntityType();

        Expression<Func<TestEntity, TestEntity, TestEntity>> expr =
            (deleted, inserted) => new TestEntity { Name = deleted.Name };

        var columns = RelationalUpsertCommandRunner.ParseReturnExpression(expr, entityType);

        Assert.Single(columns);
        var col = columns.First();
        Assert.Equal("Name", col.Alias);
        Assert.True(col.IsDeletedParam);
        Assert.Equal("Name", col.ColumnName);
    }

    [Fact]
    public void ParseReturnExpression_AnonymousType_Parses()
    {
        var entityType = BuildEntityType();

        Expression<Func<TestEntity, TestEntity, object>> expr =
            (deleted, inserted) => new { OldName = deleted.Name, NewName = inserted.Name };

        var columns = RelationalUpsertCommandRunner.ParseReturnExpression(expr, entityType);

        Assert.Equal(2, columns.Count);
        var list = columns.ToList();
        Assert.Equal("OldName", list[0].Alias);
        Assert.True(list[0].IsDeletedParam);
        Assert.Equal("Name", list[0].ColumnName);
        Assert.Equal("NewName", list[1].Alias);
        Assert.False(list[1].IsDeletedParam);
        Assert.Equal("Name", list[1].ColumnName);
    }

    [Fact]
    public void ParseReturnExpression_InsertedProperty_Parses()
    {
        var entityType = BuildEntityType();

        Expression<Func<TestEntity, TestEntity, object>> expr =
            (deleted, inserted) => new { NewTotal = inserted.Total };

        var columns = RelationalUpsertCommandRunner.ParseReturnExpression(expr, entityType);

        Assert.Single(columns);
        var col = columns.First();
        Assert.Equal("NewTotal", col.Alias);
        Assert.False(col.IsDeletedParam);
        Assert.Equal("Total", col.ColumnName);
    }
}
