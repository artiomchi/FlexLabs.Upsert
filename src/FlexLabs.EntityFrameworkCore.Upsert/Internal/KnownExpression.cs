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

        /// <inheritdoc/>
        public IEnumerable<ConstantValue> GetConstantValues()
        {
            if (Value1 != null && Value2 != null)
                return Value1.GetConstantValues().Concat(Value2.GetConstantValues());

            return Value1?.GetConstantValues() ?? Value2?.GetConstantValues() ?? new ConstantValue[0];
        }

        /// <inheritdoc/>
        public IEnumerable<PropertyValue> GetPropertyValues()
        {
            if (Value1 != null && Value2 != null)
                return Value1.GetPropertyValues().Concat(Value2.GetPropertyValues());

            return Value1?.GetPropertyValues() ?? Value2?.GetPropertyValues() ?? new PropertyValue[0];
        }
    }
}
