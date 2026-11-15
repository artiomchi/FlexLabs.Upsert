MERGE INTO "TestEntity" t
USING (SELECT :p0 AS "ID", :p1 AS "Name", :p2 AS "Status", :p3 AS "Total" FROM dual) s
ON (t."ID" = s."ID")
WHEN NOT MATCHED THEN INSERT ("ID", "Name", "Status", "Total")
VALUES (s."ID", s."Name", s."Status", s."Total")
WHEN MATCHED THEN UPDATE SET t."Name" = :p4
