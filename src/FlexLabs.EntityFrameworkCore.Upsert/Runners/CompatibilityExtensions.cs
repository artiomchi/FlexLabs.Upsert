#if !NET5_0
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    internal static class CompatibilityExtensions
    {
        internal static string GetColumnBaseName(this IProperty property) => property.GetColumnName();
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
        internal static IEntityType GetTargetTypeCompat(this INavigation navigation) => navigation.TargetEntityType;
    }
}
#endif
