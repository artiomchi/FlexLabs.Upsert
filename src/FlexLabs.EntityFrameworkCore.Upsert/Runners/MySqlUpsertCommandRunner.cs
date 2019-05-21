using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the MySql.Data.EntityFrameworkCore or the Pomelo.EntityFrameworkCore.MySql providers
    /// </summary>
    public class MySqlUpsertCommandRunner : RelationalUpsertCommandRunner
    {
        /// <inheritdoc/>
        public override bool Supports(string name) => name == "MySql.Data.EntityFrameworkCore" || name == "Pomelo.EntityFrameworkCore.MySql";
        /// <inheritdoc/>
        protected override string EscapeName(string name) => "`" + name + "`";
        /// <inheritdoc/>
        protected override string SourcePrefix => "VALUES(";
        /// <inheritdoc/>
        protected override string SourceSuffix => ")";
        /// <inheritdoc/>
        protected override string TargetPrefix => null;

        /// <inheritdoc/>
        public override string GenerateCommand(string tableName, ICollection<ICollection<(string ColumnName, ConstantValue Value)>> entities,
            ICollection<(string ColumnName, bool IsNullable)> joinColumns, ICollection<(string ColumnName, IKnownValue Value)> updateExpressions,
            KnownExpression updateCondition)
        {
            if (updateCondition != null)
                throw UnsupportedExpressionException.MySQLConditionalUpdate();

            var result = new StringBuilder("INSERT ");
            if (updateExpressions == null)
                result.Append("IGNORE ");
            result.Append($"INTO {tableName} (");
            result.Append(string.Join(", ", entities.First().Select(e => EscapeName(e.ColumnName))));
            result.Append(") VALUES (");
            result.Append(string.Join("), (", entities.Select(ec => string.Join(", ", ec.Select(e => Parameter(e.Value.ArgumentIndex))))));
            result.Append(")");
            if (updateExpressions != null)
            {
                result.Append(" ON DUPLICATE KEY UPDATE ");
                result.Append(string.Join(", ", updateExpressions
                    .Select((e, i) => updateCondition != null
                        ? $"{EscapeName(e.ColumnName)} = IF ({ExpandExpression(updateCondition)}, {ExpandValue(e.Value)}, {EscapeName(e.ColumnName)})"
                        : $"{EscapeName(e.ColumnName)} = {ExpandValue(e.Value)}")));
            }
            return result.ToString();
        }
    }
}
