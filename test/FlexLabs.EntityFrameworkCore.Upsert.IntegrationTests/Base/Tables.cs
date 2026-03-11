using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

public class Book
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string[] Genres { get; set; }
}

public class Country
{
    public int ID { get; set; }
    [Required, StringLength(50)]
    public string Name { get; set; }
    [Required, StringLength(2)]
    public string ISO { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
}

[Table("Dash-Table")]
public class DashTable
{
    public int ID { get; set; }
    [Column("Data-Set")]
    public string DataSet { get; set; }
    public DateTime Updated { get; set; }
}

public class JsonData
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ID { get; set; }
    public string Data { get; set; }
    public ChildObject Child { get; set; }
}

public class JsonDocumentData : IDisposable
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ID { get; set; }
    public JsonDocument Data { get; set; }
    public void Dispose() => Data?.Dispose();
}

public class ChildObject
{
    public string Value { get; set; }
    public DateTimeOffset Time { get; set; }
}

public class JObjectData
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ID { get; set; }
    public JObject Data { get; set; }
}

public class PageVisit
{
    public static readonly Expression<Func<PageVisit, object>> MatchKey
        = pv => new { pv.UserID, pv.Date };

    public int ID { get; set; }
    public int UserID { get; set; }
    public DateTime Date { get; set; }
    public int Visits { get; set; }
    public DateTime FirstVisit { get; set; }
    public DateTime LastVisit { get; set; }
}
public class SchemaTable
{
    public int ID { get; set; }
    public int Name { get; set; }
    public DateTime Updated { get; set; }
}

public class Status
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ID { get; set; }
    public string Name { get; set; }
    public DateTime LastChecked { get; set; }
}

public class GuidKeyAutoGen
{
    public Guid ID { get; set; }
    public string Name { get; set; }
}

public class GuidKey
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid ID { get; set; }
    public string Name { get; set; }
}

public class StringKeyAutoGen
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string ID { get; set; }
    public string Name { get; set; }
}

public class StringKey
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string ID { get; set; }
    public string Name { get; set; }
}

public class KeyOnly
{
    public int ID1 { get; set; }
    public int ID2 { get; set; }
}

public class NullableCompositeKey
{
    public int ID { get; set; }
    public int ID1 { get; set; }
    public int? ID2 { get; set; }
    public string Value { get; set; }
}

public class TestEntity
{
    public int ID { get; set; }
    public int Num1 { get; set; }
    public int Num2 { get; set; }
    public int? NumNullable1 { get; set; }
    public string Text1 { get; set; }
    public string Text2 { get; set; }
    public short Short1 { get; set; }
    public DateTime Updated { get; set; }
}

public class TestEntityFiltered
{
    public int ID { get; set; }
    public string Key { get; set; } = "default";
    public int Counter { get; set; }
    public bool IsDeleted { get; set; }
}

public class ULongEntity
{
    public int ID { get; set; }
    public int Num1 { get; set; }
    public ulong Counter { get; set; }
}

public class NullableRequired
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ID { get; set; }
    [Required]
    public string Text { get; set; }
}

public class GeneratedAlwaysAsIdentity
{
    public int ID { get; set; }
    public int Num1 { get; set; }
    public int Num2 { get; set; }
}

public class ComputedColumn
{
    public int ID { get; set; }
    public int Num1 { get; set; }
    public int Num2 { get; set; }
    public int Num3 { get; set; }
}

/// <summary>
/// Child and SubChild are mapped as complex columns
/// </summary>
public class ParentComplex
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ID { get; set; }
    public Child Child { get; set; }
    public int Counter { get; set; }
}

public class Parent
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ID { get; set; }
    public Child Child { get; set; }
    public int Counter { get; set; }
}

public class Child
{
    [Required]
    public string ChildName { get; set; }
    public int Age { get; set; }
    public SubChild SubChild { get; set; }
}

public class SubChild
{
    public string SubChildName { get; set; }
    public int Age { get; set; }
}

public class CompanyComplexJson
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    public string Name { get; set; }
    public CompanyMeta Meta { get; set; }
}

public class CompanyOwnedJson
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    public string Name { get; set; }
    public CompanyMeta Meta { get; set; }
}

public class CompanyMeta
{
    [Required]
    public string Required { get; set; }
    [JsonPropertyName("json_override")]
    public string JsonOverride { get; set; }
    public CompanyNestedMeta Nested { get; set; }
    public List<CompanyMetaValue> Properties { get; set; }
}

public class CompanyNestedMeta
{
    public string Title { get; set; }
}

public class CompanyMetaValue
{
    public string Key { get; set; }
    public string Value { get; set; }
}
