using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    public static class ExpressionHelpers
    {
        /// <summary>
        /// Attempt to get the value of the expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static object GetValue<TSource>(this Expression expression, bool nested = false)
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
                                return fInfo.GetValue(memberExp.Expression.GetValue<TSource>(true));

                            case PropertyInfo pInfo:
                                if (!nested && typeof(TSource).Equals(memberExp.Expression.Type))
                                    return new KnownExpressions(expression.NodeType, pInfo.Name);
                                return pInfo.GetValue(memberExp.Expression.GetValue<TSource>(true));

                            default: throw new Exception("can't handle this type of member expression: " + memberExp.GetType() + ", " + memberExp.Member.GetType());
                        }
                    }

                case ExpressionType.NewArrayInit:
                    {
                        var arrayExp = (NewArrayExpression)expression;
                        var result = Array.CreateInstance(arrayExp.Type.GetElementType(), arrayExp.Expressions.Count);
                        for (int i = 0; i < arrayExp.Expressions.Count; i++)
                            result.SetValue(arrayExp.Expressions[i].GetValue<TSource>(), i);
                        return result;
                    }

                case ExpressionType.Call:
                    {
                        var methodExp = (MethodCallExpression)expression;
                        var context = methodExp.Object?.GetValue<TSource>();
                        var arguments = methodExp.Arguments.Select(a => a.GetValue<TSource>()).ToArray();
                        return methodExp.Method.Invoke(context, arguments);
                    }

                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                    {
                        var exp = (BinaryExpression)expression;
                        if (!nested && exp.Method == null && exp.Right is ConstantExpression constExp && exp.Left is MemberExpression memberExp && memberExp.IsTypeProperty<TSource>())
                            return new KnownExpressions(exp.NodeType, constExp.Value);
                        if (exp.Method != null)
                            return exp.Method.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, new[] { exp.Left.GetValue<TSource>(), exp.Right.GetValue<TSource>() }, CultureInfo.InvariantCulture);

                        return null;
                    }
            }

            // If we can't translate it to a known expression, just get the value
            //return Expression.Lambda<Func<object>>(
            //    Expression.Convert(expression, typeof(object)))
            //        .Compile()();
            throw new NotImplementedException();
        }

        private static bool IsTypeProperty<T>(this MemberExpression expression)
            => typeof(T).Equals(expression.Expression.Type) &&
                expression.Member is PropertyInfo;
    }
}
