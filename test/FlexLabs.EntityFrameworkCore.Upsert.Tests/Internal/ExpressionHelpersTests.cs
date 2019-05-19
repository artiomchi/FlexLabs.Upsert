using System;
using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using FlexLabs.EntityFrameworkCore.Upsert.Tests.EF.Base;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Internal
{
    public class ExpressionHelpersTests
    {
        private Expression GetMemberExpression(LambdaExpression expression)
            => ((MemberAssignment)((MemberInitExpression)expression.Body).Bindings[0]).Expression;

        private KnownExpression IsKnownExpression(object value, ExpressionType expressionType)
        {
            var expression = Assert.IsType<KnownExpression>(value);
            Assert.Equal(expressionType, expression.ExpressionType);
            return expression;
        }

        private ConstantValue IsConstantValue(object value, object expectedValue)
        {
            var constant = Assert.IsType<ConstantValue>(value);
            Assert.Equal(expectedValue, constant.Value);
            return constant;
        }

        private PropertyValue IsPropertyValue(object value, string name, bool isLeftParam)
        {
            var property = Assert.IsType<PropertyValue>(value);
            Assert.Equal(name, property.PropertyName);
            Assert.Equal(isLeftParam, property.IsLeftParameter);
            return property;
        }

        [Fact]
        public void ExpressionHelpersTests_Constant()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = 1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            Assert.Equal(1, expValue);
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
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            Assert.Equal(value, expValue);
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
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            Assert.Equal(value.Num1, expValue);
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
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            Assert.Equal(value.Trim(), expValue);
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
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            Assert.Equal(value1 + ", " + value2, expValue);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueIncrement()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 + 1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var knownValue = IsKnownExpression(expValue, ExpressionType.Add);
            IsPropertyValue(knownValue.Value1, "Num1", true);
            IsConstantValue(knownValue.Value2, 1);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueIncrement_Reverse()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = 1 + e.Num1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var knownValue = IsKnownExpression(expValue, ExpressionType.Add);
            IsConstantValue(knownValue.Value1, 1);
            IsPropertyValue(knownValue.Value2, "Num1", true);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueSubtract()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 - 2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var knownValue = IsKnownExpression(expValue, ExpressionType.Subtract);
            IsPropertyValue(knownValue.Value1, "Num1", true);
            IsConstantValue(knownValue.Value2, 2);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueMultiply()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 * 3,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var knownValue = IsKnownExpression(expValue, ExpressionType.Multiply);
            IsPropertyValue(knownValue.Value1, "Num1", true);
            IsConstantValue(knownValue.Value2, 3);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueDivide()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 / 4,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var knownValue = IsKnownExpression(expValue, ExpressionType.Divide);
            IsPropertyValue(knownValue.Value1, "Num1", true);
            IsConstantValue(knownValue.Value2, 4);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueModulo()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 % 4,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var knownValue = IsKnownExpression(expValue, ExpressionType.Modulo);
            IsPropertyValue(knownValue.Value1, "Num1", true);
            IsConstantValue(knownValue.Value2, 4);
        }

        [Fact]
        public void ExpressionHelpersTests_Property()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            IsPropertyValue(expValue, "Num1", true);
        }

        [Fact]
        public void ExpressionHelpersTests_PropertyOther()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            IsPropertyValue(expValue, "Num2", true);
        }

        [Fact]
        public void ExpressionHelpersTests_Property_WithSource()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e1.Num1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            IsPropertyValue(expValue, "Num1", true);
        }

        [Fact]
        public void ExpressionHelpersTests_Property_FromSource()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e2.Num1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            IsPropertyValue(expValue, "Num1", false);
        }

        [Fact]
        public void ExpressionHelpersTests_DateTime_Now()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Updated = DateTime.Now,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var updated = Assert.IsType<DateTime>(expValue);
            Assert.True(updated > DateTime.Now.AddMinutes(-1));
            Assert.True(updated < DateTime.Now.AddMinutes(1));
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
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var num = Assert.IsType<int>(expValue);
            Assert.Equal(value, num);
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
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var num = Assert.IsType<int>(expValue);
            Assert.Equal(value.Value, num);
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
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var num = Assert.IsType<int>(expValue);
            Assert.Equal(value, num);
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
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var num = Assert.IsType<int>(expValue);
            Assert.Equal(value.Value, num);
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
            Assert.Throws<UnsupportedExpressionException>(() =>
            {
                memberAssig.GetValue<TestEntity>(exp);
            });

            var expValue = memberAssig.GetValue<TestEntity>(exp, useExpressionCompiler: true);

            var num1 = Assert.IsType<int>(expValue);
            Assert.Equal(input << 4, num1);
        }

        [Fact]
        public void CompoundExpression_Sum()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Text1 = e1.Text1 + "." + e2.Text2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var expression = IsKnownExpression(expValue, ExpressionType.Add);
            var exp1 = IsKnownExpression(expression.Value1, ExpressionType.Add);
            IsPropertyValue(exp1.Value1, "Text1", true);
            IsConstantValue(exp1.Value2, ".");
            IsPropertyValue(expression.Value2, "Text2", false);
        }

        [Fact]
        public void CompoundExpression_Sum_Grouped1()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Text1 = (e1.Text1 + ".") + e2.Text2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var expression = IsKnownExpression(expValue, ExpressionType.Add);
            var exp1 = IsKnownExpression(expression.Value1, ExpressionType.Add);
            IsPropertyValue(exp1.Value1, "Text1", true);
            IsConstantValue(exp1.Value2, ".");
            IsPropertyValue(expression.Value2, "Text2", false);
        }

        [Fact]
        public void CompoundExpression_Sum_Grouped2()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Text1 = e1.Text1 + ("." + e2.Text2),
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var expression = IsKnownExpression(expValue, ExpressionType.Add);
            var exp1 = IsKnownExpression(expression.Value2, ExpressionType.Add);
            IsPropertyValue(expression.Value1, "Text1", true);
            IsConstantValue(exp1.Value1, ".");
            IsPropertyValue(exp1.Value2, "Text2", false);
        }

        [Fact]
        public void CompoundExpression_Multiply()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e1.Num1 + 7 * e2.Num2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var expression = IsKnownExpression(expValue, ExpressionType.Add);
            var exp1 = IsKnownExpression(expression.Value2, ExpressionType.Multiply);
            IsPropertyValue(expression.Value1, "Num1", true);
            IsConstantValue(exp1.Value1, 7);
            IsPropertyValue(exp1.Value2, "Num2", false);
        }

        [Fact]
        public void CompoundExpression_Multiply_Grouped1()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = (e1.Num1 + 7) * e2.Num2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var expression = IsKnownExpression(expValue, ExpressionType.Multiply);
            var exp1 = IsKnownExpression(expression.Value1, ExpressionType.Add);
            IsPropertyValue(exp1.Value1, "Num1", true);
            IsConstantValue(exp1.Value2, 7);
            IsPropertyValue(expression.Value2, "Num2", false);
        }

        [Fact]
        public void CompoundExpression_Multiply_Grouped2()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e1.Num1 + (7 * e2.Num2),
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var expression = IsKnownExpression(expValue, ExpressionType.Add);
            var exp1 = IsKnownExpression(expression.Value2, ExpressionType.Multiply);
            IsPropertyValue(expression.Value1, "Num1", true);
            IsConstantValue(exp1.Value1, 7);
            IsPropertyValue(exp1.Value2, "Num2", false);
        }

        [Fact]
        public void CompoundExpression_Conditional()
        {
            Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (e1, e2) => new TestEntity
            {
                Num1 = e1.Num1 + 7 < 0 ? 0 : e1.Num1 + 7,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>(exp);

            var condExpr = IsKnownExpression(expValue, ExpressionType.Conditional);
            var testExp = IsKnownExpression(condExpr.Value3, ExpressionType.LessThan);
            var testSumExp = IsKnownExpression(testExp.Value1, ExpressionType.Add);
            IsPropertyValue(testSumExp.Value1, "Num1", true);
            IsConstantValue(testSumExp.Value2, 7);
            IsConstantValue(testExp.Value2, 0);
            IsConstantValue(condExpr.Value1, 0);
            var falseExp = IsKnownExpression(condExpr.Value2, ExpressionType.Add);
            IsPropertyValue(falseExp.Value1, "Num1", true);
            IsConstantValue(falseExp.Value2, 7);
        }
    }
}
