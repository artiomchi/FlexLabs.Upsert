using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;

namespace FlexLabs.EntityFrameworkCore.Upsert;

/// <summary>
/// Thrown when using unsupported expressions in the update clause
/// See: https://go.flexlabs.org/upsert.expressions
/// </summary>
public class UnsupportedExpressionException : Exception
{
    private UnsupportedExpressionException(string message, string? helpLink = null)
        : base(message)
    {
        HelpLink = helpLink;
    }

    internal UnsupportedExpressionException(Expression expression)
        : base(Resources.ThisTypeOfExpressionIsNotCurrentlySupported + " " + expression + ". " + Resources.SimplifyTheExpressionOrTryADifferentOne +
              Resources.FormatSeeLinkForMoreDetails(HelpLinks.SupportedExpressions))
    {
        HelpLink = HelpLinks.SupportedExpressions;
    }

    internal static UnsupportedExpressionException MySQLConditionalUpdate()
        => new(Resources.UsingConditionalUpdatesIsNotSupportedInMySQLDueToDatabaseSyntaxLimitations + " " +
            Resources.FormatSeeLinkForMoreDetails(HelpLinks.MySQLConditionalUpdate),
            HelpLinks.MySQLConditionalUpdate);

    internal static UnsupportedExpressionException ModifyingJsonMemberNotSupported(Expression expression)
        => new(Resources.FormatModifyingJsonMembersIsNotSupportedUnsupportedExpression(expression), HelpLinks.SupportedExpressions);

    internal static UnsupportedExpressionException ReadingJsonMemberNotSupported(IColumnBase column, Expression expression)
        => new(Resources.FormatReadingJsonMembersIsNotSupportedUnsupportedExpression(expression), HelpLinks.SupportedExpressions);
}
