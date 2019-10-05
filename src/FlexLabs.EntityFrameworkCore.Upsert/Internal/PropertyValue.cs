using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    /// <summary>
    /// This class represents access to a property within an expression
    /// </summary>
    public class PropertyValue : IKnownValue
    {
        /// <summary>
        /// Create an instance of the class
        /// </summary>
        /// <param name="propertyName">The property that is accessed in the expression</param>
        /// <param name="isLeftParameter">true if the property belongs to the first parameter to the expression. otherwise false</param>
        public PropertyValue(string propertyName, bool isLeftParameter)
        {
            PropertyName = propertyName;
            IsLeftParameter = isLeftParameter;
        }

        /// <summary>
        /// The property that is accessed in the expression
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// true if the property belongs to the first parameter to the expression. otherwise false
        /// </summary>
        public bool IsLeftParameter { get; }

        /// <summary>
        /// An instance of the model property class, that contains model metadata
        /// </summary>
        public IProperty Property { get; set; }

        /// <inheritdoc/>
        public IEnumerable<ConstantValue> GetConstantValues()
        {
            return Array.Empty<ConstantValue>();
        }

        /// <inheritdoc/>
        public IEnumerable<PropertyValue> GetPropertyValues()
        {
            yield return this;
        }
    }
}
