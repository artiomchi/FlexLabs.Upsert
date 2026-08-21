namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners.Models;

/// <summary>
/// Matched on a column whose only unique index is partial, which is the case that can't be expressed
/// by a conflict target listing column names alone.
/// </summary>
public class TestEntityWithFilteredIndex
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public int Total { get; set; }
}
