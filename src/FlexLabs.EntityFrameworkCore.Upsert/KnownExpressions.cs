using System;
using System.Linq.Expressions;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    public class KnownExpressions
    {
        public KnownExpressions(Type sourceType, string sourceProperty, ExpressionType expressionType, object value)
        {
            SourceType = sourceType;
            SourceProperty = sourceProperty;
            ExpressionType = expressionType;
            Value = value;
        }

        public Type SourceType { get; }
        public string SourceProperty { get; }
        public ExpressionType ExpressionType { get; }
        public object Value { get; }
    }
}
