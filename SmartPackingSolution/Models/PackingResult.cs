namespace SmartPackingSolution.Models;

using SmartPackingSolution.Validation;

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
    /// Gets the packages that could not be packed, each with the reason it was rejected.
    /// </summary>
    public IReadOnlyList<UnpackedPackage> UnpackedItems { get; }

    /// <summary>
    /// Gets the container used for packing.
    /// </summary>
    public Container Container { get; }

    /// <summary>
    /// Gets the fraction of the container's volume occupied by packed items, from 0 to 1.
    /// </summary>
    public double SpaceUtilization { get; }

    /// <summary>
    /// Gets the fraction of the packed items' own bounding box that they occupy.
    /// </summary>
    /// <remarks>
    /// Where <see cref="SpaceUtilization"/> is dominated by how well the container was
    /// sized for the load, this measures how tightly the items are packed against each
    /// other - an oversized container cannot flatter it.
    /// </remarks>
    public double BoundingBoxUtilization { get; }

    /// <summary>
    /// Gets the total weight of all packed items in kilograms.
    /// </summary>
    public double TotalWeight { get; }

    /// <summary>
    /// Gets the weighted centre of gravity of the packed load.
    /// </summary>
    public Position CenterOfGravity { get; }

    /// <summary>
    /// Gets how well the load is distributed, from 0 to 1.
    /// </summary>
    /// <remarks>
    /// Half the score rewards a centre of gravity near the middle of the container's
    /// footprint, the other half rewards keeping it low. A high score means a load that
    /// will not tip or shift in transit.
    /// </remarks>
    public double LoadBalanceScore { get; }

    /// <summary>
    /// Gets the height of the highest packed item's top face, in centimeters.
    /// </summary>
    public double MaxStackHeight { get; }

    /// <summary>
    /// Gets the wall-clock time the packing run took.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// Gets the name of the strategy that produced this result.
    /// </summary>
    public string StrategyName { get; }

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
    /// <param name="strategyName">The strategy that produced the layout.</param>
    /// <param name="elapsed">How long the run took.</param>
    public PackingResult(
        Container container,
        IEnumerable<PlacedPackage> packedItems,
        IEnumerable<UnpackedPackage> unpackedItems,
        string strategyName = "unspecified",
        TimeSpan elapsed = default)
    {
        Container = container ?? throw new ArgumentNullException(nameof(container));
        PackedItems = packedItems?.ToList() ?? throw new ArgumentNullException(nameof(packedItems));
        UnpackedItems = unpackedItems?.ToList() ?? throw new ArgumentNullException(nameof(unpackedItems));
        StrategyName = strategyName;
        Elapsed = elapsed;

        TotalWeight = PackedItems.Sum(p => p.Package.Weight);

        var usedVolume = PackedItems.Sum(p => p.Package.Volume);
        SpaceUtilization = container.Volume > 0 ? usedVolume / container.Volume : 0;

        BoundingBoxUtilization = ComputeBoundingBoxUtilization(PackedItems, usedVolume);
        MaxStackHeight = PackedItems.Count == 0 ? 0 : PackedItems.Max(p => p.TopZ);
        CenterOfGravity = ComputeCenterOfGravity(PackedItems, TotalWeight);
        LoadBalanceScore = ComputeLoadBalance(container, CenterOfGravity, PackedItems.Count);
    }

    /// <summary>
    /// Checks the layout against the laws it is supposed to obey: everything inside the
    /// container, nothing overlapping, nothing floating, nothing crushed.
    /// </summary>
    /// <param name="options">
    /// The settings the layout was produced under. When null, the defaults are used.
    /// </param>
    /// <returns>A report listing every violation found.</returns>
    public PackingValidationReport Validate(Strategies.PackingOptions? options = null) =>
        PackingValidator.Validate(this, options);

    private static double ComputeBoundingBoxUtilization(
        IReadOnlyList<PlacedPackage> packed,
        double usedVolume)
    {
        if (packed.Count == 0)
        {
            return 0;
        }

        var maxX = packed.Max(p => p.Bounds.MaxX);
        var maxY = packed.Max(p => p.Bounds.MaxY);
        var maxZ = packed.Max(p => p.Bounds.MaxZ);
        var minX = packed.Min(p => p.Bounds.MinX);
        var minY = packed.Min(p => p.Bounds.MinY);
        var minZ = packed.Min(p => p.Bounds.MinZ);

        var boundingVolume = (maxX - minX) * (maxY - minY) * (maxZ - minZ);

        return boundingVolume > 0 ? usedVolume / boundingVolume : 0;
    }

    private static Position ComputeCenterOfGravity(IReadOnlyList<PlacedPackage> packed, double totalWeight)
    {
        if (packed.Count == 0 || totalWeight <= 0)
        {
            return Position.Origin;
        }

        var x = 0.0;
        var y = 0.0;
        var z = 0.0;

        foreach (var item in packed)
        {
            var center = item.Bounds.Center;
            var weight = item.Package.Weight;
            x += center.X * weight;
            y += center.Y * weight;
            z += center.Z * weight;
        }

        return new Position(x / totalWeight, y / totalWeight, z / totalWeight);
    }

    private static double ComputeLoadBalance(Container container, Position cog, int packedCount)
    {
        if (packedCount == 0)
        {
            return 0;
        }

        var halfLength = container.Dimensions.Length / 2;
        var halfWidth = container.Dimensions.Width / 2;

        var offsetX = halfLength > 0 ? Math.Abs(cog.X - halfLength) / halfLength : 0;
        var offsetY = halfWidth > 0 ? Math.Abs(cog.Y - halfWidth) / halfWidth : 0;
        var centring = 1 - ((offsetX + offsetY) / 2);

        var lowness = container.Dimensions.Height > 0
            ? 1 - (cog.Z / container.Dimensions.Height)
            : 1;

        return Math.Clamp((0.5 * centring) + (0.5 * lowness), 0, 1);
    }
}
