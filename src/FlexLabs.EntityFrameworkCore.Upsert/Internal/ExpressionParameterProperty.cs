using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    public class ExpressionParameterProperty : IKnownValue
    {
        public ExpressionParameterProperty(string propertyName, bool isLeftParameter)
        {
            PropertyName = propertyName;
            IsLeftParameter = isLeftParameter;
        }

        public string PropertyName { get; }
        public bool IsLeftParameter { get; }
        public IProperty Property { get; set; }
    }
}
