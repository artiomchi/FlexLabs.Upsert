using System;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    /// <summary>
    /// Thrown when using unsupported expressions in the update clause
    /// See: https://go.flexlabs.org/upsert.expressions
    /// </summary>
    public class UnsupportedExpressionException : Exception
    {
        internal UnsupportedExpressionException(System.Linq.Expressions.Expression expression)
            : base("This type of expression is not currently supported: " + expression + ". Simplify the expression, or try a different one. " +
                  "See " + HelpLinks.SupportedExpressions + " for more details")
        {
            HelpLink = HelpLinks.SupportedExpressions;
        }
    }
}
