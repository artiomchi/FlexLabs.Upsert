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
                    try
                    {
                        // If we are on Windows platform, we can copy Sqlite 3.24.0 binary to the output directory.
                        // The dynamic libraries in the current execution path will load first.
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            File.Copy(Environment.Is64BitProcess ? "sqlite3_x64.dll" : "sqlite3_x86.dll", "sqlite3.dll", true);
                        }
                        // The SQLitePCL.SQLite3Provider_sqlite3 provider is in the SQLitePCLRaw.provider.sqlite3.netstandard11 package.
                        // Don't worry, this package is an official package that provides SQLite3Provider_sqlite3
                        // which load external sqlite3 standard dynamic library instead of the embeded old ones.
                        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
                        // Stop other packages from loading embeded sqlite3 library.
                        SQLitePCL.raw.FreezeProvider();
                    }
                    catch
                    {
                        // ignored
                        // Swollow all exceptions, in these cases, we will fall back to embeded ones.
                        // For example, on mobile platforms it is not quite easy to ship a custom sqlite3 build,
                        // although the SQLite.Raw also provides a package named SQLite3Provider_custom_sqlite3,
                        // which is not in active development.
                    }

                    // For debugging purpose, we want to see which sqlite3 version we are using.
                    var v = SQLitePCL.raw.sqlite3_libversion();
                    Console.WriteLine($"Currently using Sqlite v{v}");
                    
                    options.UseSqlite(connectionString);
                    break;
                default:
                    throw new InvalidOperationException("Invalid database driver: " + driver);
            }
            return options.Options;
        }
    }
}
