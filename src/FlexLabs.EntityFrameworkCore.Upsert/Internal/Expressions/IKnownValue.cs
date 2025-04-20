using System.Collections.Generic;


namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions {
    /// <summary>
    /// Value of an expression
    /// </summary>
    public interface IKnownValue {
        /// <summary>
        /// Get all the constants that are part of this value
        /// </summary>
        /// <returns>A set of constants used in this value</returns>
        IEnumerable<ConstantValue> GetConstantValues();

        /// <summary>
        /// Get all the properties that are part of this value
        /// </summary>
        /// <returns>A set of properties used in this value</returns>
        IEnumerable<PropertyValue> GetPropertyValues();
    }
}
