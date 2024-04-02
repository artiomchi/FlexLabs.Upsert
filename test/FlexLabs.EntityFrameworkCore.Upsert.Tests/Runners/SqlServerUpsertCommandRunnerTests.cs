using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    public class SqlServerUpsertCommandRunnerTests : RelationalCommandRunnerTestsBase<SqlServerUpsertCommandRunner>
    {
        public SqlServerUpsertCommandRunnerTests()
            : base("Microsoft.EntityFrameworkCore.SqlServer")
        { }

        protected override string NoUpdate_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1, @p2, @p3) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]);";

        protected override string NoUpdate_Multiple_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1, @p2, @p3), (@p4, @p5, @p6, @p7) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]);";

        protected override string NoUpdate_WithNullable_Sql =>
            "MERGE INTO [TestEntityWithNullableKey] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1, @p2, @p3, @p4, @p5) ) AS [S] ([ID], [ID1], [ID2], [Name], [Status], [Total]) " +
            "ON [T].[ID1] = [S].[ID1] AND (([S].[ID2] IS NULL AND [T].[ID2] IS NULL) OR ([S].[ID2] IS NOT NULL AND [T].[ID2] = [S].[ID2])) " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [ID1], [ID2], [Name], [Status], [Total]) VALUES ([ID], [ID1], [ID2], [Name], [Status], [Total]);";

        protected override string Update_Constant_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p1, @p2, @p3, @p4) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]) " +
            "WHEN MATCHED THEN UPDATE SET [Name] = @p0;";

        protected override string Update_Constant_Multiple_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p1, @p2, @p3, @p4), (@p5, @p6, @p7, @p8) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]) " +
            "WHEN MATCHED THEN UPDATE SET [Name] = @p0;";

        protected override string Update_Source_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1, @p2, @p3) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]) " +
            "WHEN MATCHED THEN UPDATE SET [Name] = [S].[Name];";

        protected override string Update_BinaryAdd_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p1, @p2, @p3, @p4) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]) " +
            "WHEN MATCHED THEN UPDATE SET [Total] = ( [T].[Total] + @p0 );";

        protected override string Update_Coalesce_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p1, @p2, @p3, @p4) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]) " +
            "WHEN MATCHED THEN UPDATE SET [Status] = ( COALESCE([T].[Status], @p0) );";

        protected override string Update_BinaryAddMultiply_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p1, @p2, @p3, @p4) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]) " +
            "WHEN MATCHED THEN UPDATE SET [Total] = ( ( [T].[Total] + @p0 ) * [S].[Total] );";

        protected override string Update_BinaryAddMultiplyGroup_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p1, @p2, @p3, @p4) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]) " +
            "WHEN MATCHED THEN UPDATE SET [Total] = ( [T].[Total] + ( @p0 * [S].[Total] ) );";

        protected override string Update_Condition_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p2, @p3, @p4, @p5) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]) " +
            "WHEN MATCHED AND [T].[Total] > @p1 THEN UPDATE SET [Name] = @p0;";

        protected override string Update_Condition_UpdateConditionColumn_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p3, @p4, @p5, @p6) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]) " +
            "WHEN MATCHED AND [T].[Total] > @p2 THEN UPDATE SET [Name] = @p0, [Total] = ( [T].[Total] + @p1 );";

        protected override string Update_Condition_AndCondition_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p2, @p3, @p4, @p5) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]) " +
            "WHEN MATCHED AND ( [T].[Total] > @p1 ) AND ( [T].[Status] != [S].[Status] ) THEN UPDATE SET [Name] = @p0;";

        protected override string Update_Condition_NullCheck_Sql =>
            "MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p1, @p2, @p3, @p4) ) AS [S] ([ID], [Name], [Status], [Total]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total]) VALUES ([ID], [Name], [Status], [Total]) " +
            "WHEN MATCHED AND [T].[Status] IS NOT NULL THEN UPDATE SET [Name] = @p0;";
    }
}
