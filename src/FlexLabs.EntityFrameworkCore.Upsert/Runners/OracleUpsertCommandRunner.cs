using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the Oracle.EntityFrameworkCore provider
    /// </summary>
    public class OracleUpsertCommandRunner : RelationalUpsertCommandRunner
    {
        /// <inheritdoc />
        public override bool Supports(string providerName) => providerName == "Oracle.EntityFrameworkCore";
        /// <inheritdoc />
        protected override string EscapeName([NotNull] string name) => $"\"{name}\"";
        /// <inheritdoc />
        protected override string? SourcePrefix => "s.";
        /// <inheritdoc />
        protected override string? TargetPrefix => "t.";
        /// <inheritdoc />
        protected override string Parameter(int index) => $":p{index}";
        /// <inheritdoc />
        protected override int? MaxQueryParams => 1000;

        /// <inheritdoc />
        public override string GenerateCommand(
            string tableName,
            ICollection<ICollection<(string ColumnName, ConstantValue Value, string DefaultSql, bool AllowInserts)>> entities,
            ICollection<(string ColumnName, bool IsNullable)> joinColumns,
            ICollection<(string ColumnName, IKnownValue Value)>? updateExpressions,
            KnownExpression? updateCondition,
            bool returnResult = false)
        {
            ArgumentNullException.ThrowIfNull(entities);

            if (returnResult)
                throw new NotImplementedException("Oracle runner does not support returning the result of the upsert operation yet");

            var result = new StringBuilder();
            result.Append(CultureInfo.InvariantCulture, $"MERGE INTO {tableName} t USING (");
            foreach (var item in entities.Select((e, ind) => new {e, ind}))
            {
                result.Append("SELECT ");
                result.Append(string.Join(", ", item.e.Select(ec => string.Join(" AS ", ExpandValue(ec.Value), EscapeName(ec.ColumnName)))));
                result.Append(" FROM dual");
                if (entities.Count > 1 && item.ind != entities.Count - 1)
                {
                    result.Append(" UNION ALL ");
                }
            }
            result.Append(") s ON (");
            result.Append(string.Join(" AND ", joinColumns.Select(j => $"t.{EscapeName(j.ColumnName)} = s.{EscapeName(j.ColumnName)}")));
            result.Append(") WHEN NOT MATCHED THEN INSERT (");
            result.Append(string.Join(", ", entities.First().Where(e => e.AllowInserts).Select(e => EscapeName(e.ColumnName))));
            result.Append(") VALUES (");
            result.Append(string.Join(", ", entities.First().Where(e => e.AllowInserts).Select(e => $"s.{EscapeName(e.ColumnName)}")));
            result.Append(')');
            if (updateExpressions is not null)
            {
                result.Append(" WHEN MATCHED THEN UPDATE SET ");
                result.Append(string.Join(", ", updateExpressions.Select(e => $"t.{EscapeName(e.ColumnName)} = {ExpandValue(e.Value)}")));
                if (updateCondition is not null)
                {
                    result.Append(CultureInfo.InvariantCulture, $" WHERE {ExpandExpression(updateCondition)}");
                }
            }

            return result.ToString();
        }

        /// <inheritdoc />
        protected override string ExpandExpression(KnownExpression expression, Func<string, string>? expandLeftColumn = null)
        {
            ArgumentNullException.ThrowIfNull(expression);

            switch (expression.ExpressionType)
            {
                case ExpressionType.And:
                {
                    var left = ExpandValue(expression.Value1, expandLeftColumn);
                    var right = ExpandValue(expression.Value2!, expandLeftColumn);
                    return $"BITAND({left}, {right})";
                }
                case ExpressionType.Or:
                {
                    var left = ExpandValue(expression.Value1, expandLeftColumn);
                    var right = ExpandValue(expression.Value2!, expandLeftColumn);
                    return $"BITOR({left}, {right})";
                }
                case ExpressionType.Modulo:
                {
                    var left = ExpandValue(expression.Value1, expandLeftColumn);
                    var right = ExpandValue(expression.Value2!, expandLeftColumn);
                    return $"MOD({left}, {right})";
                }

                default:
                    return base.ExpandExpression(expression, expandLeftColumn);
            }
        }
    }
}
