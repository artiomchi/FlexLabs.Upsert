using System;
using System.Collections.Generic;
using System.Diagnostics;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF.Base;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF
{
    public class BasicTest : IClassFixture<BasicTest.Contexts>
    {
        public class Contexts : IDisposable
        {
            private const string Postgres_ImageName = "flexlabs_upsert_test_postgres";
            private const string Postgres_Port = "25432";
            private static readonly string Postgres_Connection = $"Server=localhost;Port={Postgres_Port};Database={Username};Username={Username};Password={Password}";
            private const string SqlServer_ImageName = "flexlabs_upsert_test_sqlserver";
            private const string SqlServer_Port = "21433";
            private static readonly string SqlServer_Connection = $"Server=localhost,{SqlServer_Port};Database={Username};User Id=sa;Password={Password}";
            private const string MySql_ImageName = "flexlabs_upsert_test_mysql";
            private const string MySql_Port = "23306";
            private static readonly string MySql_Connection = $"Server=localhost;Port={MySql_Port};Database={Username};Uid=root;Pwd={Password}";

            private const string Username = "testuser";
            private const string Password = "P1ssw0rd";

            private IDictionary<TestDbContext.DbDriver, Process> _processes;
            public IDictionary<TestDbContext.DbDriver, TestDbContext> _dataContexts;

            public Contexts()
            {
                _processes = new Dictionary<TestDbContext.DbDriver, Process>();
                _dataContexts = new Dictionary<TestDbContext.DbDriver, TestDbContext>();

                _processes[TestDbContext.DbDriver.Postgres] = Process.Start("docker",
                    $"run --name {Postgres_ImageName} -e POSTGRES_USER={Username} -e POSTGRES_PASSWORD={Password} -e POSTGRES_DB={Username} -p {Postgres_Port}:5432 postgres:alpine");
                _processes[TestDbContext.DbDriver.MSSQL] = Process.Start("docker",
                    $"run --name {SqlServer_ImageName} -e ACCEPT_EULA=Y -e MSSQL_PID=Express -e SA_PASSWORD={Password} -p {SqlServer_Port}:1433 microsoft/mssql-server-linux");
                _processes[TestDbContext.DbDriver.MySQL] = Process.Start("docker",
                    $"run --name {MySql_ImageName} -e MYSQL_ROOT_PASSWORD={Password} -e MYSQL_USER={Username} -e MYSQL_PASSWORD={Password} -e MYSQL_DATABASE={Username} -p {MySql_Port}:3306 mysql");

                WaitForConnection(TestDbContext.DbDriver.Postgres, Postgres_Connection);
                WaitForConnection(TestDbContext.DbDriver.MSSQL, SqlServer_Connection);
                WaitForConnection(TestDbContext.DbDriver.MySQL, MySql_Connection);
            }

            private void WaitForConnection(TestDbContext.DbDriver driver, string connectionString)
            {
                var options = TestDbContext.Configure(connectionString, driver);
                var startTime = DateTime.Now;
                while (DateTime.Now.Subtract(startTime) < TimeSpan.FromSeconds(200))
                {
                    bool isSuccess = false;
                    TestDbContext context = null;
                    Console.WriteLine("Connecting to " + driver);
                    try
                    {
                        context = new TestDbContext(options);
                        context.Database.EnsureCreated();
                        _dataContexts[driver] = context;
                        isSuccess = true;
                        Console.WriteLine(" - Connection Successful!");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(" - EXCEPTION: " + ex.Message);
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    finally
                    {
                        if (!isSuccess)
                            context?.Dispose();
                    }
                }
            }

            public void Dispose()
            {
                foreach (var context in _dataContexts.Values)
                    context.Dispose();
                foreach (var context in _processes.Values)
                    context.Dispose();

                using (var processRm = Process.Start("docker", $"rm -f {Postgres_ImageName}"))
                {
                    processRm.WaitForExit();
                }
                using (var processRm = Process.Start("docker", $"rm -f {SqlServer_ImageName}"))
                {
                    processRm.WaitForExit();
                }
                using (var processRm = Process.Start("docker", $"rm -f {MySql_ImageName}"))
                {
                    processRm.WaitForExit();
                }
            }
        }

        private IDictionary<TestDbContext.DbDriver, TestDbContext> _dataContexts;
        public BasicTest(Contexts contexts)
        {
            _dataContexts = contexts._dataContexts;
        }

        [Theory]
        [InlineData(TestDbContext.DbDriver.Postgres)]
        [InlineData(TestDbContext.DbDriver.MSSQL)]
        [InlineData(TestDbContext.DbDriver.MySQL)]
        public void Test(TestDbContext.DbDriver driver)
        {
            var context = _dataContexts[driver];

            Assert.Empty(context.Countries);
        }
    }
}
