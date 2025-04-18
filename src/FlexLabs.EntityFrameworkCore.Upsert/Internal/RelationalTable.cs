using System;
using System.Collections.Generic;
using System.Linq;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;


namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

internal sealed class RelationalTable {
    private readonly RunnerQueryOptions _queryOptions;
    private readonly SortedDictionary<LevelName, IColumnBase> _columns = new();

    private readonly record struct LevelName(int Level, IEntityType Parent, string Name) : IComparable<LevelName> {
        public int CompareTo(LevelName other)
        {
            var levelComparison = Level.CompareTo(other.Level);
            if (levelComparison != 0) return levelComparison;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
    }


    internal RelationalTable(IEntityType entityType, string tableName, RunnerQueryOptions queryOptions)
    {
        _queryOptions = queryOptions;
        EntityType = entityType;
        TableName = tableName;

        var columns = GetColumns(entityType)
            .Concat(GetOwnedColumns(entityType));

        foreach (var column in columns) {
            _columns.Add(new LevelName(column.Level, column.Parent, column.Name), column);
        }
    }


    internal IEntityType EntityType { get; }
    internal string TableName { get; }
    internal IEnumerable<IColumnBase> Columns => _columns.Values.Where(_ => _.Owned is not Owned.InlineOwner);


    internal IColumnBase? FindColumn(string name)
    {
        return _columns.GetValueOrDefault(new LevelName(1, EntityType, name));
    }

    internal IColumnBase? FindColumn(IColumnBase column, string name)
    {
        if (column is OwnerColumn col) {
            return _columns.GetValueOrDefault(new LevelName(col.Level + 1, col.Parent, name));
        }

        return null;
    }


    private IEnumerable<IColumnBase> GetColumns(IEntityType entityType)
    {
        var properties = entityType
            .GetProperties()
            .Where(ValidProperty);

        foreach (var property in properties) {
            yield return new RelationalColumn(
                Parent: entityType,
                EntityGetter: null,
                Property: property,
                ColumnName: property.GetColumnName(),
                Owned: Owned.None,
                Level: 1
            );
        }
    }


    private IEnumerable<IColumnBase> GetOwnedColumns(IEntityType entityType, int level = 1, Func<object, object?>? getter = null)
    {
        // Find all properties of Owned Entities
        var owned = entityType.GetNavigations().Where(_ => _.ForeignKey.IsOwnership);

        foreach (var navigation in owned) {
            if (true) {
                var currentLevel = level + 1;
                var parent = navigation.TargetEntityType;
                var currentGetter = (object entity) => navigation.GetGetter().GetClrValueUsingContainingEntity(getter is null ? entity : getter(entity)!);

                // create a shadow property for FindColumn()
                yield return new OwnerColumn(
                    Parent: parent,
                    Name: navigation.Name,
                    ColumnName: null!,
                    Owned: Owned.InlineOwner,
                    Level: level
                );

                var properties = parent
                    .GetProperties()
                    .Where(ValidProperty);

                foreach (var property in properties) {
                    var columnName = (string?) property.FindAnnotation(RelationalAnnotationNames.ColumnName)?.Value;
                    if (columnName is null) {
                        var table = StoreObjectIdentifier.Create(property.DeclaringType, StoreObjectType.Table);
                        columnName = table switch {
                            null => null,
                            _ => property.GetDefaultColumnName(table.Value),
                        };
                    }

                    if (columnName is null) {
                        throw new NotSupportedException($"Unsupported Owned Entity '{navigation.Name}'. Column name not found for {property.Name}.");
                    }

                    yield return new RelationalColumn(
                        Parent: parent,
                        Property: property,
                        EntityGetter: currentGetter,
                        ColumnName: columnName,
                        Owned: Owned.Inline,
                        Level: currentLevel
                    );
                }

                foreach (var column in GetOwnedColumns(parent, level: currentLevel, currentGetter)) {
                    yield return column;
                }
            }
            else {
                // JSON
                //yield return new RelationalColumn(
                //    Table: this,
                //    Parent: entityType,
                //    Property: navigation,
                //    Navigation: navigation,
                //    ColumnName: navigation.GetDefaultColumnName(table.Value),
                //    Owned: Owned.Json,
                //    Level: level
                //);
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
    IEntityType Parent,
    IProperty Property,
    Func<object, object?>? EntityGetter,
    string ColumnName,
    Owned Owned,
    int Level
) : IColumnBase {
    public string Name => Property.Name;

    public (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        var obj = entity;
        if (EntityGetter is not null) {
            obj = EntityGetter(entity);
        }

        var rawValue = obj switch {
            null => null,
            _ => Property.GetGetter().GetClrValueUsingContainingEntity(obj),
        };

        string? defaultSql = null;
        if (rawValue == null) {
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
    IEntityType Parent,
    string Name,
    string ColumnName,
    Owned Owned,
    int Level
) : IColumnBase {
    public (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity) => throw new NotSupportedException();
}

/// <summary>
/// This class represents a database column
/// </summary>
public interface IColumnBase {
    /// <summary>
    /// </summary>
    IEntityType Parent { get; }
    /// <summary>
    /// The clr Name
    /// </summary>
    string Name { get; }
    /// <summary>
    /// The database name
    /// </summary>
    string ColumnName { get; }
    /// <summary>
    /// </summary>
    Owned Owned { get; }
    /// <summary>
    /// </summary>
    int Level { get; }
    /// <summary>
    /// Reads the column value from an entity.
    /// </summary>
    (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity);
}

/// <summary>
/// Represents various types of owned properties.
/// </summary>
public enum Owned {
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
