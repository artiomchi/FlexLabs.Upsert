using FlexLabs.EntityFrameworkCore.Upsert.Runners;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners
{
    public class OracleUpsertCommandRunnerTests : RelationalCommandRunnerTestsBase<OracleUpsertCommandRunner>
    {
        public OracleUpsertCommandRunnerTests()
            : base("Oracle.EntityFrameworkCore")
        {
        }

        protected override string NoUpdate_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") ";

        protected override string NoUpdate_Multiple_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual UNION ALL  SELECT :p4 AS \"ID\", :p5 AS \"Name\", :p6 AS \"Status\", :p7 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") ";

        protected override string NoUpdate_WithNullable_Sql =>
            "MERGE INTO \"TestEntityWithNullableKey\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"ID1\", :p2 AS \"ID2\", :p3 AS \"Name\", :p4 AS \"Status\", :p5 AS \"Total\" FROM dual) s ON (t.\"ID1\" = s.\"ID1\" AND t.\"ID2\" = s.\"ID2\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"ID1\", \"ID2\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"ID1\", s.\"ID2\", s.\"Name\", s.\"Status\", s.\"Total\") ";

        protected override string Update_Constant_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") WHEN MATCHED THEN UPDATE SET t.\"Name\" = :p4";

        protected override string Update_Constant_Multiple_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual UNION ALL  SELECT :p4 AS \"ID\", :p5 AS \"Name\", :p6 AS \"Status\", :p7 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") WHEN MATCHED THEN UPDATE SET t.\"Name\" = :p8";

        protected override string Update_Source_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") WHEN MATCHED THEN UPDATE SET t.\"Name\" = s.\"Name\"";

        protected override string Update_BinaryAdd_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") WHEN MATCHED THEN UPDATE SET t.\"Total\" = ( t.\"Total\" + :p4 )";

        protected override string Update_Coalesce_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") WHEN MATCHED THEN UPDATE SET t.\"Status\" = ( COALESCE(t.\"Status\", :p4) )";

        protected override string Update_BinaryAddMultiply_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") WHEN MATCHED THEN UPDATE SET t.\"Total\" = ( ( t.\"Total\" + :p4 ) * s.\"Total\" )";

        protected override string Update_BinaryAddMultiplyGroup_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") WHEN MATCHED THEN UPDATE SET t.\"Total\" = ( t.\"Total\" + ( :p4 * s.\"Total\" ) )";

        protected override string Update_Condition_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") WHEN MATCHED THEN UPDATE SET t.\"Name\" = :p4 WHERE t.\"Total\" > :p5 ";

        protected override string Update_Condition_UpdateConditionColumn_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") WHEN MATCHED THEN UPDATE SET t.\"Name\" = :p4, t.\"Total\" = ( t.\"Total\" + :p5 ) WHERE t.\"Total\" > :p6 ";

        protected override string Update_Condition_AndCondition_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") WHEN MATCHED THEN UPDATE SET t.\"Name\" = :p4 WHERE ( t.\"Total\" > :p5 ) AND ( t.\"Status\" != s.\"Status\" ) ";

        protected override string Update_Condition_NullCheck_Sql =>
            "MERGE INTO \"TestEntity\" t USING ( SELECT :p0 AS \"ID\", :p1 AS \"Name\", :p2 AS \"Status\", :p3 AS \"Total\" FROM dual) s ON (t.\"ID\" = s.\"ID\")  WHEN NOT MATCHED THEN INSERT (\"ID\", \"Name\", \"Status\", \"Total\") VALUES (s.\"ID\", s.\"Name\", s.\"Status\", s.\"Total\") WHEN MATCHED THEN UPDATE SET t.\"Name\" = :p4 WHERE t.\"Status\" IS NOT NULL ";
    }
}
