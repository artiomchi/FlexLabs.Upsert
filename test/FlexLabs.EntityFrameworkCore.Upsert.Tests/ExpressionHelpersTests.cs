using System;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests
{
    public class ExpressionHelpersTests
    {
        [Fact]
        public void ExpressionHelpersTests_ConstantExpression()
        {
            Expression<Func<TestEntity>> exp = () => new TestEntity
            {
                Num1 = 1,
            };
            var initExp = exp.Body as MemberInitExpression;
            var bindingExp = initExp.Bindings[0];

            var memberAssig = Assert.IsType<MemberAssignment>(bindingExp);
            Assert.IsType<ConstantExpression>(memberAssig.Expression);
            Assert.Equal(1, memberAssig.Expression.GetValue());
        }

        [Fact]
        public void ExpressionHelpersTests_FieldExpression()
        {
            var value = 2;
            Expression<Func<TestEntity>> exp = () => new TestEntity
            {
                Num1 = value,
            };
            var initExp = exp.Body as MemberInitExpression;
            var bindingExp = initExp.Bindings[0];

            var memberAssig = Assert.IsType<MemberAssignment>(bindingExp);
            var memberExp = Assert.IsAssignableFrom<MemberExpression>(memberAssig.Expression);
            Assert.IsAssignableFrom<FieldInfo>(memberExp.Member);
            Assert.Equal(value, memberAssig.Expression.GetValue());
        }

        [Fact]
        public void ExpressionHelpersTests_FieldAndPropertyExpression()
        {
            var value = new TestEntity { Num1 = 3 };
            Expression<Func<TestEntity>> exp = () => new TestEntity
            {
                Num1 = value.Num1,
            };
            var initExp = exp.Body as MemberInitExpression;
            var bindingExp = initExp.Bindings[0];

            var memberAssig = Assert.IsType<MemberAssignment>(bindingExp);
            var memberExp = Assert.IsAssignableFrom<MemberExpression>(memberAssig.Expression);
            Assert.IsAssignableFrom<PropertyInfo>(memberExp.Member);
            Assert.Equal(value.Num1, memberAssig.Expression.GetValue());
        }

        [Fact]
        public void ExpressionHelpersTests_MethodExpression()
        {
            var value = "hello_world ";
            Expression<Func<TestEntity>> exp = () => new TestEntity
            {
                Text1 = value.Trim(),
            };
            var initExp = exp.Body as MemberInitExpression;
            var bindingExp = initExp.Bindings[0];

            var memberAssig = Assert.IsType<MemberAssignment>(bindingExp);
            var memberExp = Assert.IsAssignableFrom<MethodCallExpression>(memberAssig.Expression);
            Assert.Equal("Trim", memberExp.Method.Name);
            Assert.Equal(value.Trim(), memberAssig.Expression.GetValue());
        }
    }
}
