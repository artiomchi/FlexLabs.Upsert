INSERT INTO "TestEntity" AS "T" ("ID", "Name", "Status", "Total")
VALUES (@p0, @p1, @p2, @p3)
ON CONFLICT ("ID")
DO UPDATE SET "Status" = ( COALESCE("T"."Status", @p4) )
