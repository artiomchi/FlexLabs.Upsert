using System;
using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Internal
{
    public class ExpressionHelpersTests
    {
        private Expression GetMemberExpression(LambdaExpression expression)
            => ((MemberAssignment)((MemberInitExpression)expression.Body).Bindings[0]).Expression;

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

            var knownValue = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Add, knownValue.ExpressionType);
            var value1 = Assert.IsType<PropertyValue>(knownValue.Value1);
            Assert.Equal("Num1", value1.PropertyName);
            Assert.True(value1.IsLeftParameter);
            var value2 = Assert.IsType<ConstantValue>(knownValue.Value2);
            Assert.Equal(1, value2.Value);
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

            var knownValue = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Add, knownValue.ExpressionType);
            var value1 = Assert.IsType<ConstantValue>(knownValue.Value1);
            Assert.Equal(1, value1.Value);
            var value2 = Assert.IsType<PropertyValue>(knownValue.Value2);
            Assert.Equal("Num1", value2.PropertyName);
            Assert.True(value2.IsLeftParameter);
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

            var knownValue = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Subtract, knownValue.ExpressionType);
            var value1 = Assert.IsType<PropertyValue>(knownValue.Value1);
            Assert.Equal("Num1", value1.PropertyName);
            Assert.True(value1.IsLeftParameter);
            var value2 = Assert.IsType<ConstantValue>(knownValue.Value2);
            Assert.Equal(2, value2.Value);
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

            var knownValue = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Multiply, knownValue.ExpressionType);
            var value1 = Assert.IsType<PropertyValue>(knownValue.Value1);
            Assert.Equal("Num1", value1.PropertyName);
            Assert.True(value1.IsLeftParameter);
            var value2 = Assert.IsType<ConstantValue>(knownValue.Value2);
            Assert.Equal(3, value2.Value);
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

            var knownValue = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Divide, knownValue.ExpressionType);
            var value1 = Assert.IsType<PropertyValue>(knownValue.Value1);
            Assert.Equal("Num1", value1.PropertyName);
            Assert.True(value1.IsLeftParameter);
            var value2 = Assert.IsType<ConstantValue>(knownValue.Value2);
            Assert.Equal(4, value2.Value);
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

            var knownValue = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Modulo, knownValue.ExpressionType);
            var value1 = Assert.IsType<PropertyValue>(knownValue.Value1);
            Assert.Equal("Num1", value1.PropertyName);
            Assert.True(value1.IsLeftParameter);
            var value2 = Assert.IsType<ConstantValue>(knownValue.Value2);
            Assert.Equal(4, value2.Value);
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

            var property = Assert.IsType<PropertyValue>(expValue);
            Assert.Equal("Num1", property.PropertyName);
            Assert.True(property.IsLeftParameter);
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

            var property = Assert.IsType<PropertyValue>(expValue);
            Assert.Equal("Num2", property.PropertyName);
            Assert.True(property.IsLeftParameter);
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

            var property = Assert.IsType<PropertyValue>(expValue);
            Assert.Equal("Num1", property.PropertyName);
            Assert.True(property.IsLeftParameter);
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

            var property = Assert.IsType<PropertyValue>(expValue);
            Assert.Equal("Num1", property.PropertyName);
            Assert.False(property.IsLeftParameter);
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
            var input = "hello";
            Expression<Func<TestEntity, TestEntity>> exp = e1 => new TestEntity
            {
                Num1 = !string.IsNullOrWhiteSpace(input)
                    ? input.Length
                    : 0,
            };

            var memberAssig = GetMemberExpression(exp);
            Assert.Throws<UnsupportedExpressionException>(() =>
            {
                memberAssig.GetValue<TestEntity>(exp);
            });

            var expValue = memberAssig.GetValue<TestEntity>(exp, useExpressionCompiler: true);
            var num1 = Assert.IsType<int>(expValue);
            Assert.Equal(input.Length, num1);
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

            var expression = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Add, expression.ExpressionType);

            var exp1 = Assert.IsType<KnownExpression>(expression.Value1);
            Assert.Equal(ExpressionType.Add, exp1.ExpressionType);
            var param1 = Assert.IsType<PropertyValue>(exp1.Value1);
            Assert.Equal("Text1", param1.PropertyName);
            var param2 = Assert.IsType<ConstantValue>(exp1.Value2);
            Assert.Equal(".", param2.Value);
            var param3 = Assert.IsType<PropertyValue>(expression.Value2);
            Assert.Equal("Text2", param3.PropertyName);
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

            var expression = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Add, expression.ExpressionType);

            var exp1 = Assert.IsType<KnownExpression>(expression.Value1);
            Assert.Equal(ExpressionType.Add, exp1.ExpressionType);
            var param1 = Assert.IsType<PropertyValue>(exp1.Value1);
            Assert.Equal("Text1", param1.PropertyName);
            var param2 = Assert.IsType<ConstantValue>(exp1.Value2);
            Assert.Equal(".", param2.Value);
            var param3 = Assert.IsType<PropertyValue>(expression.Value2);
            Assert.Equal("Text2", param3.PropertyName);
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

            var expression = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Add, expression.ExpressionType);

            var param1 = Assert.IsType<PropertyValue>(expression.Value1);
            Assert.Equal("Text1", param1.PropertyName);
            var exp1 = Assert.IsType<KnownExpression>(expression.Value2);
            Assert.Equal(ExpressionType.Add, exp1.ExpressionType);
            var param2 = Assert.IsType<ConstantValue>(exp1.Value1);
            Assert.Equal(".", param2.Value);
            var param3 = Assert.IsType<PropertyValue>(exp1.Value2);
            Assert.Equal("Text2", param3.PropertyName);
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

            var expression = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Add, expression.ExpressionType);

            var param1 = Assert.IsType<PropertyValue>(expression.Value1);
            Assert.Equal("Num1", param1.PropertyName);
            var exp1 = Assert.IsType<KnownExpression>(expression.Value2);
            Assert.Equal(ExpressionType.Multiply, exp1.ExpressionType);
            var param2 = Assert.IsType<ConstantValue>(exp1.Value1);
            Assert.Equal(7, param2.Value);
            var param3 = Assert.IsType<PropertyValue>(exp1.Value2);
            Assert.Equal("Num2", param3.PropertyName);
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

            var expression = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Multiply, expression.ExpressionType);

            var exp1 = Assert.IsType<KnownExpression>(expression.Value1);
            Assert.Equal(ExpressionType.Add, exp1.ExpressionType);
            var param1 = Assert.IsType<PropertyValue>(exp1.Value1);
            Assert.Equal("Num1", param1.PropertyName);
            var param2 = Assert.IsType<ConstantValue>(exp1.Value2);
            Assert.Equal(7, param2.Value);
            var param3 = Assert.IsType<PropertyValue>(expression.Value2);
            Assert.Equal("Num2", param3.PropertyName);
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

            var expression = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.Add, expression.ExpressionType);

            var param1 = Assert.IsType<PropertyValue>(expression.Value1);
            Assert.Equal("Num1", param1.PropertyName);
            var exp1 = Assert.IsType<KnownExpression>(expression.Value2);
            Assert.Equal(ExpressionType.Multiply, exp1.ExpressionType);
            var param2 = Assert.IsType<ConstantValue>(exp1.Value1);
            Assert.Equal(7, param2.Value);
            var param3 = Assert.IsType<PropertyValue>(exp1.Value2);
            Assert.Equal("Num2", param3.PropertyName);
        }
    }
}
