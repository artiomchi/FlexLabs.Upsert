using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    public class SqlServerUpsertCommandRunnerTests : RelationalCommandRunnerTestsBase
    {
        protected override RelationalUpsertCommandRunner GetRunner() => new SqlServerUpsertCommandRunner();

        protected override string NoUpdate_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]);";

        protected override string NoUpdate_Multiple_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1), (@p2, @p3) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]);";

        protected override string NoUpdate_WithNullable_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID1] = [S].[ID1] AND (([S].[ID2] IS NULL AND [T].[ID2] IS NULL) OR ([S].[ID2] IS NOT NULL AND [T].[ID2] = [S].[ID2])) " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]);";

        protected override string Update_Constant_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]) " +
            "WHEN MATCHED THEN UPDATE SET [Name] = @p2;";

        protected override string Update_Constant_Multiple_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1), (@p2, @p3) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]) " +
            "WHEN MATCHED THEN UPDATE SET [Name] = @p4;";

        protected override string Update_Source_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]) " +
            "WHEN MATCHED THEN UPDATE SET [Name] = [S].[Name];";

        protected override string Update_Source_RenamedCol_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]) " +
            "WHEN MATCHED THEN UPDATE SET [Name] = [S].[Name2];";

        protected override string Update_BinaryAdd_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]) " +
            "WHEN MATCHED THEN UPDATE SET [Status] = ( [T].[Status] + @p2 );";

        protected override string Update_Coalesce_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]) " +
            "WHEN MATCHED THEN UPDATE SET [Status] = ( COALESCE([T].[Status], @p2) );";

        protected override string Update_BinaryAddMultiply_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]) " +
            "WHEN MATCHED THEN UPDATE SET [Status] = ( ( [T].[Status] + @p2 ) * [S].[Status] );";

        protected override string Update_BinaryAddMultiplyGroup_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]) " +
            "WHEN MATCHED THEN UPDATE SET [Status] = ( [T].[Status] + ( @p2 * [S].[Status] ) );";

        protected override string Update_Condition_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]) " +
            "WHEN MATCHED AND [T].[Counter] > @p3 THEN UPDATE SET [Name] = @p2;";

        protected override string Update_Condition_NullCheck_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]) " +
            "WHEN MATCHED AND [T].[Counter] IS NOT NULL THEN UPDATE SET [Name] = @p2;";
    }
}
