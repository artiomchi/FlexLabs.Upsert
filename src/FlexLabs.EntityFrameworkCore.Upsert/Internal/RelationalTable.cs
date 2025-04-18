using System;
using System.Collections.Generic;
using System.Linq;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;


namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

internal sealed class RelationalTable {
    private readonly RunnerQueryOptions _queryOptions;
    private readonly SortedDictionary<string, IColumnBase> _columns = new();


    internal RelationalTable(IEntityType entityType, string tableName, RunnerQueryOptions queryOptions)
    {
        _queryOptions = queryOptions;
        EntityType = entityType;
        TableName = tableName;

        var columns = GetColumns(entityType);

        foreach (var column in columns) {
            _columns.Add(column.Name, column);
        }
    }


    internal IEntityType EntityType { get; }
    internal string TableName { get; }
    internal IEnumerable<IColumnBase> Columns => _columns.Values;


    internal IColumnBase? FindColumn(string name)
    {
        return _columns.GetValueOrDefault(name);
    }


    private IEnumerable<IColumnBase> GetColumns(IEntityType entityType)
    {
        var properties = entityType
            .GetProperties()
            .Where(ValidProperty);

        foreach (var property in properties) {
            yield return new RelationalColumn(
                Property: property,
                ColumnName: property.GetColumnName(),
                Level: 1
            );
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
    int Level
) : IColumnBase {
    public string Name => Property.Name;

    public (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        var rawValue = Property.GetGetter().GetClrValueUsingContainingEntity(entity);

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

/// <summary>
/// This class represents a database column
/// </summary>
public interface IColumnBase {
    /// <summary>
    /// The clr Name
    /// </summary>
    string Name { get; }
    /// <summary>
    /// The database name
    /// </summary>
    string ColumnName { get; }
    /// <summary>
    /// Reads the column value from an entity.
    /// </summary>
    (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity);
}
