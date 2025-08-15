namespace FlexLabs.EntityFrameworkCore.Upsert.Internal;

/// <summary>
/// Represents various types of owned properties.
/// </summary>
public enum OwnershipType
{
    /// <summary>
    /// Not owned.
    /// </summary>
    None,
    /// <summary>
    /// Owned and inlined into the table.
    /// </summary>
    Inline,
    /// <summary>
    /// Owner of inlined properties.
    /// </summary>
    InlineOwner,
    /// <summary>
    /// Owned with json conversation.
    /// </summary>
    Json,
}
