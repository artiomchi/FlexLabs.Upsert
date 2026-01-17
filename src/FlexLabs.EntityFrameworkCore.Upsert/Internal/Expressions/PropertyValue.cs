using System.Linq.Expressions;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

/// <summary>
/// This class represents access to a property within an expression
/// </summary>
public class PropertyValue : Expression, IKnownValue
{
    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Constant;
    /// <inheritdoc />
    public override Type Type => typeof(ConstantExpression);

    /// <summary>
    /// Create an instance of the class
    /// </summary>
    /// <param name="propertyName">The property that is accessed in the expression</param>
    /// <param name="isLeftParameter">true if the property belongs to the first parameter to the expression. otherwise false</param>
    /// <param name="property">Entity Framework model property class, that contains model metadata</param>
    public PropertyValue(string propertyName, bool isLeftParameter, IColumnBase property)
    {
        PropertyName = propertyName;
        IsLeftParameter = isLeftParameter;
        Column = property;
    }

    /// <summary>
    /// The property that is accessed in the expression
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// true if the property belongs to the first parameter to the expression. otherwise false
    /// </summary>
    public bool IsLeftParameter { get; }

    /// <summary>
    /// An instance of the model property class, that contains model metadata
    /// </summary>
    public IColumnBase Column { get; }

    /// <inheritdoc/>
    public IEnumerable<ConstantValue> GetConstantValues()
    {
        return [];
    }

    /// <inheritdoc/>
    public IEnumerable<PropertyValue> GetPropertyValues()
    {
        yield return this;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(PropertyValue)} ( PropertyName: {PropertyName}, Column: {Column.ColumnName}, IsLeftParameter: {IsLeftParameter} )";
    }
}
