using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// DbContext Entity property details, including the property detadata and the reflection property info
    /// </summary>
    public class ModelProperty
    {
        /// <summary>
        /// Initialises a new instance of the class
        /// </summary>
        /// <param name="propertyInfo">The reflection property info of the model property</param>
        /// <param name="propertyMetadata">The EF property metadata</param>
        public ModelProperty(PropertyInfo propertyInfo, IProperty propertyMetadata)
        {
            PropertyInfo = propertyInfo;
            PropertyMetadata = propertyMetadata;
        }

        /// <summary>
        /// The reflection property info of the model property
        /// </summary>
        public PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// The EF property metadata
        /// </summary>
        public IProperty PropertyMetadata { get; }
    }
}
