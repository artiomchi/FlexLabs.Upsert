using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    /// <summary>
    /// This class represents a constant value from an expression, which will be passed as a command argument
    /// </summary>
    public class ConstantValue : IKnownValue
    {
        /// <summary>
        /// Creates an instance of the ConstantValue class
        /// </summary>
        /// <param name="value">The value used in the expression</param>
        /// <param name="property">The property from which the value is taken</param>
        /// <param name="memberInfo">The memberInfo from which the value is taken</param>
        public ConstantValue(object value, IProperty property = null, MemberInfo memberInfo = null)
        {
            Value = value;
            Property = property;
            MemberInfo = memberInfo;
        }

        /// <summary>
        /// The value used in the expression
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// The property from which the value is taken
        /// </summary>
        public IProperty Property { get; }

        /// <summary>
        /// The memberInfo from which the value is taken
        /// </summary>
        public MemberInfo MemberInfo { get; }

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
            return Array.Empty<PropertyValue>();
        }
    }
}
