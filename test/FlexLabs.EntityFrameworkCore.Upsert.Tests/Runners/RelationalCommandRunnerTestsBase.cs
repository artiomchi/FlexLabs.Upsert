using System.Collections.Generic;
using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    public abstract class RelationalCommandRunnerTestsBase
    {
        protected abstract RelationalUpsertCommandRunner GetRunner();

        protected abstract string NoUpdate_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_NoUpdate()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue("status") { ArgumentIndex = 1}, (string)null ),
            };

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID", false) }, null, null);

            Assert.Equal(NoUpdate_Sql, generatedSql);
        }

        protected abstract string NoUpdate_Multiple_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_NoUpdate_Multiple()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)>[] entities = new[]
            {
                new[]
                {
                    ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                    ( "Status", new ConstantValue("status") { ArgumentIndex = 1}, (string)null ),
                },
                new[]
                {
                    ( "Name", new ConstantValue("value") { ArgumentIndex = 2 }, (string)null ),
                    ( "Status", new ConstantValue("status") { ArgumentIndex = 3}, (string)null ),
                },
            };

            var generatedSql = runner.GenerateCommand(tableName, entities, new[] { ("ID", false) }, null, null);

            Assert.Equal(NoUpdate_Multiple_Sql, generatedSql);
        }

        protected abstract string NoUpdate_WithNullable_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_NoUpdate_WithNullable()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue("status") { ArgumentIndex = 1}, (string)null ),
            };

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID1", false), ("ID2", true) }, null, null);

            Assert.Equal(NoUpdate_WithNullable_Sql, generatedSql);
        }

        protected abstract string Update_Constant_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Constant()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue("status") { ArgumentIndex = 1}, (string)null ),
            };
            var updates = new[]
            {
                ("Name", (IKnownValue)new ConstantValue("newValue") { ArgumentIndex = 2 })
            };

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID", false) }, updates, null);

            Assert.Equal(Update_Constant_Sql, generatedSql);
        }

        protected abstract string Update_Constant_Multiple_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Constant_Multiple()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)>[] entities = new[]
            {
                new[]
                {
                    ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                    ( "Status", new ConstantValue("status") { ArgumentIndex = 1}, (string)null ),
                },
                new[]
                {
                    ( "Name", new ConstantValue("value") { ArgumentIndex = 2 }, (string)null ),
                    ( "Status", new ConstantValue("status") { ArgumentIndex = 3}, (string)null ),
                },
            };
            var updates = new[]
            {
                ("Name", (IKnownValue)new ConstantValue("newValue") { ArgumentIndex = 4 })
            };

            var generatedSql = runner.GenerateCommand(tableName, entities, new[] { ("ID", false) }, updates, null);

            Assert.Equal(Update_Constant_Multiple_Sql, generatedSql);
        }

        protected abstract string Update_Source_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Source()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue("status") { ArgumentIndex = 1}, (string)null ),
            };
            var updates = new[]
            {
                ("Name", (IKnownValue)new PropertyValue("Name", false, new MockProperty("Name")))
            };

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID", false) }, updates, null);

            Assert.Equal(Update_Source_Sql, generatedSql);
        }

        protected abstract string Update_Source_RenamedCol_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Source_RenamedCol()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue("status") { ArgumentIndex = 1}, (string)null ),
            };
            var updates = new[]
            {
                ("Name", (IKnownValue)new PropertyValue("Name", false, new MockProperty("Name2")))
            };

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID", false) }, updates, null);

            Assert.Equal(Update_Source_RenamedCol_Sql, generatedSql);
        }

        protected abstract string Update_BinaryAdd_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_BinaryAdd()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue(3) { ArgumentIndex = 1}, (string)null ),
            };
            var updates = new[]
            {
                ("Status", (IKnownValue)new KnownExpression(ExpressionType.Add,
                    new PropertyValue("Status", true, new MockProperty("Status")),
                    new ConstantValue(1) { ArgumentIndex = 2 }))
            };

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID", false) }, updates, null);

            Assert.Equal(Update_BinaryAdd_Sql, generatedSql);
        }

        protected abstract string Update_Coalesce_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Coalesce()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue(3) { ArgumentIndex = 1}, (string)null ),
            };
            var updates = new[]
            {
                ("Status", (IKnownValue)new KnownExpression(ExpressionType.Coalesce,
                    new PropertyValue("Status", true, new MockProperty("Status")),
                    new ConstantValue(1) { ArgumentIndex = 2 }))
            };

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID", false) }, updates, null);

            Assert.Equal(Update_Coalesce_Sql, generatedSql);
        }

        protected abstract string Update_BinaryAddMultiply_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_BinaryAddMultiply()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue(3) { ArgumentIndex = 1}, (string)null ),
            };
            var updates = new[]
            {
                ("Status", (IKnownValue)new KnownExpression(ExpressionType.Multiply,
                    new KnownExpression(ExpressionType.Add,
                        new PropertyValue("Status", true, new MockProperty("Status")),
                        new ConstantValue(1) { ArgumentIndex = 2 }),
                    new PropertyValue("Status", false, new MockProperty("Status"))))
            };

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID", false) }, updates, null);

            Assert.Equal(Update_BinaryAddMultiply_Sql, generatedSql);
        }

        protected abstract string Update_BinaryAddMultiplyGroup_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_BinaryAddMultiplyGroup()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue(3) { ArgumentIndex = 1}, (string)null ),
            };
            var updates = new[]
            {
                ("Status", (IKnownValue)new KnownExpression(ExpressionType.Add,
                    new PropertyValue("Status", true, new MockProperty("Status")),
                    new KnownExpression(ExpressionType.Multiply,
                        new ConstantValue(1) { ArgumentIndex = 2 },
                        new PropertyValue("Status", false, new MockProperty("Status")))))
            };

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID", false) }, updates, null);

            Assert.Equal(Update_BinaryAddMultiplyGroup_Sql, generatedSql);
        }

        protected abstract string Update_Condition_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Condition()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue("status") { ArgumentIndex = 1}, (string)null ),
            };
            var updates = new[]
            {
                ("Name", (IKnownValue)new ConstantValue("newValue") { ArgumentIndex = 2 })
            };
            var condition = new KnownExpression(ExpressionType.GreaterThan, new PropertyValue("Counter", true, new MockProperty("Counter")), new ConstantValue(12) { ArgumentIndex = 3 });

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID", false) }, updates, condition);

            Assert.Equal(Update_Condition_Sql, generatedSql);
        }

        protected abstract string Update_Condition_AndCondition_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Condition_AndCondition()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue("status") { ArgumentIndex = 1}, (string)null ),
            };
            var updates = new[]
            {
                ("Name", (IKnownValue)new ConstantValue("newValue") { ArgumentIndex = 2 })
            };
            var condition = new KnownExpression(ExpressionType.AndAlso,
                new KnownExpression(ExpressionType.GreaterThan, new PropertyValue("Counter", true, new MockProperty("Counter")), new ConstantValue(12) { ArgumentIndex = 3 }),
                new KnownExpression(ExpressionType.NotEqual, new PropertyValue("Status", true, new MockProperty("Status")), new PropertyValue("Status", false, new MockProperty("Status"))));

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID", false) }, updates, condition);

            Assert.Equal(Update_Condition_AndCondition_Sql, generatedSql);
        }

        protected abstract string Update_Condition_NullCheck_Sql { get; }
        [Fact]
        public void SqlSyntaxRunner_Update_Condition_NullCheck()
        {
            var runner = GetRunner();
            var tableName = "myTable";
            ICollection<(string ColumnName, ConstantValue Value, string DefaultSql)> entity = new[]
            {
                ( "Name", new ConstantValue("value") { ArgumentIndex = 0 }, (string)null ),
                ( "Status", new ConstantValue("status") { ArgumentIndex = 1}, (string)null ),
            };
            var updates = new[]
            {
                ("Name", (IKnownValue)new ConstantValue("newValue") { ArgumentIndex = 2 })
            };
            var condition = new KnownExpression(ExpressionType.NotEqual, new PropertyValue("Counter", true, new MockProperty("Counter")), new ConstantValue(null) { ArgumentIndex = 3 });

            var generatedSql = runner.GenerateCommand(tableName, new[] { entity }, new[] { ("ID", false) }, updates, condition);

            Assert.Equal(Update_Condition_NullCheck_Sql, generatedSql);
        }

    }
}
