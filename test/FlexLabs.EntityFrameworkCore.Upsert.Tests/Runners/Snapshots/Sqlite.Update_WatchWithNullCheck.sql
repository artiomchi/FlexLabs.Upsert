INSERT INTO "TestEntity" AS "T" ("ID", "Name", "Status", "Total")
VALUES (@p0, @p1, @p2, @p3)
ON CONFLICT ("ID")
DO UPDATE SET "Name" = ( CASE WHEN ( EXCLUDED."Name" IS NULL ) THEN @p4 ELSE EXCLUDED."Name" END )
