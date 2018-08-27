using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    public class PostgreSqlUpsertCommandRunner : RelationalUpsertCommandRunner
    {
        public override bool Supports(string name) => name == "Npgsql.EntityFrameworkCore.PostgreSQL";

        public override string GenerateCommand(IEntityType entityType, int entityCount, ICollection<string> insertColumns, ICollection<string> joinColumns, ICollection<string> updateColumns, List<(string ColumnName, KnownExpressions Value)> updateExpressions)
        {
            var result = new StringBuilder();
            var schema = entityType.Relational().Schema;
            if (schema != null)
                schema = $"\"{schema}\".";
            result.Append($"INSERT INTO {schema}\"{entityType.Relational().TableName}\" AS \"T\" (");
            result.Append(string.Join(", ", insertColumns.Select(c => $"\"{c}\"")));
            result.Append(") VALUES (");
            foreach (var entity in Enumerable.Range(0, entityCount))
            {
                result.Append(string.Join(", ", insertColumns.Select((v, i) => $"@p{i + insertColumns.Count * entity}")));
                if (entity < entityCount - 1 && entityCount > 1)
                    result.Append("), (");
            }
            result.Append(") ON CONFLICT (");
            result.Append(string.Join(", ", joinColumns.Select(c => $"\"{c}\"")));
            result.Append(") DO UPDATE SET ");
            result.Append(string.Join(", ", updateColumns.Select((c, i) => $"\"{c}\" = @p{i + insertColumns.Count * entityCount}")));
            if (updateExpressions.Count > 0)
            {
                if (updateColumns.Count > 0)
                    result.Append(", ");
                var argumentOffset = insertColumns.Count * entityCount + updateColumns.Count;
                result.Append(string.Join(", ", updateExpressions.Select((e, i) => ExpandExpression(i + argumentOffset, e.ColumnName, e.Value))));
            }
            return result.ToString();
        }

        private string ExpandExpression(int argumentIndex, string columnName, KnownExpressions expression)
        {
            switch (expression.ExpressionType)
            {
                case System.Linq.Expressions.ExpressionType.Add:
                    return $"\"{columnName}\" = \"T\".\"{columnName}\" + @p{argumentIndex}";
                case System.Linq.Expressions.ExpressionType.Subtract:
                    return $"\"{columnName}\" = \"T\".\"{columnName}\" - @p{argumentIndex}";
                default: throw new NotSupportedException("Don't know how to process operation: " + expression.ExpressionType);
            }
        }
    }
}
