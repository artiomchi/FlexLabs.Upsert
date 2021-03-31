#if !NET5_0
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    internal static class CompatibilityExtensions
    {
        internal static string GetColumnNameCompat(this IProperty property) => property.GetColumnName();
        internal static IEntityType GetTargetTypeCompat(this INavigation navigation) => navigation.GetTargetType();
    }
}
#elif NET5_0
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    internal static class CompatibilityExtensions
    {
        internal static string GetColumnNameCompat(this IProperty property)
        {
            // NOTE: The new "GetColumnBaseName" method gives not proper relational column-name for owned-entities.
            // This prevents any "obsolete" calls.

            var annotation = property.FindAnnotation(RelationalAnnotationNames.ColumnName);
            if (annotation != null)
            {
                return (string)annotation.Value;
            }

            var table = StoreObjectIdentifier.Create(property.DeclaringEntityType, StoreObjectType.Table);
            return table == null ? property.GetDefaultColumnBaseName() : property.GetDefaultColumnName(table.Value);
        }
        internal static IEntityType GetTargetTypeCompat(this INavigation navigation) => navigation.TargetEntityType;
    }
}
#endif
