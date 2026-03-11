using System.Linq.Expressions;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

/// <summary>
/// This class represents property mappings of a MemberInitExpression within an expression
/// </summary>
internal class ParameterValue(bool isLeftParameter) : Expression, IKnownValue
{
    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Constant;
    /// <inheritdoc />
    public override Type Type => typeof(ConstantExpression);

    public bool IsLeftParameter { get; } = isLeftParameter;

    /// <summary>
    /// Invalid Operation
    /// </summary>
    public IEnumerable<ConstantValue> GetConstantValues() => throw new InvalidOperationException(Resources.FormatNotSupportedFor(nameof(ParameterValue)));

    /// <summary>
    /// Invalid Operation
    /// </summary>
    public IEnumerable<PropertyValue> GetPropertyValues() => throw new InvalidOperationException(Resources.FormatNotSupportedFor(nameof(ParameterValue)));

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(ParameterValue)} ( IsLeft: {IsLeftParameter} )";
    }
}
