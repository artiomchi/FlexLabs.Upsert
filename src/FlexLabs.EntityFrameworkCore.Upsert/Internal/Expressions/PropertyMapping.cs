namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

internal record struct PropertyMapping(
    IColumnBase Property,
    IKnownValue Value
);
