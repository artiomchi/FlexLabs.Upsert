using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>().HasIndex(b => b.Num1).IsUnique();
        modelBuilder.Entity<TestEntity>().Property(e => e.Num2).HasDefaultValue(27);
        modelBuilder.Entity<SentinelEntity>().HasIndex(b => b.Num1).IsUnique();
        modelBuilder.Entity<SentinelEntity>().Property(e => e.Num2).HasDefaultValue(27).HasSentinel(-1);
        modelBuilder.Entity<TestEntityFiltered>().HasIndex(b => b.Key).IsUnique();
        modelBuilder.Entity<TestEntityFiltered>().HasQueryFilter(b => !b.IsDeleted);
        modelBuilder.Entity<ULongEntity>().HasIndex(b => b.Num1).IsUnique();
        modelBuilder.Entity<Book>().HasIndex(b => b.Name).IsUnique();
        modelBuilder.Entity<Book>().Property(b => b.Genres)
            .HasConversion(g => string.Join(",", g), s => s.Split(new[] { ',' }));
        modelBuilder.Entity<Book>().Property<DateTime?>("NonMappedColumn");
        modelBuilder.Entity<Country>().HasIndex(c => c.ISO).IsUnique();
        modelBuilder.Entity<DashTable>().HasIndex(t => t.DataSet).IsUnique();
        modelBuilder.Entity<JObjectData>()
            .Property(d => d.Data)
            .HasConversion(
                v => v.ToJsonString(),
                v => JsonNode.Parse(v)!.AsObject());
        modelBuilder.Entity<PageVisit>().HasIndex(pv => new { pv.UserID, pv.Date }).IsUnique();
        modelBuilder.Entity<SchemaTable>().HasIndex(t => t.Name).IsUnique();
        modelBuilder.Entity<KeyOnly>().HasKey(t => new { t.ID1, t.ID2 });
        modelBuilder.Entity<NullableCompositeKey>().HasIndex(t => new { t.ID1, t.ID2 }).IsUnique().HasFilter(null);
        modelBuilder.Entity<GeneratedAlwaysAsIdentity>().HasIndex(b => b.Num1).IsUnique();
        modelBuilder.Entity<GeneratedAlwaysAsIdentity>().Property(e => e.Num2).UseIdentityAlwaysColumn();
        modelBuilder.Entity<ComputedColumn>().HasIndex(b => b.Num1).IsUnique();
        modelBuilder.Entity<ComputedColumn>().Property(e => e.Num3)
            .HasComputedColumnSql($"{EscapeColumn(nameof(ComputedColumn.Num2))} + 1", stored: true);

        modelBuilder.Entity<Parent>()
            .OwnsOne(
                c => c.Child,
                b => { b.OwnsOne(c => c.SubChild); });

        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<JsonData>().Property(j => j.Data).HasColumnType("jsonb");
            modelBuilder.Entity<JsonData>().Property(j => j.Child).HasColumnType("jsonb");
        }
        else
        {
            modelBuilder.Entity<JsonData>().Ignore(j => j.Child);
            modelBuilder.Entity<JsonDocumentData>().Ignore(j => j.Data);
        }

        modelBuilder.Entity<CompanyOwnedJson>()
            .OwnsOne(
                j => j.Meta,
                b =>
                {
                    b.ToJson();
                    b.OwnsOne(c => c.Nested);
                    b.OwnsMany(c => c.Properties);
                });

        if (Database.IsInMemory())
        {
            // in-memory provider does not support complex properties
            modelBuilder.Entity<ParentComplex>().Ignore(c => c.Child);
            modelBuilder.Entity<CompanyComplexJson>().Ignore(_ => _.Meta);
        }
        else
        {
            modelBuilder.Entity<ParentComplex>()
                .ComplexProperty(
                    c => c.Child,
                    b => b.ComplexProperty(c => c.SubChild));

            modelBuilder.Entity<CompanyComplexJson>()
                .ComplexProperty(j => j.Meta, b => b.ToJson());
        }

        if (!Database.IsMySql()) // Can't have a default value on TEXT columns in MySql
        {
            modelBuilder.Entity<NullableRequired>().Property(e => e.Text).HasDefaultValue("B");
        }

        if (!Database.IsMySql() && !Database.IsOracle()) // Can't have table schemas in MySql and Oracle
        {
            modelBuilder.Entity<SchemaTable>().Metadata.SetSchema("testsch");
        }
    }

    private string EscapeColumn(string columnName)
    {
        if (Database.IsSqlServer())
        {
            return $"[{columnName}]";
        }
        if (Database.IsMySql())
        {
            return $"`{columnName}`";
        }
        if (Database.IsOracle())
        {
            return columnName.ToUpper();
        }
        return $"\"{columnName}\"";
    }

    public DbSet<Book> Books { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<DashTable> DashTable { get; set; }
    public DbSet<GuidKey> GuidKeys { get; set; }
    public DbSet<GuidKeyAutoGen> GuidKeysAutoGen { get; set; }
    public DbSet<JObjectData> JObjectDatas { get; set; }
    public DbSet<JsonData> JsonDatas { get; set; }
    public DbSet<JsonDocumentData> JsonDocumentDatas { get; set; }
    public DbSet<KeyOnly> KeyOnlies { get; set; }
    public DbSet<NullableCompositeKey> NullableCompositeKeys { get; set; }
    public DbSet<NullableRequired> NullableRequireds { get; set; }
    public DbSet<PageVisit> PageVisits { get; set; }
    public DbSet<SchemaTable> SchemaTable { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<StringKey> StringKeys { get; set; }
    public DbSet<StringKeyAutoGen> StringKeysAutoGen { get; set; }
    public DbSet<SentinelEntity> SentinelEntities { get; set; }
    public DbSet<TestEntity> TestEntities { get; set; }
    public DbSet<TestEntityFiltered> TestEntitiesFiltered { get; set; }
    public DbSet<ULongEntity> ULongEntities { get; set; }
    public DbSet<GeneratedAlwaysAsIdentity> GeneratedAlwaysAsIdentity { get; set; }
    public DbSet<ComputedColumn> ComputedColumns { get; set; }
    public DbSet<Parent> Parents { get; set; }
    public DbSet<ParentComplex> ParentComplexes { get; set; }
    public DbSet<CompanyOwnedJson> CompanyOwnedJson { get; set; }
    public DbSet<CompanyComplexJson> CompanyComplexJson { get; set; }
}
