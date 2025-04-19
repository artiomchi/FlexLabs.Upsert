using System;
using System.Collections.Generic;
using System.Linq;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;


namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Internal;

public class ExpressionTests(ITestOutputHelper output) {
    private readonly ExpressionParser<TestEntity> _parser = new(new TestRelationalTable(), new RunnerQueryOptions());


    [Fact]
    public void supports_constant()
    {
        var result = _parser.ParseUpdaterExpression((a, b) => new TestEntity {
            Num1 = 1,
        }).ToList();

        Print(result);

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<ConstantValue>()
            .Which.Value.Should().Be(1)
        );
    }

    [Fact]
    public void supports_property()
    {
        var result = _parser.ParseUpdaterExpression((a, b) => new TestEntity {
            Num1 = b.Num1,
        }).ToList();

        Print(result);

        result[0].Should().BePropertyMapping(_ => _
            .WithColumn("Num1")
            .WithValueOfType<PropertyValue>()
            .Which.Should().BePropertyValue("Num1", isLeftParam: false)
        );
    }


    #region private

    private void Print(IEnumerable<PropertyMapping> mappings)
    {
        output.WriteLine("new TestEntity {");

        foreach (var mapping in mappings) {
            output.WriteLine($"  {mapping.Property.ColumnName} = {mapping.Value switch {
                ConstantValue x => x.Value,
                PropertyValue x => $"{(x.IsLeftParameter ? "a" : "b")}{x.Property.Path}.{x.Property.Name}",
                _ => throw new ArgumentOutOfRangeException(nameof(mapping.Value), mapping.Value, null)
            }},");
        }

        output.WriteLine("}");
    }

    #endregion
}
