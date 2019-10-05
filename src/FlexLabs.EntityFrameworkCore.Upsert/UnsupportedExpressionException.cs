using System;
using System.Globalization;

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
            : base(Resources.ThisTypeOfExpressionIsNotCurrentlySupported + " " + expression + ". " + Resources.SimplifyTheExpressionOrTryADifferentOne +
                  string.Format(CultureInfo.InvariantCulture, Resources.SeeLinkForMoreDetails, HelpLinks.SupportedExpressions))
        {
            HelpLink = HelpLinks.SupportedExpressions;
        }

        internal static UnsupportedExpressionException MySQLConditionalUpdate()
            => new UnsupportedExpressionException(Resources.UsingConditionalUpdatesIsNotSupportedInMySQLDueToDatabaseSyntaxLimitations + " " + 
                string.Format(CultureInfo.InvariantCulture, Resources.SeeLinkForMoreDetails, HelpLinks.MySQLConditionalUpdate),
                HelpLinks.MySQLConditionalUpdate);
    }
}
