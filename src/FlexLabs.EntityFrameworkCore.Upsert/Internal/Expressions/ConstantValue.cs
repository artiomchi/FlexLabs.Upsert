using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions
{
    /// <summary>
    /// This class represents a constant value from an expression, which will be passed as a command argument
    /// </summary>
    public class ConstantValue : Expression, IKnownValue
    {
        /// <inheritdoc />
        public override ExpressionType NodeType => ExpressionType.Constant;
        /// <inheritdoc />
        public override Type Type => typeof(ConstantExpression);

        /// <summary>
        /// Creates an instance of the ConstantValue class
        /// </summary>
        /// <param name="value">The value used in the expression</param>
        /// <param name="property">The property from which the value is taken</param>
        /// <param name="memberInfo">The memberInfo from which the value is taken</param>
        public ConstantValue(object? value, IColumnBase? property = null, MemberInfo? memberInfo = null)
        {
            Value = value;
            Property = property;
            MemberInfo = memberInfo;
        }

        /// <summary>
        /// The value used in the expression
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// The property from which the value is taken
        /// </summary>
        public new IColumnBase? Property { get; }

        /// <summary>
        /// The memberInfo from which the value is taken
        /// </summary>
        public MemberInfo? MemberInfo { get; }

        /// <summary>
        /// The index of the argument that will be passed to the Db command
        /// </summary>
        public int ArgumentIndex { get; set; }

        /// <inheritdoc/>
        public IEnumerable<ConstantValue> GetConstantValues()
        {
            yield return this;
        }

        /// <inheritdoc/>
        public IEnumerable<PropertyValue> GetPropertyValues()
        {
            return [];
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(ConstantValue)} ( Value: {Value} )";
        }
    }
}
