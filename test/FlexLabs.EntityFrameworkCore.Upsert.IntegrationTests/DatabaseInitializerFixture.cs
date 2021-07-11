using System;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests
{
    public abstract class DatabaseInitializerFixture
    {
        /* Docker commands for the test containers
        docker run --name flexlabs_upsert_test_postgres -e POSTGRES_USER=testuser -e POSTGRES_PASSWORD=Password12! -e POSTGRES_DB=testuser -p 25432:5432 postgres:alpine
        docker run --name flexlabs_upsert_test_mysql -e MYSQL_ROOT_PASSWORD=Password12! -e MYSQL_USER=testuser -e MYSQL_PASSWORD=Password12! -e MYSQL_DATABASE=testuser -p 23306:3306 mysql
        docker run --name flexlabs_upsert_test_mssql -e ACCEPT_EULA=Y -e SA_PASSWORD=Password12! -p 21433:1433 -d mcr.microsoft.com/mssql/server
        */

        private const string Username = "testuser";
        private const string Password = "Password12!";

        private static readonly string ConnString_InMemory = "Upsert_TestDbContext_Tests";
        private static readonly string ConnString_Sqlite = $"Data Source={Username}.db";

        private static readonly string ConnString_Postgres_GitHub = $"Server=localhost;Port=5432;Database={Username};Username=postgres;Password=root";

        private static readonly string ConnString_Postgres_Docker = $"Server=localhost;Port=25432;Database={Username};Username={Username};Password={Password}";
        private static readonly string ConnString_MySql_Docker = $"Server=localhost;Port=23306;Database={Username};Uid=root;Pwd={Password}";
        private static readonly string ConnString_SqlServer_Docker = $"Server=localhost,21433;User=sa;Password={Password};Initial Catalog=FlexLabsUpsertTests;";

        private static readonly string ConnString_Postgres_AppVeyor = $"Server=localhost;Port=5432;Database={Username};Username=postgres;Password={Password}";
        private static readonly string ConnString_MySql_AppVeyor = $"Server=localhost;Port=3306;Database={Username};Uid=root;Pwd={Password}";
        private static readonly string ConnString_SqlServer_AppVeyor = $"Server=(local)\\SQL2017;Database={Username};User Id=sa;Password={Password}";

        private static readonly string ConnString_SqlServer_LocalDb = $"Server=(localdb)\\MSSqlLocalDB;Integrated Security=SSPI;Initial Catalog=FlexLabsUpsertTests;";

        private readonly IMessageSink _diagnosticMessageSink;
        public DbContextOptions<TestDbContext> DataContextOptions { get; }
        public DbDriver DbDriver { get; }

        public DatabaseInitializerFixture(IMessageSink diagnosticMessageSink, DbDriver driver)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
            DbDriver = driver;

            var connectionString = driver switch
            {
                DbDriver.InMemory => ConnString_InMemory,
                DbDriver.Sqlite => ConnString_Sqlite,
                DbDriver.Postgres when BuildEnvironment.IsAppVeyor => ConnString_Postgres_AppVeyor,
                DbDriver.Postgres when BuildEnvironment.IsGitHub && BuildEnvironment.IsGitHubLocalPostgres => ConnString_Postgres_GitHub,
                DbDriver.Postgres => ConnString_Postgres_Docker,
                DbDriver.MySQL when BuildEnvironment.IsAppVeyor => ConnString_MySql_AppVeyor,
                DbDriver.MySQL => ConnString_MySql_Docker,
                DbDriver.MSSQL when BuildEnvironment.IsAppVeyor => ConnString_SqlServer_AppVeyor,
                DbDriver.MSSQL when BuildEnvironment.IsGitHub || Environment.OSVersion.Platform != PlatformID.Win32NT => ConnString_SqlServer_Docker,
                DbDriver.MSSQL => ConnString_SqlServer_LocalDb,
                _ => throw new ArgumentException("Can't get a connection string for driver " + driver)
            };

            DataContextOptions = GetContextOptions(connectionString, driver);
            var startTime = DateTime.Now;
            var isSuccess = false;
            while (DateTime.Now.Subtract(startTime) < TimeSpan.FromSeconds(200))
            {
                try
                {
                    using var context = new TestDbContext(DataContextOptions);
                    context.Database.EnsureDeleted();
                    context.Database.EnsureCreated();
                    isSuccess = true;
                    break;
                }
                catch (Exception ex)
                {
                    _diagnosticMessageSink.OnMessage(new DiagnosticMessage("Connecting to {0} failed! Error: {1}", driver, ex.GetBaseException().Message));
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }
            }
            if (!isSuccess)
                throw new Exception("Could not connect to the database " + driver);
        }

        private static DbContextOptions<TestDbContext> GetContextOptions(string connectionString, DbDriver driver)
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
#if NET5_0
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
#else
                    options.UseMySql(connectionString);
#endif
                    break;
                case DbDriver.InMemory:
                    options.UseInMemoryDatabase(connectionString);
                    break;
                case DbDriver.Sqlite:
                    options.UseSqlite(connectionString);
                    break;
                default:
                    throw new InvalidOperationException("Invalid database driver: " + driver);
            }
            return options.Options;
        }
    }
}
