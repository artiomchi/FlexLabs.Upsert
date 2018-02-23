using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Generators
{
    public class MySqlUpsertSqlGenerator : IUpsertSqlGenerator
    {
        public string GenerateCommand(IEntityType entityType, ICollection<string> insertColumns, ICollection<string> joinColumns, ICollection<string> updateColumns, List<(string ColumnName, KnownExpressions Value)> updateExpressions)
        {
            var result = new StringBuilder();
            var schema = entityType.Relational().Schema;
            if (schema != null)
                schema = $"`{schema}`.";
            result.Append($"INSERT INTO {schema}`{entityType.Relational().TableName}` (");
            result.Append(string.Join(", ", insertColumns.Select(c => $"`{c}`")));
            result.Append(") VALUES (");
            result.Append(string.Join(", ", insertColumns.Select((v, i) => $"@p{i}")));
            result.Append(") ON DUPLICATE KEY UPDATE ");
            result.Append(string.Join(", ", updateColumns.Select((c, i) => $"`{c}` = @p{i + insertColumns.Count}")));
            if (updateExpressions.Count > 0)
            {
                if (updateColumns.Count > 0)
                    result.Append(", ");
                var argumentOffset = insertColumns.Count + updateColumns.Count;
                result.Append(string.Join(", ", updateExpressions.Select((e, i) => ExpandExpression(i + argumentOffset, e.ColumnName, e.Value))));
            }
            return result.ToString();
        }

        private string ExpandExpression(int argumentIndex, string columnName, KnownExpressions expression)
        {
            switch (expression.ExpressionType)
            {
                case System.Linq.Expressions.ExpressionType.Add:
                    return $"`{columnName}` = `{columnName}` + @p{argumentIndex}";
                case System.Linq.Expressions.ExpressionType.Subtract:
                    return $"`{columnName}` = `{columnName}` - @p{argumentIndex}";
                default: throw new NotSupportedException("Don't know how to process operation: " + expression.ExpressionType);
            }
        }

        public bool Supports(string name) => name == "MySql.Data.EntityFrameworkCore" || name == "Pomelo.EntityFrameworkCore.MySql";
    }
}
