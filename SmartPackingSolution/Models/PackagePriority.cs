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

/// <summary>
/// Extension methods that turn a <see cref="PackagePriority"/> into the physical
/// quantities the packing engine works with.
/// </summary>
public static class PackagePriorityExtensions
{
    /// <summary>
    /// Gets the weight, in kilograms, that a package of this priority may carry on top
    /// of it when no explicit limit is supplied.
    /// </summary>
    /// <param name="priority">The package priority.</param>
    /// <returns>The default load capacity in kilograms.</returns>
    /// <remarks>
    /// <see cref="PackagePriority.Fragile"/> maps to zero, which is what actually
    /// enforces "nothing may rest on a fragile item" - and, unlike a priority
    /// comparison, it only applies to items genuinely stacked above it.
    /// </remarks>
    public static double DefaultLoadCapacity(this PackagePriority priority) => priority switch
    {
        PackagePriority.Fragile => 0,
        PackagePriority.Light => 5,
        PackagePriority.Medium => 30,
        PackagePriority.Heavy => 200,
        _ => 0
    };
}
