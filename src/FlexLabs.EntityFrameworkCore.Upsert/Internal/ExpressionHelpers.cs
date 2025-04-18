using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    /// <summary>
    /// Expression helper class that is used to deconstruct expression trees
    /// </summary>
    public static class ExpressionHelpers
    {
        /// <summary>
        /// Attempt to get the value of the expression
        /// </summary>
        /// <param name="expression">The expression we're processing</param>
        /// <param name="container">The original lambda expression/func that contained this expression</param>
        /// <param name="propertyFinder">Delegate used to find the EF Property class from a property name</param>
        /// <param name="useExpressionCompiler">Allows enabling the fallback expression compiler</param>
        /// <returns>An</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "This will only be called internally. It's marked as public for the test class")]
        public static object? GetValue<TSource>(this Expression expression, LambdaExpression container, Func<string, IColumnBase?> propertyFinder, bool useExpressionCompiler = false)
            => GetValueInternal<TSource>(expression, container, propertyFinder, useExpressionCompiler, false);

        private static object? GetValueInternal<TSource>(this Expression expression, LambdaExpression container, Func<string, IColumnBase?> propertyFinder, bool useExpressionCompiler, bool nested)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Call:
                    {
                        var methodExp = (MethodCallExpression)expression;
                        var context = methodExp.Object?.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, true);
                        var arguments = methodExp.Arguments.Select(a => a.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, true)).ToArray();
                        return methodExp.Method.Invoke(context, arguments);
                    }

                case ExpressionType.Coalesce:
                    {
                        var coalesceExp = (BinaryExpression)expression;
                        var left = coalesceExp.Left.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, nested);
                        var right = coalesceExp.Right.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, nested);

                        if (left == null)
                            return right;
                        if (left is not IKnownValue)
                            return left;

                        if (left is not IKnownValue leftValue)
                            leftValue = new ConstantValue(left);
                        if (right is not IKnownValue rightValue)
                            rightValue = new ConstantValue(right);

                        return new KnownExpression(expression.NodeType, leftValue, rightValue);
                    }

                case ExpressionType.Conditional:
                    {
                        var conditionalExp = (ConditionalExpression)expression;
                        var ifTrue = conditionalExp.IfTrue.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, nested);
                        var ifFalse = conditionalExp.IfFalse.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, nested);
                        var conditionExp = conditionalExp.Test.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, nested);

                        if (conditionExp is not IKnownValue knownCondition)
                            knownCondition = new ConstantValue(conditionExp);
                        if (ifTrue is not IKnownValue knownTrue)
                            knownTrue = new ConstantValue(ifTrue);
                        if (ifFalse is not IKnownValue knownFalse)
                            knownFalse = new ConstantValue(ifFalse);

                        return new KnownExpression(expression.NodeType, knownTrue, knownFalse, knownCondition);
                    }

                case ExpressionType.Constant:
                    {
                        return ((ConstantExpression)expression).Value;
                    }

                case ExpressionType.Convert:
                    {
                        var convertExp = (UnaryExpression)expression;
                        if (!nested)
                            return convertExp.Operand.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, nested);

                        var value = convertExp.Operand.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, true);
                        return Convert.ChangeType(value, convertExp.Type, CultureInfo.InvariantCulture);
                    }

                case ExpressionType.MemberAccess:
                    {
                        var memberExp = (MemberExpression)expression;
                        switch (memberExp.Member)
                        {
                            case FieldInfo fInfo:
                                return fInfo.GetValue(memberExp.Expression?.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, true));

                            case PropertyInfo pInfo:
                                if (!nested && memberExp.Expression?.NodeType == ExpressionType.Parameter && typeof(TSource).Equals(memberExp.Expression.Type))
                                {
                                    var isLeftParam = memberExp.Expression.Equals(container.Parameters[0]);
                                    if (isLeftParam || memberExp.Expression.Equals(container.Parameters[1]))
                                    {
                                        var property = propertyFinder(pInfo.Name)
                                            ?? throw new InvalidOperationException(Resources.FormatUnknownProperty(pInfo.Name));
                                        return new PropertyValue(pInfo.Name, isLeftParam, property);
                                    }
                                }
                                return pInfo.GetValue(memberExp.Expression?.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, true));

                            default:
                                throw new UnsupportedExpressionException(expression);
                        }
                    }

                case ExpressionType.NewArrayInit:
                    {
                        var arrayExp = (NewArrayExpression)expression;
                        var result = Array.CreateInstance(arrayExp.Type.GetElementType()!, arrayExp.Expressions.Count);
                        for (int i = 0; i < arrayExp.Expressions.Count; i++)
                            result.SetValue(arrayExp.Expressions[i].GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, true), i);
                        return result;
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
                        var exp = (BinaryExpression)expression;
                        if (!nested)
                        {
                            var leftArg = exp.Left.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, false);
                            if (leftArg is not IKnownValue leftArgKnown)
                                if (leftArg is KnownExpression leftArgExp)
                                    leftArgKnown = leftArgExp.Value1;
                                else
                                    leftArgKnown = new ConstantValue(leftArg);
                            var rightArg = exp.Right.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, false);
                            if (rightArg is not IKnownValue rightArgKnown)
                                if (rightArg is KnownExpression rightArgExp)
                                    rightArgKnown = rightArgExp.Value1;
                                else
                                    rightArgKnown = new ConstantValue(rightArg);

                            if (leftArgKnown != null && rightArgKnown != null)
                                return new KnownExpression(exp.NodeType, leftArgKnown, rightArgKnown);
                        }
                        if (exp.Method != null)
                            return exp.Method.Invoke(
                                null,
                                BindingFlags.Static | BindingFlags.Public,
                                null,
                                new[] {
                                    exp.Left.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, true),
                                    exp.Right.GetValueInternal<TSource>(container, propertyFinder, useExpressionCompiler, true)
                                },
                                CultureInfo.InvariantCulture);

                        break;
                    }
            }

            // If we can't translate it to a known expression, just get the value
            if (useExpressionCompiler)
                return Expression.Lambda<Func<object>>(
                    Expression.Convert(expression, typeof(object)))
                        .Compile()();
            throw new UnsupportedExpressionException(expression);
        }
    }
}
