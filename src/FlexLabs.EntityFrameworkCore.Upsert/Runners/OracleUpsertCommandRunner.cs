using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners;

/// <summary>
/// Upsert command runner for the Oracle.EntityFrameworkCore provider
/// </summary>
public class OracleUpsertCommandRunner : RelationalUpsertCommandRunner
{
    /// <inheritdoc />
    public override bool Supports(string providerName) => providerName == "Oracle.EntityFrameworkCore";

    /// <inheritdoc />
    public override string GenerateCommand(string tableName,
        ICollection<ICollection<(string ColumnName, ConstantValue Value, string DefaultSql, bool AllowInserts)>>
            entities, ICollection<(string ColumnName, bool IsNullable)> joinColumns,
        ICollection<(string ColumnName, IKnownValue Value)>? updateExpressions,
        KnownExpression? updateCondition)
    {
        var result = new StringBuilder();

        result.Append($"MERGE INTO {tableName} t USING ( SELECT");
        result.Append(string.Join(", ",
            entities.Select(ec => string.Join(" AS ", ec.Select(e => new List<string> { Parameter(e.Value.ArgumentIndex), e.ColumnName })))));
        result.Append("FROM dual) s");
        result.Append("ON (");
        result.Append(string.Join(" AND ", joinColumns.Select(c => c.IsNullable
            ? $"((s.[{c.ColumnName}] IS NULL AND t.[{c.ColumnName}] IS NULL) OR (s.[{c.ColumnName}] IS NOT NULL AND t.[{c.ColumnName}] = t.[{c.ColumnName}]))"
            : $"t.[{c.ColumnName}] = s.[{c.ColumnName}]")));
        result.Append(" WHEN NOT MATCHED THEN INSERT (");
        result.Append(string.Join(", ",
            entities.First().Where(e => e.AllowInserts).Select(e => EscapeName(e.ColumnName))));
        result.Append(") VALUES (");
        result.Append(string.Join(", ",
            entities.First().Where(e => e.AllowInserts).Select(e => EscapeName(e.ColumnName))));
        result.Append(')');
        if (updateExpressions != null)
        {
            result.Append(" WHEN MATCHED");
            if (updateCondition != null)
                result.Append($" AND {ExpandExpression(updateCondition)}");
            result.Append(" THEN UPDATE SET ");
            result.Append(string.Join(", ",
                updateExpressions.Select((e, i) => $"{EscapeName(e.ColumnName)} = {ExpandValue(e.Value)}")));
        }

        result.Append(';');
        return result.ToString();
    }

    /// <inheritdoc />
    protected override string EscapeName(string name) => "\"" + name + "\"";

    /// <inheritdoc />
    protected override string? SourcePrefix => "s.";

    /// <inheritdoc />
    protected override string? TargetPrefix => "t.";
}
