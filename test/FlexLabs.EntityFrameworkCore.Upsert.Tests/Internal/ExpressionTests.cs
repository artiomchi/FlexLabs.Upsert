using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;


namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Internal;

public partial class ExpressionTests(ITestOutputHelper output) {
    private readonly ExpressionParser<TestEntity> _parser = new(new TestRelationalTable(), new RunnerQueryOptions());
    private readonly ExpressionParser<TestEntity> _parserWithCompiler = new(new TestRelationalTable(), new RunnerQueryOptions { UseExpressionCompiler = true });

    #region helper

    private PropertyMapping[] Parse(Expression<Func<TestEntity, TestEntity, TestEntity>> updater, bool useExpressionCompiler = false)
    {
        var result = useExpressionCompiler switch {
            true => _parserWithCompiler.ParseUpdateExpression(updater),
            false => _parser.ParseUpdateExpression(updater),
        };
        Print(result, updater);
        return result;
    }

    private void Print(IEnumerable<PropertyMapping> mappings, object updater)
    {
        output.WriteLine("Expression:");
        output.WriteLine(updater.ToString());

        output.WriteLine("\nResult:");
        output.WriteLine("new TestEntity {");

        foreach (var mapping in mappings) {
            output.WriteLine($"  {mapping.Property.ColumnName} = {Expand(mapping.Value)},");
        }

        output.WriteLine("}");
        return;

        object Expand(IKnownValue value)
        {
            return value switch {
                ConstantValue x => x.Value,
                PropertyValue x => $"{(x.IsLeftParameter ? "a" : "b")}{x.Property.Path}.{x.Property.Name}",
                KnownExpression x => x.ExpressionType switch {
                    ExpressionType.Conditional => $"{Expand(x.Value3)} ? {Expand(x.Value1)} : {Expand(x.Value2)}",
                    ExpressionType.LessThan => $"{Expand(x.Value1)} < {Expand(x.Value2)}",
                    ExpressionType.NotEqual => $"{Expand(x.Value1)} != {Expand(x.Value2)}",
                    ExpressionType.Add => $"{Expand(x.Value1)} + {Expand(x.Value2)}",
                    ExpressionType.Subtract => $"{Expand(x.Value1)} - {Expand(x.Value2)}",
                    ExpressionType.Multiply => $"{Expand(x.Value1)} * {Expand(x.Value2)}",
                    ExpressionType.Divide => $"{Expand(x.Value1)} / {Expand(x.Value2)}",
                    ExpressionType.AndAlso => $"{Expand(x.Value1)} && {Expand(x.Value2)}",
                    ExpressionType.OrElse => $"{Expand(x.Value1)} || {Expand(x.Value2)}",
                    ExpressionType.And => $"{Expand(x.Value1)} & {Expand(x.Value2)}",
                    ExpressionType.Or => $"{Expand(x.Value1)} | {Expand(x.Value2)}",
                    ExpressionType.Modulo => $"{Expand(x.Value1)} % {Expand(x.Value2)}",
                    _ => throw new ArgumentOutOfRangeException(nameof(x), x.ExpressionType, null)
                },
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }
    }

    #endregion


    [Fact]
    public void Supports_Constant()
    {
        var result = Parse((a, e) => new TestEntity {
            Num1 = 1,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().Be(1)
        );
    }

    [Fact]
    public void Supports_Field()
    {
        var value = 2;
        var result = Parse((a, e) => new TestEntity {
            Num1 = value,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().Be(value)
        );
    }

    [Fact]
    public void Supports_FieldAndProperty()
    {
        var value = new TestEntity { Num1 = 3 };
        var result = Parse((a, e) => new TestEntity {
            Num1 = value.Num1,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().Be(value.Num1)
        );
    }

    [Fact]
    public void Supports_Method()
    {
        var value = "hello_world ";
        var result = Parse((a, e) => new TestEntity {
            Text1 = value.Trim(),
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Text1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().Be(value.Trim())
        );
    }

    [Fact]
    public void Supports_StaticMethod()
    {
        var value1 = "hello";
        var value2 = "world";
        var result = Parse((a, b) => new TestEntity {
            Text1 = string.Join(", ", new string[] { value1, value2 }),
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Text1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().Be(value1 + ", " + value2)
        );
    }

    [Fact]
    public void Supports_ValueIncrement()
    {
        var result = Parse((a, b) => new TestEntity {
            Num1 = a.Num1 + 1,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Add)
            .HavePropertyValue(e => e.Value1, "Num1", true)
            .HaveConstantValue(e => e.Value2, 1)
        );
    }

    [Fact]
    public void Supports_ValueIncrement_Reverse()
    {
        var result = Parse((a, b) => new TestEntity {
            Num1 = 1 + a.Num1,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Add)
            .HaveConstantValue(e => e.Value1, 1)
            .HavePropertyValue(e => e.Value2, "Num1", true)
        );
    }

    [Fact]
    public void Supports_ValueSubtract()
    {
        var result = Parse((a, b) => new TestEntity {
            Num1 = a.Num1 - 2,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Subtract)
            .HavePropertyValue(e => e.Value1, "Num1", true)
            .HaveConstantValue(e => e.Value2, 2)
        );
    }

    [Fact]
    public void Supports_ValueMultiply()
    {
        var result = Parse((a, b) => new TestEntity {
            Num1 = a.Num1 * 3,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Multiply)
            .HavePropertyValue(e => e.Value1, "Num1", true)
            .HaveConstantValue(e => e.Value2, 3)
        );
    }

    [Fact]
    public void Supports_ValueDivide()
    {
        var result = Parse((a, b) => new TestEntity {
            Num1 = a.Num1 / 4,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Divide)
            .HavePropertyValue(e => e.Value1, "Num1", true)
            .HaveConstantValue(e => e.Value2, 4)
        );
    }

    [Fact]
    public void Supports_ValueModulo()
    {
        var result = Parse((a, b) => new TestEntity {
            Num1 = a.Num1 % 4,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Modulo)
            .HavePropertyValue(e => e.Value1, "Num1", true)
            .HaveConstantValue(e => e.Value2, 4)
        );
    }

    [Fact]
    public void Supports_ValueBitwiseOr()
    {
        var result = Parse((a, b) => new TestEntity {
            Num1 = a.Num1 | 1,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Or)
            .HavePropertyValue(e => e.Value1, "Num1", true)
            .HaveConstantValue(e => e.Value2, 1)
        );
    }

    [Fact]
    public void Supports_ValueBitwiseAnd()
    {
        var result = Parse((a, b) => new TestEntity {
            Num1 = a.Num1 & 1,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.And)
            .HavePropertyValue(e => e.Value1, "Num1", true)
            .HaveConstantValue(e => e.Value2, 1)
        );
    }

    [Fact]
    public void Supports_Property()
    {
        var result = Parse((a, b) => new TestEntity {
            Num1 = a.Num1,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<PropertyValue>()
            .Which.Should().BePropertyValue("Num1", true)
        );
    }

    [Fact]
    public void Supports_PropertyOther()
    {
        var result = Parse((a, b) => new TestEntity {
            Num1 = a.Num2,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<PropertyValue>()
            .Which.Should().BePropertyValue("Num2", true)
        );
    }

    [Fact]
    public void Supports_Property_WithSource()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Num1 = e1.Num1,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<PropertyValue>()
            .Which.Should().BePropertyValue("Num1", true)
        );
    }

    [Fact]
    public void Supports_Property_FromSource()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Num1 = e2.Num1,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<PropertyValue>()
            .Which.Should().BePropertyValue("Num1", false)
        );
    }

    [Fact]
    public void Supports_DateTime_Now()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Updated = DateTime.Now,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Updated")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().BeOfType<DateTime>().Subject
            .Should().BeBefore(DateTime.Now.AddMinutes(1))
            .And.BeAfter(DateTime.Now.AddMinutes(-1))
        );
    }

    [Fact]
    public void Supports_Nullable_Assign()
    {
        int value = 5;

        var result = Parse((e1, e2) => new TestEntity {
            NumNullable1 = value,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("NumNullable1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().BeOfType<int>().And.Be(value)
        );
    }

    [Fact]
    public void Supports_Nullable_Cast()
    {
        int? value = 5;

        var result = Parse((e1, e2) => new TestEntity {
            Num1 = (int) value,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().BeOfType<int>().And.Be(value)
        );
    }

    [Fact]
    public void Supports_Nullable_Coalesce()
    {
        int? value = 5;

        var result = Parse((e1, e2) => new TestEntity {
            Num1 = value ?? 0,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().BeOfType<int>().And.Be(value)
        );
    }

    [Fact]
    public void Supports_Nullable_GetValueOrDefault()
    {
        int? value = 5;

        var result = Parse((e1, e2) => new TestEntity {
            Num1 = value.GetValueOrDefault(),
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().BeOfType<int>().And.Be(value)
        );
    }

    [Fact]
    public void Supports_UnsupportedExpression_With_Compile()
    {
        var input = 5;
        Expression<Func<TestEntity, TestEntity, TestEntity>> exp = (a, e1) => new TestEntity {
            Num1 = input << 4,
        };

        var action = () => Parse(exp);
        action.Should().Throw<UnsupportedExpressionException>();

        var result = Parse(exp, useExpressionCompiler: true);

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().BeOfType<int>().And.Be(input << 4)
        );
    }

    [Fact]
    public void CompoundExpression_Sum()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Text1 = e1.Text1 + "." + e2.Text2,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Text1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Add)
            .HaveKnownExpression(e => e.Value1, ExpressionType.Add, ke => ke
                .HavePropertyValue(e => e.Value1, "Text1", true)
                .HaveConstantValue(e => e.Value2, "."))
            .HavePropertyValue(e => e.Value2, "Text2", false)
        );
    }

    [Fact]
    public void CompoundExpression_Sum_Grouped1()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Text1 = (e1.Text1 + ".") + e2.Text2,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Text1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Add)
            .HaveKnownExpression(e => e.Value1, ExpressionType.Add, ke => ke
                .HavePropertyValue(e => e.Value1, "Text1", true)
                .HaveConstantValue(e => e.Value2, "."))
            .HavePropertyValue(e => e.Value2, "Text2", false)
        );
    }

    [Fact]
    public void CompoundExpression_Sum_Grouped2()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Text1 = e1.Text1 + ("." + e2.Text2),
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Text1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Add)
            .HavePropertyValue(e => e.Value1, "Text1", true)
            .HaveKnownExpression(e => e.Value2, ExpressionType.Add, ke => ke
                .HaveConstantValue(e => e.Value1, ".")
                .HavePropertyValue(e => e.Value2, "Text2", false))
        );
    }

    [Fact]
    public void CompoundExpression_Multiply()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Num1 = e1.Num1 + 7 * e2.Num2,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Add)
            .HavePropertyValue(e => e.Value1, "Num1", true)
            .HaveKnownExpression(e => e.Value2, ExpressionType.Multiply, ke => ke
                .HaveConstantValue(e => e.Value1, 7)
                .HavePropertyValue(e => e.Value2, "Num2", false))
        );
    }

    [Fact]
    public void CompoundExpression_Multiply_Grouped1()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Num1 = (e1.Num1 + 7) * e2.Num2,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Multiply)
            .HaveKnownExpression(e => e.Value1, ExpressionType.Add, ke => ke
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HaveConstantValue(e => e.Value2, 7))
            .HavePropertyValue(e => e.Value2, "Num2", false)
        );
    }

    [Fact]
    public void CompoundExpression_Multiply_Grouped2()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Num1 = e1.Num1 + (7 * e2.Num2),
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Add)
            .HavePropertyValue(e => e.Value1, "Num1", true)
            .HaveKnownExpression(e => e.Value2, ExpressionType.Multiply, ke => ke
                .HaveConstantValue(e => e.Value1, 7)
                .HavePropertyValue(e => e.Value2, "Num2", false))
        );
    }

    [Fact]
    public void CompoundExpression_Conditional()
    {
        var result = Parse((a, b) => new TestEntity {
            Num1 = a.Num1 + 7 < 0 ? 0 : a.Num1 + 7,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Conditional)
            .HaveConstantValue(e => e.Value1, 0)
            .HaveKnownExpression(e => e.Value2, ExpressionType.Add, ke => ke
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HaveConstantValue(e => e.Value2, 7))
            .HaveKnownExpression(e => e.Value3, ExpressionType.LessThan, ke => ke
                .HaveKnownExpression(e => e.Value1, ExpressionType.Add, ke2 => ke2
                    .HavePropertyValue(e => e.Value1, "Num1", true)
                    .HaveConstantValue(e => e.Value2, 7))
                .HaveConstantValue(e => e.Value2, 0))
        );
    }

    [Fact]
    public void CompoundExpression_Conditional_NotEqual()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Num1 = e1.Num1 != 4 ? 0 : 1,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Conditional)
            .HaveConstantValue(e => e.Value1, 0)
            .HaveConstantValue(e => e.Value2, 1)
            .HaveKnownExpression(e => e.Value3, ExpressionType.NotEqual, ke => ke
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HaveConstantValue(e => e.Value2, 4))
        );
    }

    [Fact]
    public void CompoundExpression_Conditional_NotNull()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Num1 = e1.Text1 != null ? 0 : 1,
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.Conditional)
            .HaveConstantValue(e => e.Value1, 0)
            .HaveConstantValue(e => e.Value2, 1)
            .HaveKnownExpression(e => e.Value3, ExpressionType.NotEqual, ke => ke
                .HavePropertyValue(e => e.Value1, "Text1", true)
                .HaveConstantValue(e => e.Value2, null))
        );
    }

    [Fact]
    public void Condition_AndAlso()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Boolean = e1.Num1 != e2.Num1 && e1.Text1 != e2.Text1
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Boolean")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.AndAlso)
            .HaveKnownExpression(e => e.Value1, ExpressionType.NotEqual, ke => ke
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HavePropertyValue(e => e.Value2, "Num1", false))
            .HaveKnownExpression(e => e.Value2, ExpressionType.NotEqual, ke => ke
                .HavePropertyValue(e => e.Value1, "Text1", true)
                .HavePropertyValue(e => e.Value2, "Text1", false))
        );
    }

    [Fact]
    public void Condition_ElseIf()
    {
        var result = Parse((e1, e2) => new TestEntity {
            Boolean = e1.Num1 != e2.Num1 || e1.Text1 != e2.Text1
        });

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Boolean")
            .WithValueOfType<KnownExpression>()
            .Which.Should().BeKnownExpression(ExpressionType.OrElse)
            .HaveKnownExpression(e => e.Value1, ExpressionType.NotEqual, ke => ke
                .HavePropertyValue(e => e.Value1, "Num1", true)
                .HavePropertyValue(e => e.Value2, "Num1", false))
            .HaveKnownExpression(e => e.Value2, ExpressionType.NotEqual, ke => ke
                .HavePropertyValue(e => e.Value1, "Text1", true)
                .HavePropertyValue(e => e.Value2, "Text1", false))
        );
    }
}
