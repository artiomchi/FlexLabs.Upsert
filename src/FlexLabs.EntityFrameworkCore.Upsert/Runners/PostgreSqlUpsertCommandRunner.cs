﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the Npgsql.EntityFrameworkCore.PostgreSQL provider
    /// </summary>
    public class PostgreSqlUpsertCommandRunner : RelationalUpsertCommandRunner
    {
        /// <inheritdoc/>
        public override bool Supports(string providerName) => providerName == "Npgsql.EntityFrameworkCore.PostgreSQL";
        /// <inheritdoc/>
        protected override string EscapeName(string name) => "\"" + name + "\"";
        /// <inheritdoc/>
        protected override string? SourcePrefix => "EXCLUDED.";
        /// <inheritdoc/>
        protected override string? TargetPrefix => "\"T\".";
        /// <inheritdoc/>
        protected override int? MaxQueryParams => 32767;

        /// <inheritdoc/>
        public override string GenerateCommand(string tableName, ICollection<ICollection<(string ColumnName, ConstantValue Value, string DefaultSql, bool AllowInserts)>> entities,
            ICollection<(string ColumnName, bool IsNullable)> joinColumns, ICollection<(string ColumnName, IKnownValue Value)>? updateExpressions,
            KnownExpression? updateCondition)
        {
            var result = new StringBuilder();
            result.Append(CultureInfo.InvariantCulture, $"INSERT INTO {tableName} AS \"T\" (");
            result.Append(string.Join(", ", entities.First().Select(e => EscapeName(e.ColumnName))));
            result.Append(") VALUES (");
            result.Append(string.Join("), (", entities.Select(ec => string.Join(", ", ec.Select(e => e.DefaultSql ?? Parameter(e.Value.ArgumentIndex))))));
            result.Append(") ON CONFLICT (");
            result.Append(string.Join(", ", joinColumns.Select(c => EscapeName(c.ColumnName))));
            result.Append(") DO ");
            if (updateExpressions != null)
            {
                result.Append("UPDATE SET ");
                result.Append(string.Join(", ", updateExpressions.Select((e, i) => $"{EscapeName(e.ColumnName)} = {ExpandValue(e.Value)}")));
                if (updateCondition != null)
                    result.Append(CultureInfo.InvariantCulture, $" WHERE {ExpandExpression(updateCondition)}");
            }
            else
            {
                result.Append("NOTHING");
            }
            return result.ToString();
        }
    }
}
