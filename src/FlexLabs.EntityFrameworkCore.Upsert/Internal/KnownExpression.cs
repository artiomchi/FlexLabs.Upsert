using System.Linq.Expressions;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    /// <summary>
    /// A class that represents a known type of expression
    /// </summary>
    public class KnownExpression
    {
        /// <summary>
        /// Initialises a new instance of the class
        /// </summary>
        /// <param name="sourceType">The type of the object that the property is read from</param>
        /// <param name="sourceProperty">The name of the property that is read</param>
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
        /// <param name="sourceType">The type of the object that the property is read from</param>
        /// <param name="sourceProperty">The name of the property that is read</param>
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
    }
}
