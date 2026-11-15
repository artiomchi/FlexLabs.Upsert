MERGE INTO [TestEntity] WITH (HOLDLOCK) AS [T]
USING ( VALUES (@p0, @p1, @p2, @p3) ) AS [S] ([ID], [Name], [Status], [Total])
ON [T].[ID] = [S].[ID]
WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [Name], [Status], [Total])
VALUES ([ID], [Name], [Status], [Total])
WHEN MATCHED AND ( [T].[Total] > @p5 ) AND ( [T].[Status] != [S].[Status] ) THEN UPDATE SET [Name] = @p4;
