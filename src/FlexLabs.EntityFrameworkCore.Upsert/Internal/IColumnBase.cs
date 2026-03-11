using FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

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
    OwnershipType Owned { get; }
    /// <summary>
    /// A hierarchical path for owned columns
    /// </summary>
    string? Path { get; }

    /// <summary>
    /// Reads the column value from an entity.
    /// </summary>
    (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity);
}
