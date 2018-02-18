using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Generators
{
    public class PostgreSQLUpsertSqlGenerator : IUpsertSqlGenerator
    {
        public string GenerateCommand(IEntityType _entityType, ICollection<string> insertColumns, ICollection<string> joinColumns, ICollection<string> updateColumns)
        {
            var result = new StringBuilder();
            result.Append($"INSERT INTO {_entityType.Relational().Schema ?? "public"}.\"{_entityType.Relational().TableName}\" AS \"T\" (");
            result.AppendJoin(", ", insertColumns.Select(c => $"\"{c}\""));
            result.Append(") VALUES (");
            result.AppendJoin(", ", insertColumns.Select((v, i) => $"@p{i}"));
            result.Append(") ON CONFLICT (");
            result.AppendJoin(", ", joinColumns.Select(c => $"\"{c}\""));
            result.Append(") DO UPDATE SET ");
            result.AppendJoin(", ", updateColumns.Select((c, i) => $"\"{c}\" = @p{i + insertColumns.Count}"));
            return result.ToString();
        }

        public bool Supports(string name) => name == "Npgsql.EntityFrameworkCore.PostgreSQL";
    }
}
