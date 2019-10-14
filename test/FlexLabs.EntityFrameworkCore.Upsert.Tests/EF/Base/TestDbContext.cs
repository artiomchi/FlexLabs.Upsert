using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF.Base
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
            modelBuilder.Entity<PageVisit>().HasIndex(pv => new { pv.UserID, pv.Date }).IsUnique();
            modelBuilder.Entity<SchemaTable>().HasIndex(t => t.Name).IsUnique();
            modelBuilder.Entity<KeyOnly>().HasKey(t => new { t.ID1, t.ID2 });
            modelBuilder.Entity<NullableCompositeKey>().HasIndex(t => new { t.ID1, t.ID2 }).IsUnique().HasFilter(null);

            if (dbProvider.Name == "Npgsql.EntityFrameworkCore.PostgreSQL")
                modelBuilder.Entity<JsonData>().Property(j => j.Data).HasColumnType("jsonb");
            if (dbProvider.Name != "Pomelo.EntityFrameworkCore.MySql") // Can't have a default value on TEXT columns in MySql
                modelBuilder.Entity<NullableRequired>().Property(e => e.Text).HasDefaultValue("B");
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<DashTable> DashTable { get; set; }
        public DbSet<GuidKey> GuidKeys { get; set; }
        public DbSet<GuidKeyAutoGen> GuidKeysAutoGen { get; set; }
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

        public enum DbDriver
        {
            Postgres,
            MSSQL,
            MySQL,
            InMemory,
            Sqlite,
        }

        public static DbContextOptions<TestDbContext> Configure(string connectionString, DbDriver driver)
        {
            var options = new DbContextOptionsBuilder<TestDbContext>();
            switch (driver)
            {
                case DbDriver.Postgres:
                    options.UseNpgsql(connectionString);
                    break;
                case DbDriver.MSSQL:
                    options.UseSqlServer(connectionString);
                    break;
                case DbDriver.MySQL:
                    options.UseMySql(connectionString);
                    break;
                case DbDriver.InMemory:
                    options.UseInMemoryDatabase(connectionString);
                    break;
                case DbDriver.Sqlite:
#if !EFCORE3
                    // If we are on Windows platform, we can copy Sqlite 3.24.0 binary to the output directory.
                    // The dynamic libraries in the current execution path will load first.
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        File.Copy(Environment.Is64BitProcess ? "sqlite3_x64.dll" : "sqlite3_x86.dll", "sqlite3.dll", true);
                    }
                    // Using the SQLitePCLRaw.provider.sqlite3.netstandard11 package
                    // which loads the external sqlite3 standard dynamic library instead of the embeded old one.
                    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
                    // Stop other packages from loading embedded sqlite3 library.
                    SQLitePCL.raw.FreezeProvider();

                    // For debugging purpose, we want to see which sqlite3 version we are using.
                    //Console.WriteLine($"Currently using Sqlite v{SQLitePCL.raw.sqlite3_libversion()}");
#endif
                    options.UseSqlite(connectionString);
                    break;
                default:
                    throw new InvalidOperationException("Invalid database driver: " + driver);
            }
            return options.Options;
        }
    }
}
