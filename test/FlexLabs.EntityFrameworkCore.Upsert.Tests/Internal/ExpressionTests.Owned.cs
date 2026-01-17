using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Internal;

public partial class ExpressionTests
{
    [Fact, Trait("Category", "Owned")]
    public void Supports_Owned_Constant()
    {
        var result = Parse((a, e) => new TestEntity
        {
            Child = new OwnedChildEntity
            {
                Num1 = 1
            }
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Child_Num1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().Be(1)
        );
    }

    [Fact, Trait("Category", "Owned")]
    public void Supports_Owned_Field()
    {
        var value = 2;
        var result = Parse((a, e) => new TestEntity
        {
            Child = new OwnedChildEntity
            {
                Num1 = value
            }
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Child_Num1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().Be(value)
        );
    }

    [Fact, Trait("Category", "Owned")]
    public void Supports_Owned_FieldAndProperty()
    {
        var value = new { Num1 = 3 };
        var result = Parse((a, e) => new TestEntity
        {
            Child = new OwnedChildEntity
            {
                Num1 = value.Num1
            }
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Child_Num1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().Be(value.Num1)
        );
    }

    [Fact, Trait("Category", "Owned")]
    public void Supports_Owned_Method()
    {
        var value = "hello_world ";
        var result = Parse((a, e) => new TestEntity
        {
            Child = new OwnedChildEntity
            {
                Text1 = value.Trim(),
            }
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Child_Text1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().Be(value.Trim())
        );
    }

    [Fact, Trait("Category", "Owned")]
    public void Supports_Owned_StaticMethod()
    {
        var value1 = "hello";
        var value2 = "world";
        var result = Parse((a, b) => new TestEntity
        {
            Child = new OwnedChildEntity
            {
                Text1 = string.Join(", ", new string[] { value1, value2 }),
            }
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Child_Text1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().Be(value1 + ", " + value2)
        );
    }

    [Fact, Trait("Category", "Owned")]
    public void Supports_Owned_ValueIncrement()
    {
        var result = Parse((a, b) => new TestEntity
        {
            Child = new OwnedChildEntity
            {
                Num1 = a.Child.Num1 + 1,
            }
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Child_Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Add)
            .HavePropertyValue(e => e.Value1, "Child_Num1", true)
            .HaveConstantValue(e => e.Value2, 1)
        );
    }

    [Fact, Trait("Category", "Owned")]
    public void Supports_Owned_Property()
    {
        var result = Parse((a, e) => new TestEntity
        {
            Child = new OwnedChildEntity
            {
                Num1 = a.Child.Num2
            }
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Child_Num1")
            .WithValueOfType<PropertyValue>()
            .Which.Should().BePropertyValue("Child_Num2", true)
        );
    }

    [Fact, Trait("Category", "Owned")]
    public void Handles_Name_Collisions_With_Owned_Entities()
    {
        var result = Parse((a, e) => new TestEntity
        {
            Child = new OwnedChildEntity
            {
                Num1 = a.Num1
            }
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Child_Num1")
            .WithValueOfType<PropertyValue>()
            .Which.Should().BePropertyValue("Num1", true)
        );
    }
}
