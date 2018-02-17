using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    internal static class ExpressionHelpers
    {
        public static Object GetValue(this Expression expression)
        {
            switch (expression)
            {
                case ConstantExpression exp:
                    return exp.Value;

                case MemberExpression exp:
                    switch (exp.Member)
                    {
                        case FieldInfo fInfo:
                            return fInfo.GetValue(exp.Expression.GetValue());

                        case PropertyInfo pInfo:
                            return pInfo.GetValue(exp.Expression.GetValue());

                        default: throw new Exception("can't handle this type of member expression: " + exp.GetType() + ", " + exp.Member.GetType());
                    }

                case BinaryExpression exp:
                    if (exp.Method != null)
                        return exp.Method.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, new[] { exp.Left.GetValue(), exp.Right.GetValue() }, CultureInfo.InvariantCulture);

                    return exp;

                case MethodCallExpression exp:
                    return exp.Method.Invoke(exp.Object.GetValue(), BindingFlags.Public, null, exp.Arguments.Select(a => a.GetValue()).ToArray(), CultureInfo.InvariantCulture);

                default:
                    return expression;
            }
        }

        public static IQueryable<TEntity> WhereEquals<TEntity>(this IQueryable<TEntity> query, string column, Object value)
        {
            var paramExp = Expression.Parameter(typeof(TEntity), "e");
            var propExp = Expression.Property(paramExp, column);
            var matchExp = Expression.Equal(propExp, Expression.Constant(value));
            var result = Expression.Lambda<Func<TEntity, bool>>(matchExp, paramExp);
            return query.Where(result);
        }
    }
}
