namespace FlexLabs.EntityFrameworkCore.Upsert.Internal
{
    /// <summary>
    /// This class represents a constant value from an expression, which will be passed as a command argument
    /// </summary>
    public class ConstantValue : IKnownValue
    {
        /// <summary>
        /// Creates an instance of the ConstantValue class
        /// </summary>
        /// <param name="value">The value used in the expression</param>
        public ConstantValue(object value)
        {
            Value = value;
        }

        /// <summary>
        /// The value used in the expression
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// The index of the argument that will be passed to the Db command
        /// </summary>
        public int ArgumentIndex { get; set; }
    }
}
