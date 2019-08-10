#if !NETCORE3
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    internal static class ProxyExtensions
    {
        public static string GetSchema(this IEntityType entity)
        {
            if (typeof(IProperty).GetProperty("AfterSaveBehavior") != null)
                return entity.Relational().ColumnName;

            var method = typeof(RelationalEntityTypeExtensions).GetMethod("GetSchema", BindingFlags.Static);
            return (string)method.Invoke(null, new object[] { entity });
        }

        public static string GetTableName(this IEntityType entity)
        {
            if (typeof(IProperty).GetProperty("AfterSaveBehavior") != null)
                return entity.Relational().ColumnName;

            var method = typeof(RelationalEntityTypeExtensions).GetMethod("GetTableName", BindingFlags.Static);
            return (string)method.Invoke(null, new object[] { entity });
        }

        public static PropertySaveBehavior GetAfterSaveBehavior(this IProperty property)
        {
            if (typeof(IProperty).GetProperty("AfterSaveBehavior") != null)
                return property.AfterSaveBehavior;

            var method = typeof(PropertyExtensions).GetMethod("GetAfterSaveBehavior", BindingFlags.Static);
            return (PropertySaveBehavior)method.Invoke(null, new object[] { property });
        }

        public static string GetColumnName(this IProperty property)
        {
            if (typeof(IProperty).GetProperty("AfterSaveBehavior") != null)
                return property.Relational().ColumnName;

            var method = typeof(PropertyExtensions).GetMethod("GetColumnName", BindingFlags.Static);
            return (string)method.Invoke(null, new object[] { property });
        }

        public static string GetDefaultValue(this IProperty property)
        {
            if (typeof(IProperty).GetProperty("AfterSaveBehavior") != null)
                return property.Relational().DefaultValue;

            var method = typeof(PropertyExtensions).GetMethod("DefaultValue", BindingFlags.Static);
            return (string)method.Invoke(null, new object[] { property });
        }

        public static string GetDefaultValueSql(this IProperty property)
        {
            if (typeof(IProperty).GetProperty("AfterSaveBehavior") != null)
                return property.Relational().DefaultValue;

            var method = typeof(PropertyExtensions).GetMethod("DefaultValueSql", BindingFlags.Static);
            return (string)method.Invoke(null, new object[] { property });
        }
    }
}
#endif
