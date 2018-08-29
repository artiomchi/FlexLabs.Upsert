using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public override string GenerateCommand(IEntityType entityType, int entityCount, ICollection<string> insertColumns, ICollection<string> joinColumns,
            ICollection<string> updateColumns, List<(string ColumnName, KnownExpressions Value)> updateExpressions)
        {
            var result = new StringBuilder();
            var schema = entityType.Relational().Schema;
            if (schema != null)
                schema = $"[{schema}].";
            result.Append($"MERGE INTO {schema}[{entityType.Relational().TableName}] WITH (HOLDLOCK) AS [T] USING ( VALUES (");
            foreach (var entity in Enumerable.Range(0, entityCount))
            {
                result.Append(string.Join(", ", insertColumns.Select((v, i) => $"@p{i + insertColumns.Count * entity}")));
                if (entity < entityCount - 1 && entityCount > 1)
                    result.Append("), (");
            }
            result.Append($") ) AS [S] (");
            result.Append(string.Join(", ", insertColumns.Select(c => $"[{c}]")));
            result.Append(") ON ");
            result.Append(string.Join(" AND ", joinColumns.Select(c => $"[T].[{c}] = [S].[{c}]")));
            result.Append(" WHEN NOT MATCHED BY TARGET THEN INSERT (");
            result.Append(string.Join(", ", insertColumns.Select(c => $"[{c}]")));
            result.Append(") VALUES (");
            result.Append(string.Join(", ", insertColumns.Select(c => $"[{c}]")));
            result.Append(") WHEN MATCHED THEN UPDATE SET ");
            result.Append(string.Join(", ", updateColumns.Select((c, i) => $"[{c}] = @p{i + insertColumns.Count * entityCount}")));
            if (updateExpressions.Count > 0)
            {
                if (updateColumns.Count > 0)
                    result.Append(", ");
                var argumentOffset = insertColumns.Count * entityCount + updateColumns.Count;
                result.Append(string.Join(", ", updateExpressions.Select((e, i) => ExpandExpression(i + argumentOffset, e.ColumnName, e.Value))));
            }
            result.Append(";");
            return result.ToString();
        }

        private string ExpandExpression(int argumentIndex, string columnName, KnownExpressions expression)
        {
            switch (expression.ExpressionType)
            {
                case System.Linq.Expressions.ExpressionType.Add:
                    return $"[{columnName}] = [T].[{columnName}] +  @p{argumentIndex}";
                case System.Linq.Expressions.ExpressionType.Subtract:
                    return $"[{columnName}] = [T].[{columnName}] -  @p{argumentIndex}";
                default: throw new NotSupportedException("Don't know how to process operation: " + expression.ExpressionType);
            }
        }
    }
}
