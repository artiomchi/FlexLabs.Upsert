using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Internal;

public class RelationalColumnTests
{
    public class DefaultsEntity
    {
        public int ID { get; set; }
        public int Specified { get; set; }
        public int Unspecified { get; set; }
        public int SqlDefault { get; set; }
        public int NoDefault { get; set; }
        public string Text { get; set; } = "";
    }

    private class DefaultsContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite("Data Source=:memory:");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<DefaultsEntity>();
            entity.Property(e => e.Specified).HasDefaultValue(27);
            // Parameterless: the database has a default, but its value isn't known to the model (stored as DBNull.Value)
            entity.Property(e => e.Unspecified).HasDefaultValue();
            entity.Property(e => e.SqlDefault).HasDefaultValueSql("33");
            entity.Property(e => e.Text)
                .HasDefaultValue("ready")
                .HasSentinel("unset")
                .Metadata.SetValueComparer(new ValueComparer<string>(
                    (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase),
                    v => v.ToUpperInvariant().GetHashCode()));
        }
    }

    private static (string ColumnName, FlexLabs.EntityFrameworkCore.Upsert.Internal.Expressions.ConstantValue Value, string? DefaultSql, bool AllowInserts) GetValue(string propertyName, int value)
    {
        using var context = new DefaultsContext();
        var property = context.Model.FindEntityType(typeof(DefaultsEntity))!.FindProperty(propertyName)!;
        var column = new RelationalColumn(property, propertyName, OwnershipType.None);
        var entity = new DefaultsEntity();
        typeof(DefaultsEntity).GetProperty(propertyName)!.SetValue(entity, value);
        return column.GetValue(entity);
    }

    [Fact]
    public void GetValue_SentinelWithSpecifiedDefault_UsesDefault()
    {
        var result = GetValue(nameof(DefaultsEntity.Specified), 0);
        result.Value.Value.Should().Be(27);
        result.DefaultSql.Should().BeNull();
    }

    [Fact]
    public void GetValue_NonSentinelWithSpecifiedDefault_KeepsValue()
    {
        var result = GetValue(nameof(DefaultsEntity.Specified), 5);
        result.Value.Value.Should().Be(5);
        result.DefaultSql.Should().BeNull();
    }

    [Fact]
    public void GetValue_SentinelWithSqlDefault_UsesDefaultSql()
    {
        var result = GetValue(nameof(DefaultsEntity.SqlDefault), 0);
        result.DefaultSql.Should().Be("33");
    }

    [Fact]
    public void GetValue_SentinelWithNoDefault_KeepsValue()
    {
        var result = GetValue(nameof(DefaultsEntity.NoDefault), 0);
        result.Value.Value.Should().Be(0);
        result.DefaultSql.Should().BeNull();
    }

    [Fact]
    public void GetValue_SentinelWithUnspecifiedDefault_KeepsValue()
    {
        var result = GetValue(nameof(DefaultsEntity.Unspecified), 0);
        result.Value.Value.Should().Be(0);
        result.DefaultSql.Should().BeNull();
    }

    [Theory]
    [InlineData("unset", "ready")]
    [InlineData("UNSET", "ready")] // matches the sentinel when using the property value comparer
    [InlineData("other", "other")]
    public void GetValue_SentinelUsesPropertyValueComparer(string provided, string expected)
    {
        using var context = new DefaultsContext();
        var property = context.Model.FindEntityType(typeof(DefaultsEntity))!.FindProperty(nameof(DefaultsEntity.Text))!;
        var column = new RelationalColumn(property, nameof(DefaultsEntity.Text), OwnershipType.None);

        var result = column.GetValue(new DefaultsEntity { Text = provided });

        result.Value.Value.Should().Be(expected);
    }
}
