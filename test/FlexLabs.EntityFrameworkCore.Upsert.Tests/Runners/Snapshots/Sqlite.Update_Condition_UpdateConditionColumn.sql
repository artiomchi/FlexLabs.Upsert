INSERT INTO "TestEntity" AS "T" ("ID", "Name", "Status", "Total")
VALUES (@p0, @p1, @p2, @p3)
ON CONFLICT ("ID")
DO UPDATE SET "Name" = @p4, "Total" = ( "T"."Total" + @p5 )
WHERE "T"."Total" > @p6
