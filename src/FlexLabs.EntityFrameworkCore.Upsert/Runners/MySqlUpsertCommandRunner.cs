using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Microsoft.EntityFrameworkCore;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the MySql.Data.EntityFrameworkCore or the Pomelo.EntityFrameworkCore.MySql providers
    /// </summary>
    public class MySqlUpsertCommandRunner : RelationalUpsertCommandRunner
    {
        /// <inheritdoc/>
        public override bool Supports(string providerName) => providerName == "MySql.Data.EntityFrameworkCore" || providerName == "Pomelo.EntityFrameworkCore.MySql";
        /// <inheritdoc/>
        protected override string EscapeName(string name) => "`" + name + "`";
        /// <inheritdoc/>
        protected override string? SourcePrefix => "VALUES(";
        /// <inheritdoc/>
        protected override string? SourceSuffix => ")";
        /// <inheritdoc/>
        protected override string? TargetPrefix => null;
        /// <inheritdoc/>
        protected override int? MaxQueryParams => 65535;

        /// <inheritdoc/>
        public override string GenerateCommand(string tableName,
            ICollection<ICollection<(string ColumnName, ConstantValue Value, string DefaultSql, bool AllowInserts)>>
                entities,
            ICollection<(string ColumnName, bool IsNullable)> joinColumns,
            ICollection<(string ColumnName, IKnownValue Value)>? updateExpressions,
            KnownExpression? updateCondition, bool returnResult = false)
        {
            var result = new StringBuilder("INSERT ");
            if (updateExpressions == null)
                result.Append("IGNORE ");
            result.Append(CultureInfo.InvariantCulture, $"INTO {tableName} (");
            result.Append(string.Join(", ", entities.First().Select(e => EscapeName(e.ColumnName))));
            result.Append(") VALUES (");
            result.Append(string.Join("), (", entities.Select(ec => string.Join(", ", ec.Select(e => e.DefaultSql ?? Parameter(e.Value.ArgumentIndex))))));
            result.Append(')');
            if (updateExpressions != null)
            {
                result.Append(" ON DUPLICATE KEY UPDATE ");
                if (updateCondition != null)
                {
                    var columns = updateCondition.GetPropertyValues()
                        .Select(v => v.Property.GetColumnName())
                        .ToArray();

                    var variables = string.Join(", ", updateExpressions
                        .Where(e => columns.Contains(e.ColumnName))
                        .Select(e => $"IF (({Variable(e.ColumnName)} := {EscapeName(e.ColumnName)}) IS NOT NULL, NULL, NULL)"));
                    string expandColumn(string propertyName)
                    {
                        if (updateExpressions.Any(e => e.ColumnName == propertyName))
                            return Variable(propertyName);
                        return TargetPrefix + EscapeName(propertyName) + TargetSuffix;
                    }
                    result.Append(string.Join(", ", updateExpressions
                        .Select((e, i) =>
                        {
                            var valueExpression = $"IF ({ExpandExpression(updateCondition, expandColumn)}, {ExpandValue(e.Value)}, {EscapeName(e.ColumnName)})";
                            return i == 0 && variables.Length > 0
                                ? $"{EscapeName(e.ColumnName)} = COALESCE ({variables}, {valueExpression})"
                                : $"{EscapeName(e.ColumnName)} = {valueExpression}";
                        })));
                }
                else
                {
                    result.Append(string.Join(", ", updateExpressions.Select((e, i) => $"{EscapeName(e.ColumnName)} = {ExpandValue(e.Value)}")));
                }
            }
            return result.ToString();
        }
    }
}
