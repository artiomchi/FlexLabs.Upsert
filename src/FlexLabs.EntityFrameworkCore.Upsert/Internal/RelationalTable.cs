using System;
using System.Collections.Generic;
using System.Linq;
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
        return entityType.Model.GetRelationalModel().Tables.First(t => t.Name == tblName && t.Schema == tblSchema);
    }

    private IEnumerable<IColumnBase> GetColumns(IEntityType entityType)
    {
        return entityType
            .GetProperties()
            .Where(IsPropertyValid)
            .Select(p => new RelationalColumn(
                Property: p,
                ColumnName: p.GetColumnName(),
                Owned: OwnershipType.None));
    }

    private IEnumerable<IColumnBase> GetOwnedColumns(IEntityType entityType, string? path = null, Func<object, object?>? getter = null)
    {
        // Find all properties of Owned Entities
        var owned = entityType.GetNavigations().Where(_ => _.ForeignKey.IsOwnership);

        foreach (var navigation in owned)
        {
            object? currentGetter(object entity)
            {
                var obj = getter is null ? entity : getter(entity);
                return obj != null
                    ? navigation.GetGetter().GetClrValueUsingContainingEntity(obj)
                    : null;
            }

            if (navigation.TargetEntityType.IsMappedToJson())
            {
                var columnName = navigation.TargetEntityType.GetContainerColumnName()
                    ?? throw new NotSupportedException($"Unsupported owned json column: '{navigation.Name}'. Failed to get column name.");

                var jsonColumn = GetTable(entityType).FindColumn(columnName)
                    ?? throw new NotSupportedException($"Unsupported owned json column: '{navigation.Name}'. Failed to get relational column.");

                yield return new JsonColumn(
                    Column: jsonColumn,
                    Navigation: navigation,
                    Name: navigation.Name,
                    ColumnName: columnName,
                    Owned: OwnershipType.Json,
                    Path: path);
            }
            else
            {
                // create a shadow property for FindColumn()
                yield return new OwnerColumn(
                    Name: navigation.Name,
                    ColumnName: null!,
                    Owned: OwnershipType.InlineOwner,
                    Path: path);

                var parent = navigation.TargetEntityType;
                var currentPath = $"{path}.{navigation.Name}";

                var properties = parent
                    .GetProperties()
                    .Where(IsPropertyValid);

                foreach (var property in properties)
                {
                    var columnName = (string?)property.FindAnnotation(RelationalAnnotationNames.ColumnName)?.Value;
                    if (columnName is null)
                    {
                        var table = StoreObjectIdentifier.Create(property.DeclaringType, StoreObjectType.Table);
                        columnName = table != null ? property.GetDefaultColumnName(table.Value) : null;
                    }

                    if (columnName is null)
                    {
                        throw new NotSupportedException($"Unsupported Owned Entity '{navigation.Name}'. Column name not found for {property.Name}.");
                    }

                    yield return new RelationalColumn(
                        Property: property,
                        ColumnName: columnName,
                        Owned: OwnershipType.Inline,
                        Path: currentPath,
                        EntityGetter: currentGetter);
                }

                foreach (var column in GetOwnedColumns(parent, currentPath, currentGetter))
                {
                    yield return column;
                }
            }
        }
    }

    private bool IsPropertyValid(IProperty property)
    {
        var valid =
            _queryOptions.AllowIdentityMatch ||
            property.ValueGenerated == ValueGenerated.Never ||
            property.GetAfterSaveBehavior() == PropertySaveBehavior.Save;
        if (!valid)
            return false;

        var pgIdentity = property
            .GetAnnotations()
            .FirstOrDefault(a => a.Name == "Npgsql:ValueGenerationStrategy")
            ?.Value?.ToString() == "IdentityAlwaysColumn";
        return !pgIdentity && !property.IsShadowProperty();
    }
}
