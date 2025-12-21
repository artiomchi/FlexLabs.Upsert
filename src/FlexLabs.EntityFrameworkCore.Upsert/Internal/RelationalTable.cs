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
            .Concat(GetComplexColumns(entityType))
            .Concat(GetOwnedColumns(entityType));

        AddColumnRange(columns);
    }

    internal IEntityType EntityType { get; }
    internal string TableName { get; }

    private static ITable GetTable(ITypeBase entityType)
    {
        var tblName = entityType.GetTableName() ?? entityType.GetViewName();
        var tblSchema = entityType.GetSchema();
        return entityType.Model.GetRelationalModel().Tables.First(t => t.Name == tblName && t.Schema == tblSchema);
    }

    private static ITableMapping? GetTableMapping(ITypeBase structuralType, IEntityType entityType)
    {
        foreach (var mapping in structuralType.GetTableMappings())
        {
            var table = mapping.Table;
            if (table.Name == entityType.GetTableName() && table.Schema == entityType.GetSchema())
            {
                return mapping;
            }
        }

        return null;
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

    private IEnumerable<IColumnBase> GetComplexColumns(ITypeBase entityType)
    {
        // Find all properties of Complex Properties
        var complexProperties = entityType.GetComplexProperties();

        foreach (var complexProperty in complexProperties)
        {
            foreach (var column in ProcessComplexProperty(complexProperty))
            {
                yield return column;
            }
        }

        yield break;

        IEnumerable<IColumnBase> ProcessComplexProperty(IComplexProperty complexProperty, string? path = null)
        {
            // Complex table shared properties with a child complex property mapped to JSON is currently not supported by ef core but requested:
            // see: https://github.com/dotnet/efcore/issues/36558
            // EF Core 10 throws the following exception in this case (Child is table shared complex and SubChild is mapped to JSON):
            // See Experiment: https://github.com/r-Larch/FlexLabs.Upsert/tree/feat/net10-complex-props-with-json
            // ```
            // System.InvalidOperationException
            // Complex property 'ParentComplexJson.Child#Child.SubChild' is mapped to JSON but its containing type 'ParentComplexJson.Child#Child' is not.
            // Map the root complex type to JSON. See https://github.com/dotnet/efcore/issues/36558.
            // ```
            // The following IF condition should therefore never be true in deeper recursions.
            // therefore, we could refactor this check to the caller level only, but we keep it here because once ef core adds support for this scenario, this would be the way to handle it.
            if (complexProperty.ComplexType.IsMappedToJson())
            {
                var columnName = complexProperty.ComplexType.GetContainerColumnName()
                                 ?? throw new NotSupportedException(Resources.FormatUnsupportedComplexJsonPropertyFailedToGetColumnName(complexProperty.Name));

                var jsonColumn = GetTable(entityType).FindColumn(columnName)
                                 ?? throw new NotSupportedException(Resources.FormatUnsupportedComplexJsonPropertyFailedToGetRelationalColumn(complexProperty.Name));

                yield return new ComplexJsonColumn(
                    Column: jsonColumn,
                    Property: complexProperty,
                    ColumnName: columnName,
                    Owned: OwnershipType.Json,
                    Path: path
                );
            }
            else
            {
                // create a shadow property for FindColumn()
                yield return new OwnerColumn(
                    Name: complexProperty.Name,
                    ColumnName: null!,
                    Owned: OwnershipType.InlineOwner,
                    Path: path);

                var parent = complexProperty.DeclaringType;
                var currentPath = $"{path}.{complexProperty.Name}";

                var complexTableMapping = GetTableMapping(complexProperty.ComplexType, parent.ContainingEntityType);
                if (complexTableMapping is null)
                {
                    throw new NotSupportedException(Resources.FormatUnsupportedComplexPropertyColumnMappingNotFound(complexProperty.Name));
                }

                foreach (var mapping in complexTableMapping.ColumnMappings)
                {
                    yield return new RelationalColumn(
                        Property: mapping.Property,
                        ColumnName: mapping.Column.Name,
                        Owned: OwnershipType.Inline,
                        Path: currentPath,
                        EntityGetter: null);
                }

                var properties = complexProperty.ComplexType.GetComplexProperties();
                foreach (var property in properties)
                {
                    foreach (var column in ProcessComplexProperty(property, currentPath))
                    {
                        yield return column;
                    }
                }
            }
        }
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
                    ?? throw new NotSupportedException(Resources.FormatUnsupportedOwnedJsonColumnFailedToGetColumnName(navigation.Name));

                var jsonColumn = GetTable(entityType).FindColumn(columnName)
                    ?? throw new NotSupportedException(Resources.FormatUnsupportedOwnedJsonColumnFailedToGetRelationalColumn(navigation.Name));

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
                        throw new NotSupportedException(Resources.FormatUnsupportedOwnedEntityColumnNameNotFoundForProperty(navigation.Name, property.Name));
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
