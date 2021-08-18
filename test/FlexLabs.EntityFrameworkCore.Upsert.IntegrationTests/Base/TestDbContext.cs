using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var dbProvider = this.GetService<IDatabaseProvider>();

            modelBuilder.Entity<TestEntity>().HasIndex(b => b.Num1).IsUnique();
            modelBuilder.Entity<TestEntity>().Property(e => e.Num2).HasDefaultValue(27);
            modelBuilder.Entity<Book>().HasIndex(b => b.Name).IsUnique();
            modelBuilder.Entity<Book>().Property(b => b.Genres)
                .HasConversion(g => string.Join(",", g), s => s.Split(new[] { ',' }));
            modelBuilder.Entity<Book>().Property<DateTime?>("NonMappedColumn");
            modelBuilder.Entity<Country>().HasIndex(c => c.ISO).IsUnique();
            modelBuilder.Entity<DashTable>().HasIndex(t => t.DataSet).IsUnique();
            modelBuilder.Entity<JObjectData>()
                .Property(d => d.Data)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<JObject>(v));
            modelBuilder.Entity<PageVisit>().HasIndex(pv => new { pv.UserID, pv.Date }).IsUnique();
            modelBuilder.Entity<SchemaTable>().HasIndex(t => t.Name).IsUnique();
            modelBuilder.Entity<KeyOnly>().HasKey(t => new { t.ID1, t.ID2 });
            modelBuilder.Entity<NullableCompositeKey>().HasIndex(t => new { t.ID1, t.ID2 }).IsUnique().HasFilter(null);

            if (dbProvider.Name == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                modelBuilder.Entity<JsonData>().Property(j => j.Data).HasColumnType("jsonb");
                modelBuilder.Entity<JsonData>().Property(j => j.Child).HasColumnType("jsonb");

                modelBuilder.Entity<GeneratedAlwaysAsIdentity>().HasIndex(b => b.Num1).IsUnique();
                modelBuilder.Entity<GeneratedAlwaysAsIdentity>().Property(e => e.Num2).UseIdentityAlwaysColumn();
            }
            else
            {
                modelBuilder.Entity<JsonData>().Ignore(j => j.Child);
            }
            if (dbProvider.Name != "Pomelo.EntityFrameworkCore.MySql") // Can't have a default value on TEXT columns in MySql
                modelBuilder.Entity<NullableRequired>().Property(e => e.Text).HasDefaultValue("B");
            if (dbProvider.Name == "Pomelo.EntityFrameworkCore.MySql") // Can't have table schemas in MySql
                modelBuilder.Entity<SchemaTable>().Metadata.SetSchema(null);
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<DashTable> DashTable { get; set; }
        public DbSet<GuidKey> GuidKeys { get; set; }
        public DbSet<GuidKeyAutoGen> GuidKeysAutoGen { get; set; }
        public DbSet<JObjectData> JObjectDatas { get; set; }
        public DbSet<JsonData> JsonDatas { get; set; }
        public DbSet<KeyOnly> KeyOnlies { get; set; }
        public DbSet<NullableCompositeKey> NullableCompositeKeys { get; set; }
        public DbSet<NullableRequired> NullableRequireds { get; set; }
        public DbSet<PageVisit> PageVisits { get; set; }
        public DbSet<SchemaTable> SchemaTable { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<StringKey> StringKeys { get; set; }
        public DbSet<StringKeyAutoGen> StringKeysAutoGen { get; set; }
        public DbSet<TestEntity> TestEntities { get; set; }
        public DbSet<GeneratedAlwaysAsIdentity> GeneratedAlwaysAsIdentity { get; set; }
    }
}
