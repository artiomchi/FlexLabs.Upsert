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
        protected override string EscapeName(string name) => "\"" + name + "\"";
        protected override string SourcePrefix => "EXCLUDED.";
        protected override string TargetPrefix => "\"T\".";

        public override string GenerateCommand(IEntityType entityType, ICollection<ICollection<(string ColumnName, ConstantValue Value)>> entities, ICollection<string> joinColumns,
            List<(string ColumnName, KnownExpression Value)> updateExpressions)
        {
            var result = new StringBuilder();
            result.Append($"INSERT INTO {GetSchema(entityType)}\"{entityType.Relational().TableName}\" AS \"T\" (");
            result.Append(string.Join(", ", entities.First().Select(e => EscapeName(e.ColumnName))));
            result.Append(") VALUES (");
            result.Append(string.Join("), (", entities.Select(ec => string.Join(", ", ec.Select(e => Parameter(e.Value.ArgumentIndex))))));
            result.Append(") ON CONFLICT (");
            result.Append(string.Join(", ", joinColumns.Select(c => EscapeName(c))));
            result.Append(") DO ");
            if (updateExpressions != null)
            {
                result.Append("UPDATE SET ");
                result.Append(string.Join(", ", updateExpressions.Select((e, i) => $"{EscapeName(e.ColumnName)} = {ExpandExpression(e.Value)}")));
            }
            else
            {
                result.Append("NOTHING");
            }
            return result.ToString();
        }
    }
}
