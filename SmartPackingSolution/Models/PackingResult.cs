namespace SmartPackingSolution.Models;

/// <summary>
/// Represents the result of a packing optimization operation.
/// </summary>
public class PackingResult
{
    /// <summary>
    /// Gets the list of successfully packed packages.
    /// </summary>
    public IReadOnlyList<PlacedPackage> PackedItems { get; }

    /// <summary>
    /// Gets the list of packages that could not be packed.
    /// </summary>
    public IReadOnlyList<PackageItem> UnpackedItems { get; }

    /// <summary>
    /// Gets the container used for packing.
    /// </summary>
    public Container Container { get; }

    /// <summary>
    /// Gets the percentage of container volume utilized (0.0 to 1.0).
    /// </summary>
    public double SpaceUtilization { get; }

    /// <summary>
    /// Gets the total weight of all packed items in kilograms.
    /// </summary>
    public double TotalWeight { get; }

    /// <summary>
    /// Gets whether all items were successfully packed.
    /// </summary>
    public bool IsFullyPacked => UnpackedItems.Count == 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackingResult"/> class.
    /// </summary>
    /// <param name="container">The container used.</param>
    /// <param name="packedItems">The successfully packed items.</param>
    /// <param name="unpackedItems">The items that could not be packed.</param>
    public PackingResult(
        Container container,
        IEnumerable<PlacedPackage> packedItems,
        IEnumerable<PackageItem> unpackedItems)
    {
        Container = container ?? throw new ArgumentNullException(nameof(container));
        PackedItems = packedItems?.ToList() ?? throw new ArgumentNullException(nameof(packedItems));
        UnpackedItems = unpackedItems?.ToList() ?? throw new ArgumentNullException(nameof(unpackedItems));

        TotalWeight = PackedItems.Sum(p => p.Package.Weight);
        var usedVolume = PackedItems.Sum(p => p.Package.Volume);
        SpaceUtilization = container.Volume > 0 ? usedVolume / container.Volume : 0;
    }
}