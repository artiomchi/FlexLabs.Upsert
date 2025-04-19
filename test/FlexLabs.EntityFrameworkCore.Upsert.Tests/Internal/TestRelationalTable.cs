using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using IColumnBase = FlexLabs.EntityFrameworkCore.Upsert.Internal.IColumnBase;


#nullable enable

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Internal;

internal class TestRelationalTable : RelationalTableBase {
    public TestRelationalTable()
    {
        AddColumnRange([
            Column<TestEntity>(_ => _.ID),
            Column<TestEntity>(_ => _.Num1),
            Column<TestEntity>(_ => _.Num2),
            Column<TestEntity>(_ => _.NumNullable1),
            Column<TestEntity>(_ => _.Text1),
            Column<TestEntity>(_ => _.Text2),
            Column<TestEntity>(_ => _.Boolean),
            Column<TestEntity>(_ => _.Updated),
            Column<TestEntity>(_ => _.Child, Owned.InlineOwner),
            Column<TestEntity>(_ => _.Child.ID, Owned.Inline),
            Column<TestEntity>(_ => _.Child.Num1, Owned.Inline),
            Column<TestEntity>(_ => _.Child.Num2, Owned.Inline),
            Column<TestEntity>(_ => _.Child.NumNullable1, Owned.Inline),
            Column<TestEntity>(_ => _.Child.Text1, Owned.Inline),
            Column<TestEntity>(_ => _.Child.Text2, Owned.Inline),
            Column<TestEntity>(_ => _.Child.Updated, Owned.Inline),
            Column<TestEntity>(_ => _.Child.NestedChild, Owned.InlineOwner),
            Column<TestEntity>(_ => _.Child.NestedChild.Num1, Owned.Inline),
            Column<TestEntity>(_ => _.Child.NestedChild.Num2, Owned.Inline),
            Column<TestEntity>(_ => _.Child.NestedChild.Text1, Owned.Inline),
        ]);
    }

    private static TestColumn Column<T>(Expression<Func<T, object?>> property, Owned owned = Owned.None)
    {
        var exp = property.Body switch {
            UnaryExpression u => u.Operand,
            var x => x,
        };

        var members = new List<PropertyInfo>();
        while (exp is MemberExpression memberExpression) {
            var member = (PropertyInfo) memberExpression.Member;
            members.Add(member);
            exp = memberExpression.Expression;
        }

        var name = members.First().Name;
        var path = string.Join(".", members.Skip(1).Reverse().Select(_ => _.Name)) switch {
            "" => null,
            var x => $".{x}",
        };

        var accessor = members.AsEnumerable().Reverse().Aggregate<PropertyInfo, Func<object?, object?>>(x => x, (c, n) => _ => c(_) switch {
            null => null,
            var x => n.GetValue(x)
        });

        return new TestColumn(name, path, owned, accessor);
    }
}

public record TestColumn(
    string Name,
    string? Path,
    Owned Owned,
    Func<object?, object?> ValueAccessor
) : IColumnBase {
    public string ColumnName => Path switch {
        null => Name,
        _ => $"{Path}.{Name}".TrimStart('.').Replace('.', '_'),
    };

    public (string ColumnName, ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(object entity)
    {
        var value = ValueAccessor(entity);
        return (ColumnName, new ConstantValue(value), DefaultSql: null, AllowInserts: true);
    }
}
