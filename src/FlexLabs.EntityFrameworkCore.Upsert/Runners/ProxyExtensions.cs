#if !EFCORE3
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    internal static class ProxyExtensions
    {
        public static string GetSchema(this IEntityType entity) => entity.Relational().Schema;
        public static string GetTableName(this IEntityType entity) => entity.Relational().TableName;
        public static PropertySaveBehavior GetAfterSaveBehavior(this IProperty property) => property.AfterSaveBehavior;
        public static string GetColumnName(this IProperty property) => property.Relational().ColumnName;
        public static object GetDefaultValue(this IProperty property) => property.Relational().DefaultValue;
        public static string GetDefaultValueSql(this IProperty property) => property.Relational().DefaultValueSql;
    }
}
#endif
