using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF.Base
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Country>().HasIndex(c => c.ISO).IsUnique();
            modelBuilder.Entity<PageVisit>().HasIndex(pv => new { pv.UserID, pv.Date }).IsUnique();
            modelBuilder.Entity<DashTable>().HasIndex(t => t.DataSet).IsUnique();
            modelBuilder.Entity<SchemaTable>().HasIndex(t => t.Name).IsUnique();
        }

        public DbSet<Country> Countries { get; set; }
        public DbSet<PageVisit> PageVisits { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<DashTable> DashTable { get; set; }
        public DbSet<SchemaTable> SchemaTable { get; set; }

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
                    // If we are on Windows platform, we can copy Sqlite 3.24.0 binary to the output directory.
                    // The dynamic libraries in the current execution path will load first.
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        File.Copy(Environment.Is64BitProcess ? "sqlite3_x64.dll" : "sqlite3_x86.dll", "sqlite3.dll", true);
                    }
                    // Using the SQLitePCLRaw.provider.sqlite3.netstandard11 package
                    // which loads the external sqlite3 standard dynamic library instead of the embeded old one.
                    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
                    // Stop other packages from loading embeded sqlite3 library.
                    SQLitePCL.raw.FreezeProvider();

                    // For debugging purpose, we want to see which sqlite3 version we are using.
                    //Console.WriteLine($"Currently using Sqlite v{SQLitePCL.raw.sqlite3_libversion()}");

                    options.UseSqlite(connectionString);
                    break;
                default:
                    throw new InvalidOperationException("Invalid database driver: " + driver);
            }
            return options.Options;
        }
    }
}
