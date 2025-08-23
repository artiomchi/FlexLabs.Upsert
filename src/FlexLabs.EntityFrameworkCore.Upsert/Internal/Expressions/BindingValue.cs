using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

/// <summary>
/// This class represents property bindings of a MemberInitExpression within an expression
/// </summary>
internal class BindingValue(List<MemberBinding> bindings) : Expression, IKnownValue
{
    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Constant;
    /// <inheritdoc />
    public override Type Type => typeof(ConstantExpression);

    public List<MemberBinding> Bindings { get; } = bindings;

    /// <summary>
    /// Invalid Operation
    /// </summary>
    public IEnumerable<ConstantValue> GetConstantValues() => throw new InvalidOperationException("Not Supported for BindingValue");

    /// <summary>
    /// Invalid Operation
    /// </summary>
    public IEnumerable<PropertyValue> GetPropertyValues() => throw new InvalidOperationException("Not Supported for BindingValue");

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(BindingValue)} ( Count: {Bindings.Count} )";
    }
}
