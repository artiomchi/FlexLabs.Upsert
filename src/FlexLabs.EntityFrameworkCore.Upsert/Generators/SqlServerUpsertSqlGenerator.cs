using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Generators
{
    public class SqlServerUpsertSqlGenerator : IUpsertSqlGenerator
    {
        public string GenerateCommand(IEntityType entityType, ICollection<string> insertColumns, ICollection<string> joinColumns, ICollection<string> updateColumns, List<(string ColumnName, KnownExpressions Value)> updateExpressions)
        {
            var result = new StringBuilder();
            result.Append($"MERGE INTO {entityType.Relational().Schema ?? "dbo"}.[{entityType.Relational().TableName}] AS [T] USING ( VALUES (");
            result.Append(string.Join(", ", insertColumns.Select((v, i) => $"@p{i}")));
            result.Append($") ) AS [S] (");
            result.Append(string.Join(", ", insertColumns.Select(c => $"[{c}]")));
            result.Append(") ON ");
            result.Append(string.Join(" AND ", joinColumns.Select(c => $"[T].[{c}] = [S].[{c}]")));
            result.Append(" WHEN NOT MATCHED BY TARGET THEN INSERT (");
            result.Append(string.Join(", ", insertColumns.Select(c => $"[{c}]")));
            result.Append(") VALUES (");
            result.Append(string.Join(", ", insertColumns.Select(c => $"[{c}]")));
            result.Append(") WHEN MATCHED THEN UPDATE SET ");
            result.Append(string.Join(", ", updateColumns.Select((c, i) => $"[{c}] = @p{i + insertColumns.Count}")));
            if (updateExpressions.Count > 0)
            {
                if (updateColumns.Count > 0)
                    result.Append(", ");
                var argumentOffset = insertColumns.Count + updateColumns.Count;
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

        public bool Supports(string name) => name == "Microsoft.EntityFrameworkCore.SqlServer";
    }
}
