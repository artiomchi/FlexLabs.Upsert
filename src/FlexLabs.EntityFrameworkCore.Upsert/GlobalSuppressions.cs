// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "These exceptions will only be thrown by internal code")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "This will only be called internally. It's marked as public for the test class", Scope = "member", Target = "~M:FlexLabs.EntityFrameworkCore.Upsert.Internal.ExpressionHelpers.GetValue``1(System.Linq.Expressions.Expression,System.Linq.Expressions.LambdaExpression,System.Func{System.String,Microsoft.EntityFrameworkCore.Metadata.IProperty},System.Boolean)~System.Object")]
