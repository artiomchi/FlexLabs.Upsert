namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    public class ExpressionParameterProperty
    {
        public ExpressionParameterProperty(string propertyName, bool isLeftParameter)
        {
            PropertyName = propertyName;
            IsLeftParameter = isLeftParameter;
        }

        public string PropertyName { get; }
        public bool IsLeftParameter { get; }
    }
}
