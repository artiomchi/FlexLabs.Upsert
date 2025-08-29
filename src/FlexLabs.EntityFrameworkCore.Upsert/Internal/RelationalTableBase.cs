using System;
using System.Collections.Generic;
using System.Linq;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

internal abstract class RelationalTableBase
{
    private readonly Dictionary<LevelName, IColumnBase> _columns = [];

    public void AddColumn(IColumnBase column)
    {
        _columns.Add(new LevelName(column.Name, column.Path), column);
    }

    public void AddColumnRange(IEnumerable<IColumnBase> columns)
    {
        foreach (var column in columns)
        {
            AddColumn(column);
        }
    }

    /// <summary>
    /// List of all table mapped columns.
    /// </summary>
    public IEnumerable<IColumnBase> Columns => _columns.Values.Where(c => c.Owned != OwnershipType.InlineOwner);

    /// <summary>
    /// Search a table column.
    /// </summary>
    /// <param name="name">Clr name of the column to search</param>
    public IColumnBase? FindColumn(string name)
    {
        return _columns.GetValueOrDefault(new LevelName(name, null));
    }

    /// <summary>
    /// Search column of inlined owned entity.
    /// </summary>
    /// <param name="column">Must be InlineOwner</param>
    /// <param name="name">Clr name of the column to search</param>
    public IColumnBase? FindColumn(IColumnBase column, string name)
    {
        if (column.Owned == OwnershipType.InlineOwner)
        {
            return _columns.GetValueOrDefault(new LevelName(name, $"{column.Path}.{column.Name}"));
        }

        return null;
    }

    /// <summary>
    /// Return all columns of an owned entity including columns of nested owned entities.
    /// </summary>
    /// <param name="column">Must be InlineOwner</param>
    public IEnumerable<IColumnBase> FindColumnFor(IColumnBase column)
    {
        if (column.Owned == OwnershipType.InlineOwner)
        {
            var path = $"{column.Path}.{column.Name}";
            foreach (var (key, value) in _columns)
            {
                if (key.Path == path)
                {
                    if (value.Owned == OwnershipType.InlineOwner)
                    {
                        foreach (var col in FindColumnFor(value))
                        {
                            yield return col;
                        }
                    }
                    else
                    {
                        yield return value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Serves as a dictionary key for hierarchical access.
    /// </summary>
    private readonly record struct LevelName(string Name, string? Path) : IComparable<LevelName>
    {
        public int CompareTo(LevelName other)
        {
            var compare1 = string.Compare(Path, other.Path, StringComparison.Ordinal);
            if (compare1 != 0) return compare1;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
    }
}
