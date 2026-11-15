INSERT INTO "TestEntity" AS "T" ("ID", "Name", "Status", "Total")
VALUES (@p0, @p1, @p2, @p3), (@p4, @p5, @p6, @p7)
ON CONFLICT ("ID")
DO UPDATE SET "Name" = @p8
