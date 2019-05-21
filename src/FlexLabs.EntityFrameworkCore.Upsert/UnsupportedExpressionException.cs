using System;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    /// <summary>
    /// Thrown when using unsupported expressions in the update clause
    /// See: https://go.flexlabs.org/upsert.expressions
    /// </summary>
    public class UnsupportedExpressionException : Exception
    {
        private UnsupportedExpressionException(string message, string helpLink = null)
            : base(message)
        {
            HelpLink = helpLink;
        }

        internal UnsupportedExpressionException(System.Linq.Expressions.Expression expression)
            : base("This type of expression is not currently supported: " + expression + ". Simplify the expression, or try a different one. " +
                  "See " + HelpLinks.SupportedExpressions + " for more details")
        {
            HelpLink = HelpLinks.SupportedExpressions;
        }

        internal static UnsupportedExpressionException MySQLConditionalUpdate()
            => new UnsupportedExpressionException("Using conditional updates is not supported in MySQL due to database syntax limitations. " +
                "See " + HelpLinks.MySQLConditionalUpdate + " for more details",
                HelpLinks.MySQLConditionalUpdate);
    }
}
