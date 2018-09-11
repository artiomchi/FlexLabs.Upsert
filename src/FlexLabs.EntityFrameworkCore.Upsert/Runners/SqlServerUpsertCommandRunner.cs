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
        protected override string SourcePrefix => "[S].";
        protected override string TargetPrefix => "[T].";

        protected override string GenerateCommand(IEntityType entityType, ICollection<IEnumerable<(string ColumnName, ConstantValue Value)>> entities, ICollection<string> joinColumns,
            List<(string ColumnName, KnownExpression Value)> updateExpressions)
        {
            var result = new StringBuilder();
            var schema = entityType.Relational().Schema;
            if (schema != null)
                schema = $"[{schema}].";
            result.Append($"MERGE INTO {schema}[{entityType.Relational().TableName}] WITH (HOLDLOCK) AS [T] USING ( VALUES (");
            var firstPassed = false;
            foreach (var entity in entities)
            {
                if (firstPassed)
                    result.Append("), (");
                result.Append(string.Join(", ", entity.Select(e => Parameter(e.Value.ArgumentIndex))));
                firstPassed = true;
            }
            result.Append($") ) AS [S] (");
            result.Append(string.Join(", ", entities.First().Select(e => Column(e.ColumnName))));
            result.Append(") ON ");
            result.Append(string.Join(" AND ", joinColumns.Select(c => $"[T].[{c}] = [S].[{c}]")));
            result.Append(" WHEN NOT MATCHED BY TARGET THEN INSERT (");
            result.Append(string.Join(", ", entities.First().Select(e => Column(e.ColumnName))));
            result.Append(") VALUES (");
            result.Append(string.Join(", ", entities.First().Select(e => Column(e.ColumnName))));
            result.Append(") WHEN MATCHED THEN UPDATE SET ");
            result.Append(string.Join(", ", updateExpressions.Select((e, i) => $"{Column(e.ColumnName)} = {ExpandExpression(e.Value)}")));
            result.Append(";");
            return result.ToString();
        }
    }
}
