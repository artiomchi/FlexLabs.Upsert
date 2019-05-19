using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    /// <summary>
    /// A class that represents a known type of expression
    /// </summary>
    public class KnownExpression : IKnownValue
    {
        /// <summary>
        /// Initialises a new instance of the class
        /// </summary>
        /// <param name="expressionType">The type of the operation being executed</param>
        /// <param name="value">The value used in the expression</param>
        public KnownExpression(ExpressionType expressionType, IKnownValue value)
        {
            ExpressionType = expressionType;
            Value1 = value;
        }

        /// <summary>
        /// Initialises a new instance of the class
        /// </summary>
        /// <param name="expressionType">The type of the operation being executed</param>
        /// <param name="value1">The value used in the expression</param>
        /// <param name="value2">The value used in the expression</param>
        public KnownExpression(ExpressionType expressionType, IKnownValue value1, IKnownValue value2)
        {
            ExpressionType = expressionType;
            Value1 = value1;
            Value2 = value2;
        }

        /// <summary>
        /// Initialises a new instance of the class
        /// </summary>
        /// <param name="expressionType">The type of the operation being executed</param>
        /// <param name="value1">The value used in the expression</param>
        /// <param name="value2">The value used in the expression</param>
        /// <param name="value3">The value used in the expression</param>
        public KnownExpression(ExpressionType expressionType, IKnownValue value1, IKnownValue value2, IKnownValue value3)
        {
            ExpressionType = expressionType;
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
        }

        /// <summary>
        /// The type of the operation being executed
        /// </summary>
        public ExpressionType ExpressionType { get; }

        /// <summary>
        /// The value used in the expression
        /// </summary>
        public IKnownValue Value1 { get; }

        /// <summary>
        /// The value used in the expression
        /// </summary>
        public IKnownValue Value2 { get; }

        /// <summary>
        /// The value used in the expression
        /// </summary>
        public IKnownValue Value3 { get; }

        private IEnumerable<IKnownValue> GetValues()
        {
            yield return Value1;
            yield return Value2;
            yield return Value3;
        }

        /// <inheritdoc/>
        public IEnumerable<ConstantValue> GetConstantValues() => GetValues().Where(v => v != null).SelectMany(v => v.GetConstantValues());

        /// <inheritdoc/>
        public IEnumerable<PropertyValue> GetPropertyValues() => GetValues().Where(v => v != null).SelectMany(v => v.GetPropertyValues());
    }
}
