namespace SmartPackingSolution.Models;

/// <summary>
/// Defines the priority and stacking constraints for packages.
/// </summary>
public enum PackagePriority
{
    /// <summary>
    /// Fragile items that cannot support any weight above them.
    /// Examples: glass bottles, electronics, delicate items.
    /// </summary>
    Fragile = 0,

    /// <summary>
    /// Light items that can be placed on top of medium or heavy items.
    /// Examples: pillows, clothing, foam items.
    /// </summary>
    Light = 1,

    /// <summary>
    /// Standard items with moderate strength.
    /// Can support light items but not heavy ones.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Heavy and sturdy items that should be placed at the bottom.
    /// Can support all other types. Examples: furniture, appliances.
    /// </summary>
    Heavy = 3
}