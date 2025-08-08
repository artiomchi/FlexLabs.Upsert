using System;
using System.Collections.Generic;
using System.Linq;
using FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

internal sealed class RelationalTable : RelationalTableBase
{
    private readonly RunnerQueryOptions _queryOptions;

    internal RelationalTable(IEntityType entityType, string tableName, RunnerQueryOptions queryOptions)
    {
        _queryOptions = queryOptions;
        EntityType = entityType;
        TableName = tableName;

        var columns = GetColumns(entityType)
            .Concat(GetOwnedColumns(entityType));

        AddColumnRange(columns);
    }

    internal IEntityType EntityType { get; }
    internal string TableName { get; }

    private static ITable GetTable(IEntityType entityType)
    {
        var tblName = entityType.GetTableName() ?? entityType.GetViewName();
        var tblSchema = entityType.GetSchema();
        return entityType.Model.GetRelationalModel().Tables.First(_ => _.Name == tblName && _.Schema == tblSchema);
    }

    private IEnumerable<IColumnBase> GetColumns(IEntityType entityType)
    {
        var properties = entityType
            .GetProperties()
            .Where(ValidProperty);

        foreach (var property in properties)
        {
            yield return new RelationalColumn(
                Property: property,
                ColumnName: property.GetColumnName(),
                Owned: Owned.None
            );
        }
    }

    private IEnumerable<IColumnBase> GetOwnedColumns(IEntityType entityType, string? path = null, Func<object, object?>? getter = null)
    {
        // Find all properties of Owned Entities
        var owned = entityType.GetNavigations().Where(_ => _.ForeignKey.IsOwnership);

        foreach (var navigation in owned)
        {
            if (navigation.TargetEntityType.IsMappedToJson())
            {
                var columnName = navigation.TargetEntityType.GetContainerColumnName();
                if (columnName is null)
                {
                    throw new NotSupportedException($"Unsupported owned json column: '{navigation.Name}'. Failed to get column name.");
                }

                var jsonColumn = GetTable(entityType).FindColumn(columnName);
                if (jsonColumn is null)
                {
                    throw new NotSupportedException($"Unsupported owned json column: '{navigation.Name}'. Failed to get relational column.");
                }

                yield return new JsonColumn(
                    Column: jsonColumn,
                    Navigation: navigation,
                    Name: navigation.Name,
                    ColumnName: columnName,
                    Owned: Owned.Json,
                    Path: path
                );
            }
            else
            {
                // create a shadow property for FindColumn()
                yield return new OwnerColumn(
                    Name: navigation.Name,
                    ColumnName: null!,
                    Owned: Owned.InlineOwner,
                    Path: path
                );

                var parent = navigation.TargetEntityType;
                var currentPath = $"{path}.{navigation.Name}";

                var currentGetter = (object entity) =>
                {
                    var obj = getter is null ? entity : getter(entity);
                    return obj switch
                    {
                        null => null,
                        _ => navigation.GetGetter().GetClrValueUsingContainingEntity(obj),
                    };
                };

                var properties = parent
                    .GetProperties()
                    .Where(ValidProperty);

                foreach (var property in properties)
                {
                    var columnName = (string?)property.FindAnnotation(RelationalAnnotationNames.ColumnName)?.Value;
                    if (columnName is null)
                    {
                        var table = StoreObjectIdentifier.Create(property.DeclaringType, StoreObjectType.Table);
                        columnName = table switch
                        {
                            null => null,
                            _ => property.GetDefaultColumnName(table.Value),
                        };
                    }

                    if (columnName is null)
                    {
                        throw new NotSupportedException($"Unsupported Owned Entity '{navigation.Name}'. Column name not found for {property.Name}.");
                    }

                    yield return new RelationalColumn(
                        Property: property,
                        ColumnName: columnName,
                        Owned: Owned.Inline,
                        Path: currentPath,
                        EntityGetter: currentGetter
                    );
                }

                foreach (var column in GetOwnedColumns(parent, currentPath, currentGetter))
                {
                    yield return column;
                }
            }
        }
    }

    private bool ValidProperty(IProperty property)
    {
        var valid = _queryOptions.AllowIdentityMatch ||
                    property.ValueGenerated == ValueGenerated.Never ||
                    property.GetAfterSaveBehavior() == PropertySaveBehavior.Save;

        var pgIdentity = property
            .GetAnnotations()
            .FirstOrDefault(a => a.Name == "Npgsql:ValueGenerationStrategy")
            ?.Value?.ToString() == "IdentityAlwaysColumn";

        return valid && !pgIdentity && !property.IsShadowProperty();
    }
}

internal sealed record RelationalColumn(
    IProperty Property,
    string ColumnName,
    Owned Owned,
    string? Path = null,
    Func<object, object?>? EntityGetter = null
) : IColumnBase
{
    public string Name => Property.Name;

    public (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        var obj = entity;
        if (EntityGetter is not null)
        {
            obj = EntityGetter(entity);
        }

        var rawValue = obj switch
        {
            null => null,
            _ => Property.GetGetter().GetClrValueUsingContainingEntity(obj),
        };

        string? defaultSql = null;
        if (rawValue == null)
        {
            if (Property.GetDefaultValue() != null)
                rawValue = Property.GetDefaultValue();
            else
                defaultSql = Property.GetDefaultValueSql();
        }

        var value = new ConstantValue(rawValue, this);
        var allowInserts = Property.ValueGenerated == ValueGenerated.Never || Property.GetAfterSaveBehavior() == PropertySaveBehavior.Save;

        return (ColumnName, value, defaultSql, allowInserts);
    }
}

internal sealed record OwnerColumn(
    string Name,
    string ColumnName,
    Owned Owned,
    string? Path
) : IColumnBase
{
    public (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity) => throw new NotSupportedException();
}

internal sealed record JsonColumn(
    IColumn Column,
    INavigation Navigation,
    string Name,
    string ColumnName,
    Owned Owned,
    string? Path
) : IColumnBase
{
    public (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        var rawValue = Navigation.GetGetter().GetClrValueUsingContainingEntity(entity);
        var jsonValue = rawValue switch
        {
            null => null,
            _ => Column.StoreTypeMapping.GenerateProviderValueSqlLiteral(rawValue).Trim('\''),
        };

        var value = new ConstantValue(jsonValue, this);

        return (ColumnName, value, DefaultSql: null, AllowInserts: true);
    }
}

/// <summary>
/// This class represents a database column
/// </summary>
public interface IColumnBase
{
    /// <summary>
    /// The clr Name
    /// </summary>
    string Name { get; }
    /// <summary>
    /// The database name
    /// </summary>
    string ColumnName { get; }
    /// <summary>
    /// The ownership mode
    /// </summary>
    Owned Owned { get; }
    /// <summary>
    /// A hierarchical path for owned columns
    /// </summary>
    string? Path { get; }

    /// <summary>
    /// Reads the column value from an entity.
    /// </summary>
    (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity);
}

/// <summary>
/// Represents various types of owned properties.
/// </summary>
public enum Owned
{
    /// <summary>
    /// Not owned.
    /// </summary>
    None,
    /// <summary>
    /// Owned and inlined into the table.
    /// </summary>
    Inline,
    /// <summary>
    /// Owner of inlined properties.
    /// </summary>
    InlineOwner,
    /// <summary>
    /// Owned with json conversation.
    /// </summary>
    Json,
}
