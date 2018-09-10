using System;
using System.Linq.Expressions;
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
            var expValue = memberAssig.GetValue<TestEntity>();

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
            var expValue = memberAssig.GetValue<TestEntity>();

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
            var expValue = memberAssig.GetValue<TestEntity>();

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
            var expValue = memberAssig.GetValue<TestEntity>();

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
            var expValue = memberAssig.GetValue<TestEntity>();

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
            var expValue = memberAssig.GetValue<TestEntity>();

            var knownValue = Assert.IsType<KnownExpressions>(expValue);
            Assert.Equal(ExpressionType.Add, knownValue.ExpressionType);
            //Assert.Equal("Num1", knownValue.SourceProperty);
            //Assert.Equal(typeof(TestEntity), knownValue.SourceType);
            Assert.Equal(1, knownValue.Value);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueSubtract()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 - 2,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>();

            var knownValue = Assert.IsType<KnownExpressions>(expValue);
            Assert.Equal(ExpressionType.Subtract, knownValue.ExpressionType);
            //Assert.Equal("Num1", knownValue.SourceProperty);
            //Assert.Equal(typeof(TestEntity), knownValue.SourceType);
            Assert.Equal(2, knownValue.Value);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueMultiply()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 * 3,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>();

            var knownValue = Assert.IsType<KnownExpressions>(expValue);
            Assert.Equal(ExpressionType.Multiply, knownValue.ExpressionType);
            //Assert.Equal("Num1", knownValue.SourceProperty);
            //Assert.Equal(typeof(TestEntity), knownValue.SourceType);
            Assert.Equal(3, knownValue.Value);
        }

        [Fact]
        public void ExpressionHelpersTests_ValueDivide()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1 / 4,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>();

            var knownValue = Assert.IsType<KnownExpressions>(expValue);
            Assert.Equal(ExpressionType.Divide, knownValue.ExpressionType);
            //Assert.Equal("Num1", knownValue.SourceProperty);
            //Assert.Equal(typeof(TestEntity), knownValue.SourceType);
            Assert.Equal(4, knownValue.Value);
        }

        [Fact]
        public void ExpressionHelpersTests_OtherProp()
        {
            Expression<Func<TestEntity, TestEntity>> exp = e => new TestEntity
            {
                Num1 = e.Num1,
            };

            var memberAssig = GetMemberExpression(exp);
            var expValue = memberAssig.GetValue<TestEntity>();

            var knownValue = Assert.IsType<KnownExpressions>(expValue);
            Assert.Equal(ExpressionType.MemberAccess, knownValue.ExpressionType);
            //Assert.Equal("Num1", knownValue.SourceProperty);
            //Assert.Equal(typeof(TestEntity), knownValue.SourceType);
            Assert.Equal("Num1", knownValue.Value);
        }
    }
}
