MERGE INTO [TestEntityWithNullableKey] WITH (HOLDLOCK) AS [T]
USING ( VALUES (@p0, @p1, @p2, @p3, @p4, @p5) ) AS [S] ([ID], [ID1], [ID2], [Name], [Status], [Total])
ON [T].[ID1] = [S].[ID1] AND (([S].[ID2] IS NULL AND [T].[ID2] IS NULL) OR ([S].[ID2] IS NOT NULL AND [T].[ID2] = [S].[ID2]))
WHEN NOT MATCHED BY TARGET THEN INSERT ([ID], [ID1], [ID2], [Name], [Status], [Total])
VALUES ([ID], [ID1], [ID2], [Name], [Status], [Total]);
