using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
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

        result.Append($"MERGE INTO {tableName} t USING (");
        result.Append("SELECT ");
        result.Append(string.Join("", string.Join(" ", entities.Select(ec => string.Join(", ",
            ec.Select(e => string.Join(" AS ", ExpandValue(e.Value), EscapeName(e.ColumnName))))))));
        result.Append(" FROM dual ) s ON (");
        result.Append(string.Join(" AND ",
            joinColumns.Select(j => $"t.{EscapeName(j.ColumnName)} = s.{EscapeName(j.ColumnName)}")));
        result.Append(") ");
        result.Append(" WHEN NOT MATCHED THEN INSERT (");
        result.Append(string.Join(", ",
            entities.First().Where(e => e.AllowInserts).Select(e => EscapeName(e.ColumnName))));
        result.Append(") VALUES (");
        result.Append(string.Join(", ",
            entities.First().Where(e => e.AllowInserts).Select(e => ExpandValue(e.Value))));
        result.Append(") ");
        if (updateExpressions is not null)
        {
            result.Append("WHEN MATCHED ");
            if (updateCondition is not null)
            {
                result.Append($" AND {ExpandExpression(updateCondition)} ");
            }

            result.Append("THEN UPDATE SET ");
            result.Append(string.Join(", ",
                updateExpressions.Select(e => $"t.{EscapeName(e.ColumnName)} = {ExpandValue(e.Value)}")));
        }

        return result.ToString();
    }

    /// <inheritdoc />
    protected override string ExpandExpression(KnownExpression expression,
        Func<string, string>? expandLeftColumn = null)
    {
        ArgumentNullException.ThrowIfNull(expression);

        switch (expression.ExpressionType)
        {
            case ExpressionType.Add:
            case ExpressionType.Divide:
            case ExpressionType.Multiply:
            case ExpressionType.Subtract:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            {
                var left = ExpandValue(expression.Value1, expandLeftColumn);
                var right = ExpandValue(expression.Value2!, expandLeftColumn);
                var op = GetSimpleOperator(expression.ExpressionType);
                return $"{left} {op} {right}";
            }
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            {
                var value1Null = expression.Value1 is ConstantValue constant1 && constant1.Value == null;
                var value2Null = expression.Value2 is ConstantValue constant2 && constant2.Value == null;
                if (value1Null || value2Null)
                {
                    return IsNullExpression(value2Null ? expression.Value1! : expression.Value2!,
                        expression.ExpressionType == ExpressionType.NotEqual);
                }

                var left = ExpandValue(expression.Value1, expandLeftColumn);
                var right = ExpandValue(expression.Value2!, expandLeftColumn);
                var op = GetSimpleOperator(expression.ExpressionType);
                return $"{left} {op} {right}";
            }

            case ExpressionType.Coalesce:
            {
                var left = ExpandValue(expression.Value1, expandLeftColumn);
                var right = ExpandValue(expression.Value2!, expandLeftColumn);
                return $"COALESCE({left}, {right})";
            }

            case ExpressionType.Conditional:
            {
                var ifTrue = ExpandValue(expression.Value1, expandLeftColumn);
                var ifFalse = ExpandValue(expression.Value2!, expandLeftColumn);
                var test = ExpandValue(expression.Value3!, expandLeftColumn);
                return $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";
            }

            case ExpressionType.MemberAccess:
            case ExpressionType.Constant:
            {
                return ExpandValue(expression.Value1, expandLeftColumn);
            }

            case ExpressionType.AndAlso:
            case ExpressionType.OrElse:
            {
                var exp = expression.ExpressionType == ExpressionType.AndAlso ? "AND" : "OR";
                var left = ExpandValue(expression.Value1, expandLeftColumn);
                var right = ExpandValue(expression.Value2!, expandLeftColumn);
                return $"{left} {exp} {right}";
            }
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
                throw new NotSupportedException("Don't know how to process operation: " + expression.ExpressionType);
        }
    }

    /// <inheritdoc />
    protected override string GetSimpleOperator(ExpressionType expressionType)
    {
        return expressionType switch
        {
            ExpressionType.Add => "+",
            ExpressionType.Divide => "/",
            ExpressionType.Multiply => "*",
            ExpressionType.Subtract => "-",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "!=",
            _ => throw new InvalidOperationException($"{expressionType} is not a simple arithmetic operation"),
        };
    }

    /// <inheritdoc />
    protected override string EscapeName([NotNull] string name) => $"\"{name.ToUpperInvariant()}\"";

    /// <inheritdoc />
    protected override string? SourcePrefix => "s.";

    /// <inheritdoc />
    protected override string? TargetPrefix => "t.";

    /// <inheritdoc />
    protected override string Parameter(int index) => $":p{index}";
}
