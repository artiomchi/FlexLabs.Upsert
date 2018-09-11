namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    public class ConstantValue : IKnownValue
    {
        public ConstantValue(object value)
        {
            Value = value;
        }

        public object Value { get; }
        public int ParameterIndex { get; set; }
    }
}
