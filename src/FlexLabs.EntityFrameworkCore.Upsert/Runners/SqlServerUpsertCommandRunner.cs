using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the Microsoft.EntityFrameworkCore.SqlServer provider
    /// </summary>
    public class SqlServerUpsertCommandRunner : RelationalUpsertCommandRunner
    {
        public override bool Supports(string name) => name == "Microsoft.EntityFrameworkCore.SqlServer";
        protected override string Column(string name) => "[" + name + "]";
        protected override string Parameter(int index) => "@p" + index;
        protected override string SourcePrefix => "[S].";
        protected override string TargetPrefix => "[T].";

        protected override string GenerateCommand(IEntityType entityType, int entityCount, ICollection<string> insertColumns, ICollection<string> joinColumns,
            ICollection<string> updateColumns, List<(string ColumnName, KnownExpression Value)> updateExpressions)
        {
            var result = new StringBuilder();
            var schema = entityType.Relational().Schema;
            if (schema != null)
                schema = $"[{schema}].";
            result.Append($"MERGE INTO {schema}[{entityType.Relational().TableName}] WITH (HOLDLOCK) AS [T] USING ( VALUES (");
            foreach (var entity in Enumerable.Range(0, entityCount))
            {
                result.Append(string.Join(", ", insertColumns.Select((v, i) => Parameter(i + insertColumns.Count * entity))));
                if (entity < entityCount - 1 && entityCount > 1)
                    result.Append("), (");
            }
            result.Append($") ) AS [S] (");
            result.Append(string.Join(", ", insertColumns.Select(c => Column(c))));
            result.Append(") ON ");
            result.Append(string.Join(" AND ", joinColumns.Select(c => $"[T].[{c}] = [S].[{c}]")));
            result.Append(" WHEN NOT MATCHED BY TARGET THEN INSERT (");
            result.Append(string.Join(", ", insertColumns.Select(c => Column(c))));
            result.Append(") VALUES (");
            result.Append(string.Join(", ", insertColumns.Select(c => Column(c))));
            result.Append(") WHEN MATCHED THEN UPDATE SET ");
            result.Append(string.Join(", ", updateColumns.Select((c, i) => $"{Column(c)} = {Parameter(i + insertColumns.Count * entityCount)}")));
            if (updateExpressions.Count > 0)
            {
                if (updateColumns.Count > 0)
                    result.Append(", ");
                var argumentOffset = insertColumns.Count * entityCount + updateColumns.Count;
                result.Append(string.Join(", ", updateExpressions.Select((e, i) => $"{Column(e.ColumnName)} = {ExpandExpression(i + argumentOffset, e.ColumnName, e.Value)}")));
            }
            result.Append(";");
            return result.ToString();
        }
    }
}
