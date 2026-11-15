using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

internal static class PropertyDefaultExtensions
{
    /// <summary>
    /// Gets the configured default value, if there is one that can be used.
    /// A parameterless HasDefaultValue() is stored as DBNull.Value. It means that the database has a default, but
    /// the model doesn't know what it is, so it's not a value that can be assigned to a CLR property or sent as a parameter.
    /// </summary>
    public static bool TryGetUsableDefaultValue(this IProperty property, out object? defaultValue)
    {
        if (property.TryGetDefaultValue(out defaultValue) && defaultValue is not null and not DBNull)
            return true;

        defaultValue = null;
        return false;
    }

    /// <summary>
    /// Checks if the value is the property sentinel, which means that the value is "not set". This uses the property
    /// value comparer, in the same way that Entity Framework does.
    /// </summary>
    public static bool IsSentinel(this IProperty property, object? value)
        => property.GetValueComparer().Equals(value, property.Sentinel);
}
