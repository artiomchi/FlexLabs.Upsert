using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the Npgsql.EntityFrameworkCore.PostgreSQL provider
    /// </summary>
    public class PostgreSqlUpsertCommandRunner : RelationalUpsertCommandRunner
    {
        public override bool Supports(string name) => name == "Npgsql.EntityFrameworkCore.PostgreSQL";
        protected override string Column(string name) => "\"" + name + "\"";
        protected override string SourcePrefix => "EXCLUDED.";
        protected override string TargetPrefix => "\"T\".";

        protected override string GenerateCommand(IEntityType entityType, int entityCount, ICollection<string> insertColumns, ICollection<string> joinColumns,
            List<(string ColumnName, KnownExpression Value)> updateExpressions)
        {
            var result = new StringBuilder();
            var schema = entityType.Relational().Schema;
            if (schema != null)
                schema = $"\"{schema}\".";
            result.Append($"INSERT INTO {schema}\"{entityType.Relational().TableName}\" AS \"T\" (");
            result.Append(string.Join(", ", insertColumns.Select(c => Column(c))));
            result.Append(") VALUES (");
            foreach (var entity in Enumerable.Range(0, entityCount))
            {
                result.Append(string.Join(", ", insertColumns.Select((v, i) => Parameter(i + insertColumns.Count * entity))));
                if (entity < entityCount - 1 && entityCount > 1)
                    result.Append("), (");
            }
            result.Append(") ON CONFLICT (");
            result.Append(string.Join(", ", joinColumns.Select(c => Column(c))));
            result.Append(") DO UPDATE SET ");
            result.Append(string.Join(", ", updateExpressions.Select((e, i) => $"{Column(e.ColumnName)} = {ExpandExpression(e.Value)}")));
            return result.ToString();
        }
    }
}
