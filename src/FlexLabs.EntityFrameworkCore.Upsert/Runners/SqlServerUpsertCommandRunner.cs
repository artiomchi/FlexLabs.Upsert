using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the Microsoft.EntityFrameworkCore.SqlServer provider
    /// </summary>
    public class SqlServerUpsertCommandRunner : RelationalUpsertCommandRunner
    {
        public override bool Supports(string name) => name == "Microsoft.EntityFrameworkCore.SqlServer";
        protected override string EscapeName(string name) => "[" + name + "]";
        protected override string SourcePrefix => "[S].";
        protected override string TargetPrefix => "[T].";

        public override string GenerateCommand(string tableName, ICollection<ICollection<(string ColumnName, ConstantValue Value)>> entities, ICollection<string> joinColumns,
            ICollection<(string ColumnName, KnownExpression Value)> updateExpressions)
        {
            var result = new StringBuilder();
            result.Append($"MERGE INTO {tableName} WITH (HOLDLOCK) AS [T] USING ( VALUES (");
            result.Append(string.Join("), (", entities.Select(ec => string.Join(", ", ec.Select(e => Parameter(e.Value.ArgumentIndex))))));
            result.Append($") ) AS [S] (");
            result.Append(string.Join(", ", entities.First().Select(e => EscapeName(e.ColumnName))));
            result.Append(") ON ");
            result.Append(string.Join(" AND ", joinColumns.Select(c => $"[T].[{c}] = [S].[{c}]")));
            result.Append(" WHEN NOT MATCHED BY TARGET THEN INSERT (");
            result.Append(string.Join(", ", entities.First().Select(e => EscapeName(e.ColumnName))));
            result.Append(") VALUES (");
            result.Append(string.Join(", ", entities.First().Select(e => EscapeName(e.ColumnName))));
            result.Append(")");
            if (updateExpressions != null)
            {
                result.Append(" WHEN MATCHED THEN UPDATE SET ");
                result.Append(string.Join(", ", updateExpressions.Select((e, i) => $"{EscapeName(e.ColumnName)} = {ExpandExpression(e.Value)}")));
            }
            result.Append(";");
            return result.ToString();
        }
    }
}
