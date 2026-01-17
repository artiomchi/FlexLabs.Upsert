using System.Linq.Expressions;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

/// <summary>
/// A class that represents a known type of expression
/// </summary>
public class KnownExpression : Expression, IKnownValue
{
    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Constant;
    /// <inheritdoc />
    public override Type Type => typeof(ConstantExpression);

    /// <summary>
    /// Initialises a new instance of the class
    /// </summary>
    /// <param name="expressionType">The type of the operation being executed</param>
    /// <param name="value">The value used in the expression</param>
    public KnownExpression(ExpressionType expressionType, IKnownValue value)
    {
        ExpressionType = expressionType;
        Value1 = value;
    }

    /// <summary>
    /// Initialises a new instance of the class
    /// </summary>
    /// <param name="expressionType">The type of the operation being executed</param>
    /// <param name="value1">The value used in the expression</param>
    /// <param name="value2">The value used in the expression</param>
    public KnownExpression(ExpressionType expressionType, IKnownValue value1, IKnownValue value2)
    {
        ExpressionType = expressionType;
        Value1 = value1;
        Value2 = value2;
    }

    /// <summary>
    /// Initialises a new instance of the class
    /// </summary>
    /// <param name="expressionType">The type of the operation being executed</param>
    /// <param name="value1">The value used in the expression</param>
    /// <param name="value2">The value used in the expression</param>
    /// <param name="value3">The value used in the expression</param>
    public KnownExpression(ExpressionType expressionType, IKnownValue value1, IKnownValue value2, IKnownValue value3)
    {
        ExpressionType = expressionType;
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
    }

    /// <summary>
    /// The type of the operation being executed
    /// </summary>
    public ExpressionType ExpressionType { get; }

    /// <summary>
    /// The value used in the expression
    /// </summary>
    public IKnownValue Value1 { get; }

    /// <summary>
    /// The value used in the expression
    /// </summary>
    public IKnownValue? Value2 { get; }

    /// <summary>
    /// The value used in the expression
    /// </summary>
    public IKnownValue? Value3 { get; }

    internal IEnumerable<IKnownValue> GetValues()
    {
        if (Value1 != null)
            yield return Value1;
        if (Value2 != null)
            yield return Value2;
        if (Value3 != null)
            yield return Value3;
    }

    /// <inheritdoc/>
    public IEnumerable<ConstantValue> GetConstantValues() => GetValues().Where(v => v != null).SelectMany(v => v.GetConstantValues());

    /// <inheritdoc/>
    public IEnumerable<PropertyValue> GetPropertyValues() => GetValues().Where(v => v != null).SelectMany(v => v.GetPropertyValues());

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(KnownExpression)} ( Op: {ExpressionType}, Value1: {Value1}, Value2: {Value2}, Value3: {Value3} )";
    }
}
