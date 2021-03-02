using System;
using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Internal
{
    public class ExpressionHelpersTests
    {
        private static Expression GetMemberExpression(LambdaExpression expression)
            => ((MemberAssignment)((MemberInitExpression)expression.Body).Bindings[0]).Expression;

        private IProperty NoProperty(string propertyName) => default;

        [Fact]
        public void ExpressionHelpersTests_Constant()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = 1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().Be(1);
        }

        [Fact]
        public void ExpressionHelpersTests_Field()
        {
            var value = 2;
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = value,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().Be(value);
        }

        [Fact]
        public void ExpressionHelpersTests_FieldAndProperty()
        {
            var value = new TestEntity { Num1 = 3 };
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = value.Num1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().Be(value.Num1);
        }

        [Fact]
        public void ExpressionHelpersTests_Method()
        {
            var value = "hello_world ";
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Text1 = value.Trim(),
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().Be(value.Trim());
        }

        [Fact]
        public void ExpressionHelpersTests_StaticMethod()
        {
            var value1 = "hello";
            var value2 = "world";
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Text1 = string.Join(", ", value1, value2),
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().Be(value1 + ", " + value2);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueIncrement()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 + 1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Add)
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HaveConstantValue(e => e.Value2, 1);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueIncrement_Reverse()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = 1 + e.Num1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Add)
               .HaveConstantValue(e => e.Value1, 1)
               .HavePropertyValue(e => e.Value2, "Num1", true);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueSubtract()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 - 2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Subtract)
               .HavePropertyValue(e => e.Value1, "Num1", true)
               .HaveConstantValue(e => e.Value2, 2);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueMultiply()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 * 3,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Multiply)
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HaveConstantValue(e => e.Value2, 3);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueDivide()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 / 4,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Divide)
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HaveConstantValue(e => e.Value2, 4);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueModulo()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 % 4,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Modulo)
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HaveConstantValue(e => e.Value2, 4);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueBitwiseOr()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 | 1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Or)
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HaveConstantValue(e => e.Value2, 1);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueBitwiseAnd()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 & 1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.And)
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HaveConstantValue(e => e.Value2, 1);
        }

        [Fact]
        public void ExpressionHelpersTests_Property()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BePropertyValue("Num1", true);
        }

        [Fact]
        public void ExpressionHelpersTests_PropertyOther()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BePropertyValue("Num2", true);
        }

        [Fact]
        public void ExpressionHelpersTests_Property_WithSource()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e1.Num1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BePropertyValue("Num1", true);
        }

        [Fact]
        public void ExpressionHelpersTests_Property_FromSource()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e2.Num1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BePropertyValue("Num1", false);
        }

        [Fact]
        public void ExpressionHelpersTests_DateTime_Now()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Updated = DateTime.Now,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeOfType<DateTime>().Subject
                .Should().BeBefore(DateTime.Now.AddMinutes(1))
                .And.BeAfter(DateTime.Now.AddMinutes(-1));
        }

        [Fact]
        public void ExpressionHelpersTests_Nullable_Assign()
        {
            int value = 5;

            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                NumNullable1 = value,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeOfType<int>().And.Be(value);
        }

        [Fact]
        public void ExpressionHelpersTests_Nullable_Cast()
        {
            int? value = 5;

            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = (int)value,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeOfType<int>().And.Be(value);
        }

        [Fact]
        public void ExpressionHelpersTests_Nullable_Coalesce()
        {
            int? value = 5;

            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = value ?? 0,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeOfType<int>().And.Be(value);
        }

        [Fact]
        public void ExpressionHelpersTests_Nullable_GetValueOrDefault()
        {
            int? value = 5;

            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = value.GetValueOrDefault(),
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeOfType<int>().And.Be(value);
        }

        [Fact]
        public void ExpressionHelperTests_UnsupportedExpression()
        {
            var input = 5;
            Expression<Func<TestEntity, TestEntity>> exp = e1 => new TestEntity
            {
                Num1 = input << 4,
            };

            var memberAssig = GetMemberExpression(exp);
            Action action = () => memberAssig.GetValue<TestEntity>(exp, NoProperty);
            action.Should().Throw<UnsupportedExpressionException>();

            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty, useExpressionCompiler: true);

            expValue.Should().BeOfType<int>().And.Be(input << 4);
        }

        [Fact]
        public void CompoundExpression_Sum()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Text1 = e1.Text1 + "." + e2.Text2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Add)
                .HaveKnownExpression(e => e.Value1, ExpressionType.Add, ke => ke
                    .HavePropertyValue(e => e.Value1, "Text1", true)
                    .HaveConstantValue(e => e.Value2, "."))
                .HavePropertyValue(e => e.Value2, "Text2", false);
        }

        [Fact]
        public void CompoundExpression_Sum_Grouped1()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Text1 = (e1.Text1 + ".") + e2.Text2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Add)
                .HaveKnownExpression(e => e.Value1, ExpressionType.Add, ke => ke
                    .HavePropertyValue(e => e.Value1, "Text1", true)
                    .HaveConstantValue(e => e.Value2, "."))
                .HavePropertyValue(e => e.Value2, "Text2", false);
        }

        [Fact]
        public void CompoundExpression_Sum_Grouped2()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Text1 = e1.Text1 + ("." + e2.Text2),
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Add)
                .HavePropertyValue(e => e.Value1, "Text1", true)
                .HaveKnownExpression(e => e.Value2, ExpressionType.Add, ke => ke
                    .HaveConstantValue(e => e.Value1, ".")
                    .HavePropertyValue(e => e.Value2, "Text2", false));
        }

        [Fact]
        public void CompoundExpression_Multiply()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e1.Num1 + 7 * e2.Num2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Add)
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HaveKnownExpression(e => e.Value2, ExpressionType.Multiply, ke => ke
                    .HaveConstantValue(e => e.Value1, 7)
                    .HavePropertyValue(e => e.Value2, "Num2", false));
        }

        [Fact]
        public void CompoundExpression_Multiply_Grouped1()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = (e1.Num1 + 7) * e2.Num2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Multiply)
                .HaveKnownExpression(e => e.Value1, ExpressionType.Add, ke => ke
                    .HavePropertyValue(e => e.Value1, "Num1", true)
                    .HaveConstantValue(e => e.Value2, 7))
                .HavePropertyValue(e => e.Value2, "Num2", false);
        }

        [Fact]
        public void CompoundExpression_Multiply_Grouped2()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e1.Num1 + (7 * e2.Num2),
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Add)
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HaveKnownExpression(e => e.Value2, ExpressionType.Multiply, ke => ke
                    .HaveConstantValue(e => e.Value1, 7)
                    .HavePropertyValue(e => e.Value2, "Num2", false));
        }

        [Fact]
        public void CompoundExpression_Conditional()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e1.Num1 + 7 < 0 ? 0 : e1.Num1 + 7,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Conditional)
                .HaveConstantValue(e => e.Value1, 0)
                .HaveKnownExpression(e => e.Value2, ExpressionType.Add, ke => ke
                    .HavePropertyValue(e => e.Value1, "Num1", true)
                    .HaveConstantValue(e => e.Value2, 7))
                .HaveKnownExpression(e => e.Value3, ExpressionType.LessThan, ke => ke
                    .HaveKnownExpression(e => e.Value1, ExpressionType.Add, ke2 => ke2
                        .HavePropertyValue(e => e.Value1, "Num1", true)
                        .HaveConstantValue(e => e.Value2, 7))
                    .HaveConstantValue(e => e.Value2, 0));
        }

        [Fact]
        public void CompoundExpression_Conditional_NotEqual()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e1.Num1 != 4 ? 0 : 1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Conditional)
                .HaveConstantValue(e => e.Value1, 0)
                .HaveConstantValue(e => e.Value2, 1)
                .HaveKnownExpression(e => e.Value3, ExpressionType.NotEqual, ke => ke
                    .HavePropertyValue(e => e.Value1, "Num1", true)
                    .HaveConstantValue(e => e.Value2, 4));
        }

        [Fact]
        public void CompoundExpression_Conditional_NotNull()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e1.Text1 != null ? 0 : 1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.Conditional)
                .HaveConstantValue(e => e.Value1, 0)
                .HaveConstantValue(e => e.Value2, 1)
                .HaveKnownExpression(e => e.Value3, ExpressionType.NotEqual, ke => ke
                    .HavePropertyValue(e => e.Value1, "Text1", true)
                    .HaveConstantValue(e => e.Value2, null));
        }

        [Fact]
        public void Condition_AndAlso()
        {
            Expression<Func<TestEntity, TestEntity, bool>> exp = (e1, e2)
                => e1.Num1 != e2.Num1 && e1.Text1 != e2.Text1;

            var expValue = exp.Body.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.AndAlso)
                .HaveKnownExpression(e => e.Value1, ExpressionType.NotEqual, ke => ke
                    .HavePropertyValue(e => e.Value1, "Num1", true)
                    .HavePropertyValue(e => e.Value2, "Num1", false))
                .HaveKnownExpression(e => e.Value2, ExpressionType.NotEqual, ke => ke
                    .HavePropertyValue(e => e.Value1, "Text1", true)
                    .HavePropertyValue(e => e.Value2, "Text1", false));
        }

        [Fact]
        public void Condition_ElseIf()
        {
            Expression<Func<TestEntity, TestEntity, bool>> exp = (e1, e2)
                => e1.Num1 != e2.Num1 || e1.Text1 != e2.Text1;

            var expValue = exp.Body.GetValue<TestEntity>(exp, NoProperty);

            expValue.Should().BeKnownExpression(ExpressionType.OrElse)
                .HaveKnownExpression(e => e.Value1, ExpressionType.NotEqual, ke => ke
                    .HavePropertyValue(e => e.Value1, "Num1", true)
                    .HavePropertyValue(e => e.Value2, "Num1", false))
                .HaveKnownExpression(e => e.Value2, ExpressionType.NotEqual, ke => ke
                    .HavePropertyValue(e => e.Value1, "Text1", true)
                    .HavePropertyValue(e => e.Value2, "Text1", false));
        }
    }
}
