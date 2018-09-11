using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the Microsoft.EntityFrameworkCore.Sqlite provider
    /// </summary>
    public class SqliteUpsertCommandRunner : RelationalUpsertCommandRunner
    {
        public override bool Supports(string name) => name == "Microsoft.EntityFrameworkCore.Sqlite";
        protected override string Column(string name) => "\"" + name + "\"";
        protected override string SourcePrefix => "EXCLUDED.";
        protected override string TargetPrefix => "\"T\".";

        protected override string GenerateCommand(IEntityType entityType, ICollection<ICollection<(string ColumnName, ConstantValue Value)>> entities, ICollection<string> joinColumns,
            List<(string ColumnName, KnownExpression Value)> updateExpressions)
        {
            var result = new StringBuilder();
            result.Append($"INSERT INTO \"{entityType.Relational().TableName}\" AS \"T\" (");
            result.Append(string.Join(", ", entities.First().Select(e => Column(e.ColumnName))));
            result.Append(") VALUES (");
            result.Append(string.Join("), (", entities.Select(ec => string.Join(", ", ec.Select(e => Parameter(e.Value.ArgumentIndex))))));
            result.Append(") ON CONFLICT (");
            result.Append(string.Join(", ", joinColumns.Select(c => Column(c))));
            result.Append(") DO ");
            if (updateExpressions != null)
            {
                result.Append("UPDATE SET ");
                result.Append(string.Join(", ", updateExpressions.Select((e, i) => $"{Column(e.ColumnName)} = {ExpandExpression(e.Value)}")));
            }
            else
            {
                result.Append("NOTHING");
            }
            return result.ToString();
        }
    }
}
