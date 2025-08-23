using System.Linq.Expressions;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

internal record struct MemberBinding(
    string MemberName,
    IKnownValue Value,
    Expression Expression
);
