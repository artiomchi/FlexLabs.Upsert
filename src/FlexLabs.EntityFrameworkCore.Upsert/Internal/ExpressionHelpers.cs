using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    /// <summary>
    /// Expression helper classe that is used to deconstruct expression trees
    /// </summary>
    public static class ExpressionHelpers
    {
        /// <summary>
        /// Attempt to get the value of the expression
        /// </summary>
        /// <param name="expression">The expression we're processing</param>
        /// <param name="container">The original lambda expression/func that contained this expression</param>
        /// <param name="useExpressionCompiler">Allows enabling the fallback expression compiler</param>
        /// <returns>An</returns>
        public static object GetValue<TSource>(this Expression expression, LambdaExpression container, bool useExpressionCompiler = false)
            => GetValueInternal<TSource>(expression, container, useExpressionCompiler, false);

        private static object GetValueInternal<TSource>(this Expression expression, LambdaExpression container, bool useExpressionCompiler, bool nested)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Call:
                    {
                        var methodExp = (MethodCallExpression)expression;
                        var context = methodExp.Object?.GetValueInternal<TSource>(container, useExpressionCompiler, true);
                        var arguments = methodExp.Arguments.Select(a => a.GetValueInternal<TSource>(container, useExpressionCompiler, true)).ToArray();
                        return methodExp.Method.Invoke(context, arguments);
                    }

                case ExpressionType.Coalesce:
                    {
                        var coalesceExp = (BinaryExpression)expression;
                        var left = coalesceExp.Left.GetValueInternal<TSource>(container, useExpressionCompiler, nested);
                        var right = coalesceExp.Right.GetValueInternal<TSource>(container, useExpressionCompiler, nested);

                        if (left == null)
                            return right;
                        if (!(left is IKnownValue))
                            return left;

                        if (!(left is IKnownValue leftValue))
                            leftValue = new ConstantValue(left);
                        if (!(right is IKnownValue rightValue))
                            rightValue = new ConstantValue(right);

                        return new KnownExpression(expression.NodeType, leftValue, rightValue);
                    }

                case ExpressionType.Constant:
                    {
                        return ((ConstantExpression)expression).Value;
                    }

                case ExpressionType.Convert:
                    {
                        var convertExp = (UnaryExpression)expression;
                        if (!nested)
                            return convertExp.Operand.GetValueInternal<TSource>(container, useExpressionCompiler, nested);

                        var value = convertExp.Operand.GetValueInternal<TSource>(container, useExpressionCompiler, true);
                        return Convert.ChangeType(value, convertExp.Type);
                    }

                case ExpressionType.MemberAccess:
                    {
                        var memberExp = (MemberExpression)expression;
                        switch (memberExp.Member)
                        {
                            case FieldInfo fInfo:
                                return fInfo.GetValue(memberExp.Expression?.GetValueInternal<TSource>(container, useExpressionCompiler, true));

                            case PropertyInfo pInfo:
                                if (!nested &&
                                    memberExp.Expression != null &&
                                    typeof(TSource).Equals(memberExp.Expression.Type) &&
                                    memberExp.Expression is ParameterExpression paramExp)
                                {
                                    var isLeftParam = paramExp.Equals(container.Parameters[0]);
                                    if (isLeftParam || paramExp.Equals(container.Parameters[1]))
                                        return new KnownExpression(expression.NodeType, new ParameterProperty(pInfo.Name, isLeftParam));
                                }
                                return pInfo.GetValue(memberExp.Expression?.GetValueInternal<TSource>(container, useExpressionCompiler, true));

                            default:
                                throw new UnsupportedExpressionException(expression);
                        }
                    }

                case ExpressionType.NewArrayInit:
                    {
                        var arrayExp = (NewArrayExpression)expression;
                        var result = Array.CreateInstance(arrayExp.Type.GetElementType(), arrayExp.Expressions.Count);
                        for (int i = 0; i < arrayExp.Expressions.Count; i++)
                            result.SetValue(arrayExp.Expressions[i].GetValueInternal<TSource>(container, useExpressionCompiler, true), i);
                        return result;
                    }

                case ExpressionType.Add:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.Subtract:
                    {
                    var exp = (BinaryExpression)expression;
                    if (!nested && exp.Method == null)
                    {
                            IKnownValue getValue(Expression e)
                            {
                                switch (e.NodeType)
                                {
                                    case ExpressionType.Constant:
                                        {
                                            return new ConstantValue(((ConstantExpression)e).Value);
                                        }

                                    case ExpressionType.MemberAccess:
                                        {
                                            var memberExp = (MemberExpression)e;
                                            switch (memberExp.Expression.NodeType)
                                            {
                                                case ExpressionType.Constant:
                                                    {
                                                        var constExp = (ConstantExpression)memberExp.Expression;
                                                        switch (memberExp.Member)
                                                        {
                                                            case FieldInfo fInfo:
                                                                return new ConstantValue(fInfo.GetValue(constExp.Value), property: null, memberInfo: fInfo);

                                                            case PropertyInfo pInfo:
                                                                return new ConstantValue(pInfo.GetValue(constExp.Value), property: null, memberInfo: pInfo);
                                                        }
                                                        break;
                                                    }

                                                case ExpressionType.Parameter:
                                                    {
                                                        if (memberExp.Member is PropertyInfo)
                                                        {
                                                            var isLeftParam = memberExp.Expression.Equals(container.Parameters[0]);
                                                            if (isLeftParam || memberExp.Expression.Equals(container.Parameters[1]))
                                                                return new ParameterProperty(memberExp.Member.Name, isLeftParam);
                                                        }
                                                        break;
                                                    }
                                            }
                                            break;
                                        }
                                }
                                return null;
                            };

                            var leftArg = getValue(exp.Left);
                            var rightArg = getValue(exp.Right);
                            if (leftArg != null && rightArg != null)
                                return new KnownExpression(exp.NodeType, leftArg, rightArg);
                        }
                        if (exp.Method != null)
                            return exp.Method.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, new[] { exp.Left.GetValueInternal<TSource>(container, useExpressionCompiler, true), exp.Right.GetValueInternal<TSource>(container, useExpressionCompiler, true) }, CultureInfo.InvariantCulture);

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
