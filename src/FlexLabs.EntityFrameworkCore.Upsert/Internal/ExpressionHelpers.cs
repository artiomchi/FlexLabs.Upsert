using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    public static class ExpressionHelpers
    {
        /// <summary>
        /// Attempt to get the value of the expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static object GetValue<TSource>(this Expression expression, LambdaExpression container) => GetValue<TSource>(expression, container, false);

        private static object GetValue<TSource>(this Expression expression, LambdaExpression container, bool nested)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    {
                        return ((ConstantExpression)expression).Value;
                    }

                case ExpressionType.MemberAccess:
                    {
                        var memberExp = (MemberExpression)expression;
                        switch (memberExp.Member)
                        {
                            case FieldInfo fInfo:
                                return fInfo.GetValue(memberExp.Expression.GetValue<TSource>(container, true));

                            case PropertyInfo pInfo:
                                if (!nested && typeof(TSource).Equals(memberExp.Expression.Type) && memberExp.Expression is ParameterExpression paramExp)
                                {
                                    var isLeftParam = paramExp.Equals(container.Parameters[0]);
                                    if (isLeftParam || paramExp.Equals(container.Parameters[1]))
                                        return new KnownExpression(expression.NodeType, new ParameterProperty(pInfo.Name, isLeftParam));
                                }
                                return pInfo.GetValue(memberExp.Expression.GetValue<TSource>(container, true));

                            default: throw new Exception("can't handle this type of member expression: " + memberExp.GetType() + ", " + memberExp.Member.GetType());
                        }
                    }

                case ExpressionType.NewArrayInit:
                    {
                        var arrayExp = (NewArrayExpression)expression;
                        var result = Array.CreateInstance(arrayExp.Type.GetElementType(), arrayExp.Expressions.Count);
                        for (int i = 0; i < arrayExp.Expressions.Count; i++)
                            result.SetValue(arrayExp.Expressions[i].GetValue<TSource>(container, true), i);
                        return result;
                    }

                case ExpressionType.Call:
                    {
                        var methodExp = (MethodCallExpression)expression;
                        var context = methodExp.Object?.GetValue<TSource>(container, true);
                        var arguments = methodExp.Arguments.Select(a => a.GetValue<TSource>(container, true)).ToArray();
                        return methodExp.Method.Invoke(context, arguments);
                    }

                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                    {
                        var exp = (BinaryExpression)expression;
                        if (!nested && exp.Method == null)
                        {
                            IKnownValue getValue(Expression e)
                            {
                                if (e is ConstantExpression constExp)
                                    return new ConstantValue(constExp.Value);
                                if (e is MemberExpression memberExp && memberExp.Expression is ParameterExpression paramExp && memberExp.Member is PropertyInfo)
                                {
                                    var isLeftParam = paramExp.Equals(container.Parameters[0]);
                                    if (isLeftParam || paramExp.Equals(container.Parameters[1]))
                                        return new ParameterProperty(memberExp.Member.Name, isLeftParam);
                                }
                                return null;
                            };

                            var leftArg = getValue(exp.Left);
                            var rightArg = getValue(exp.Right);
                            if (leftArg != null && rightArg != null)
                                return new KnownExpression(exp.NodeType, leftArg, rightArg);
                        }
                        if (exp.Method != null)
                            return exp.Method.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, new[] { exp.Left.GetValue<TSource>(container, true), exp.Right.GetValue<TSource>(container, true) }, CultureInfo.InvariantCulture);

                        throw new NotImplementedException();
                        //return null;
                    }
            }

            // If we can't translate it to a known expression, just get the value
            //return Expression.Lambda<Func<object>>(
            //    Expression.Convert(expression, typeof(object)))
            //        .Compile()();
            throw new NotImplementedException();
        }
    }
}
