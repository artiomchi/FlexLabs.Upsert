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

        protected override string Update_Constant_Sql =>
            "MERGE INTO myTable WITH (HOLDLOCK) AS [T] " +
            "USING ( VALUES (@p0, @p1) ) AS [S] ([Name], [Status]) " +
            "ON [T].[ID] = [S].[ID] " +
            "WHEN NOT MATCHED BY TARGET THEN INSERT ([Name], [Status]) VALUES ([Name], [Status]) " +
            "WHEN MATCHED THEN UPDATE SET [Name] = @p2;";

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
            "WHEN MATCHED THEN UPDATE SET [Status] = [T].[Status] + @p2;";
    }
}
