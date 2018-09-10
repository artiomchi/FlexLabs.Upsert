using System;
using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests
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
            var property = Assert.IsType<ExpressionParameterProperty>(knownValue.Value1);
            Assert.Equal("Num1", property.PropertyName);
            Assert.True(property.IsLeftParameter);
            Assert.Equal(1, knownValue.Value2);
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
            Assert.Equal(1, knownValue.Value1);
            var property = Assert.IsType<ExpressionParameterProperty>(knownValue.Value2);
            Assert.Equal("Num1", property.PropertyName);
            Assert.True(property.IsLeftParameter);
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
            var property = Assert.IsType<ExpressionParameterProperty>(knownValue.Value1);
            Assert.Equal("Num1", property.PropertyName);
            Assert.True(property.IsLeftParameter);
            Assert.Equal(2, knownValue.Value2);
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
            var property = Assert.IsType<ExpressionParameterProperty>(knownValue.Value1);
            Assert.Equal("Num1", property.PropertyName);
            Assert.True(property.IsLeftParameter);
            Assert.Equal(3, knownValue.Value2);
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
            var property = Assert.IsType<ExpressionParameterProperty>(knownValue.Value1);
            Assert.Equal("Num1", property.PropertyName);
            Assert.True(property.IsLeftParameter);
            Assert.Equal(4, knownValue.Value2);
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

            var knownValue = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.MemberAccess, knownValue.ExpressionType);
            var property = Assert.IsType<ExpressionParameterProperty>(knownValue.Value1);
            Assert.Equal("Num1", property.PropertyName);
            Assert.True(property.IsLeftParameter);
            Assert.Null(knownValue.Value2);
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

            var knownValue = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.MemberAccess, knownValue.ExpressionType);
            var property = Assert.IsType<ExpressionParameterProperty>(knownValue.Value1);
            Assert.Equal("Num2", property.PropertyName);
            Assert.True(property.IsLeftParameter);
            Assert.Null(knownValue.Value2);
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

            var knownValue = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.MemberAccess, knownValue.ExpressionType);
            var property = Assert.IsType<ExpressionParameterProperty>(knownValue.Value1);
            Assert.Equal("Num1", property.PropertyName);
            Assert.True(property.IsLeftParameter);
            Assert.Null(knownValue.Value2);
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

            var knownValue = Assert.IsType<KnownExpression>(expValue);
            Assert.Equal(ExpressionType.MemberAccess, knownValue.ExpressionType);
            var property = Assert.IsType<ExpressionParameterProperty>(knownValue.Value1);
            Assert.Equal("Num1", property.PropertyName);
            Assert.False(property.IsLeftParameter);
            Assert.Null(knownValue.Value2);
        }
    }
}
