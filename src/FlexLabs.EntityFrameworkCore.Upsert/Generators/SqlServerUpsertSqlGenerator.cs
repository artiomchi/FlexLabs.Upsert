using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Generators
{
    public class SqlServerUpsertSqlGenerator : IUpsertSqlGenerator
    {
        public string GenerateCommand(IEntityType entityType, ICollection<string> insertColumns, ICollection<string> joinColumns, ICollection<string> updateColumns)
        {
            var result = new StringBuilder();
            result.Append($"MERGE INTO {entityType.Relational().Schema ?? "dbo"}.[{entityType.Relational().TableName}] AS [T] USING ( VALUES (");
            result.AppendJoin(", ", insertColumns.Select((v, i) => $"@p{i}"));
            result.Append($") ) AS [S] (");
            result.AppendJoin(", ", insertColumns.Select(c => $"[{c}]"));
            result.Append(") ON ");
            result.AppendJoin(" AND ", joinColumns.Select(c => $"[T].[{c}] = [S].[{c}]"));
            result.Append(" WHEN NOT MATCHED BY TARGET THEN INSERT (");
            result.AppendJoin(", ", insertColumns.Select(c => $"[{c}]"));
            result.Append(") VALUES (");
            result.AppendJoin(", ", insertColumns.Select(c => $"[{c}]"));
            result.Append(") WHEN MATCHED THEN UPDATE SET ");
            result.AppendJoin(", ", updateColumns.Select((c, i) => $"[{c}] = @p{i + insertColumns.Count}"));
            return result.ToString();
        }

        public bool Supports(string name) => name == "Microsoft.EntifyFrameworkCore.SqlServer";
    }
}
