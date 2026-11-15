MERGE INTO "TestEntityWithNullableKey" t
USING (SELECT :p0 AS "ID", :p1 AS "ID1", :p2 AS "ID2", :p3 AS "Name", :p4 AS "Status", :p5 AS "Total" FROM dual) s
ON (t."ID1" = s."ID1" AND t."ID2" = s."ID2")
WHEN NOT MATCHED THEN INSERT ("ID", "ID1", "ID2", "Name", "Status", "Total")
VALUES (s."ID", s."ID1", s."ID2", s."Name", s."Status", s."Total")
