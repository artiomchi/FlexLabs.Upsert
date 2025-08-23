using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

internal class UpdateExpressionVisitor(
    RelationalTableBase table,
    ParameterExpression leftParameter,
    ParameterExpression rigthParameter,
    bool useExpressionCompiler
) : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var context = node.Object != null ? GetValueObject(node.Object) : null;
        var arguments = node.Arguments.Select(GetValueObject).ToArray();
        var result = node.Method.Invoke(context, arguments);

        return new ConstantValue(result);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Coalesce:
                {
                    var left = GetKnownValue(node.Left);
                    var right = GetKnownValue(node.Right);

                    return left switch
                    {
                        // eg. null ?? right
                        ConstantValue { Value: null } => (Expression)right,
                        // eg. left_not_null ?? right
                        ConstantValue { Value: not null } value => value,
                        _ => new KnownExpression(node.NodeType, left, right)
                    };
                }
            case ExpressionType.Add:
            case ExpressionType.Divide:
            case ExpressionType.Modulo:
            case ExpressionType.Multiply:
            case ExpressionType.Subtract:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.AndAlso:
            case ExpressionType.OrElse:
            case ExpressionType.And:
            case ExpressionType.Or:
                {
                    var left = GetKnownValue(node.Left);
                    var right = GetKnownValue(node.Right);

                    if (left is ConstantValue { Value: var l } &&
                        right is ConstantValue { Value: var r } &&
                        node.Method != null)
                    {
                        var value = node.Method.Invoke(
                            null,
                            BindingFlags.Static | BindingFlags.Public,
                            null,
                            parameters: [l, r],
                            culture: CultureInfo.InvariantCulture);
                        return new ConstantValue(value);
                    }

                    return new KnownExpression(node.NodeType, left, right);
                }
        }

        return (Expression)GetValueCompiled(node);
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        var ifTrue = GetKnownValue(node.IfTrue);
        var ifFalse = GetKnownValue(node.IfFalse);
        var condition = GetKnownValue(node.Test);

        return new KnownExpression(node.NodeType, ifTrue, ifFalse, condition);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        return node.Value switch
        {
            IKnownValue x => (Expression)x,
            _ => new ConstantValue(node.Value),
        };
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        var source = GetValueObject(node.Operand);

        // handle: implicit operator method call
        if (node.Method != null)
        {
            var value = node.Method.Invoke(
                null,
                BindingFlags.Static | BindingFlags.Public,
                null,
                parameters: [source],
                culture: CultureInfo.InvariantCulture);
            return new ConstantValue(value);
        }

        // handle eg: int -> int?
        if (node.Type.IsInstanceOfType(source))
        {
            return new ConstantValue(source);
        }

        var result = Convert.ChangeType(source, node.Type, CultureInfo.InvariantCulture);
        return new ConstantValue(result);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == leftParameter)
        {
            return new ParameterValue(isLeftParameter: true);
        }
        else if (node == rigthParameter)
        {
            return new ParameterValue(isLeftParameter: false);
        }

        throw new UnsupportedExpressionException(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var knownValue = node.Expression != null ? GetKnownValue(node.Expression) : null;
        if (knownValue is ParameterValue parameter)
        {
            var column = table.FindColumn(node.Member.Name) ?? throw new InvalidOperationException(Resources.FormatUnknownProperty(node));
            return new PropertyValue(column.Name, parameter.IsLeftParameter, column);
        }
        else if (knownValue is PropertyValue { Column: var owner, IsLeftParameter: var isLeft })
        {
            var validOwner = owner switch
            {
                { Owned: OwnershipType.InlineOwner } => owner,
                { Owned: OwnershipType.Json } json => throw UnsupportedExpressionException.ReadingJsonMemberNotSupported(json, node),
                _ => throw new InvalidOperationException(Resources.FormatUnsupportedPropertyAccess(node)),
            };
            var column = table.FindColumn(validOwner, node.Member.Name) switch
            {
                null => throw new InvalidOperationException(Resources.FormatUnknownProperty(node)),
                { Owned: OwnershipType.Json } json => throw UnsupportedExpressionException.ReadingJsonMemberNotSupported(json, node),
                var x => x,
            };
            return new PropertyValue(column.Name, isLeftParameter: isLeft, column);
        }

        var target = GetValueObject(node.Expression);

        var value = node.Member switch
        {
            FieldInfo f => f.GetValue(target),
            PropertyInfo p => p.GetValue(target),
            _ => throw new UnsupportedExpressionException(node)
        };

        return new ConstantValue(value);
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        var bindings = new List<MemberBinding>();
        foreach (var binding in node.Bindings.Cast<MemberAssignment>())
        {
            var member = binding.Member;
            var value = GetKnownValue(binding.Expression);
            bindings.Add(new MemberBinding(member.Name, value, binding.Expression));
        }

        return new BindingValue(bindings);
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        var result = Array.CreateInstance(node.Type.GetElementType()!, node.Expressions.Count);

        for (var i = 0; i < node.Expressions.Count; i++)
        {
            var value = GetValueObject(node.Expressions[i]);
            result.SetValue(value, i);
        }

        return new ConstantValue(result);
    }

    public bool TryGetValueObject([NotNullWhen(false)] Expression? expression, out object? value)
    {
        if (expression is null)
        {
            value = null;
            return true;
        }

        var knownValue = GetKnownValue(expression);
        if (knownValue is ConstantValue constantValue)
        {
            value = constantValue.Value;
            return true;
        }

        value = null;
        return false;
    }

    public object? GetValueObject(Expression? expression)
    {
        return TryGetValueObject(expression, out var value)
            ? value
            : throw new UnsupportedExpressionException(expression);
    }

    public IKnownValue GetKnownValue(Expression expression)
    {
        var result = Visit(expression);

        return result switch
        {
            IKnownValue x => x,
            _ => GetValueCompiled(result),
        };
    }

    protected IKnownValue GetValueCompiled(Expression expression)
    {
        // TODO check if it is safe to compile - contains no IKnownValue!
        if (useExpressionCompiler)
        {
            var value = Expression.Lambda<Func<object>>(
                Expression.Convert(expression, typeof(object))
            ).Compile()();

            return new ConstantValue(value);
        }

        throw new UnsupportedExpressionException(expression);
    }
}
