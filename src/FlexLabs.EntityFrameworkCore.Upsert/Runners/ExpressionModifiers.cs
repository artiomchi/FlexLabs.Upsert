using System;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Modifier flags that may affect how an expression is translated to SQL
    /// </summary>
    [Flags]
    public enum ExpressionModifiers
    {
        /// <summary>
        /// Replace references to the DB column with a variable referencing this column
        /// </summary>
        LeftPropertyAsVariable = 1,
    }
}
