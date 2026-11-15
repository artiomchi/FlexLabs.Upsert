INSERT INTO "TestEntityWithNullableKey" AS "T" ("ID", "ID1", "ID2", "Name", "Status", "Total")
VALUES (@p0, @p1, @p2, @p3, @p4, @p5)
ON CONFLICT ("ID1", "ID2")
DO NOTHING
