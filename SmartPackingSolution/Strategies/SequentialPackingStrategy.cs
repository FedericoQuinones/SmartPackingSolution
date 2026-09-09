namespace SmartPackingSolution.Strategies;

using SmartPackingSolution.Models;

/// <summary>
/// Base class for strategies that offer packages one at a time in a fixed order and take
/// the placement the engine reports.
/// </summary>
/// <remarks>
/// Everything physical - overlap, support, load bearing, orientation - lives in
/// <see cref="PackingContext"/>. A subclass supplies only an ordering and whether to take
/// the first feasible placement or the best-scoring one.
/// </remarks>
public abstract class SequentialPackingStrategy : IPackingStrategy
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <summary>
    /// Gets whether to accept the first feasible placement rather than scoring all of them.
    /// </summary>
    protected abstract bool FirstFit { get; }

    /// <summary>
    /// Applies the strategy's own defaults on top of the caller's options.
    /// </summary>
    /// <param name="options">The caller's options.</param>
    /// <returns>The options this run will use.</returns>
    protected virtual PackingOptions Configure(PackingOptions options) => options;

    /// <inheritdoc/>
    public PackingResult Pack(
        Container container,
        IEnumerable<PackageItem> packages,
        PackingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(packages);

        var effective = Configure(options ?? PackingOptions.Default);
        var items = packages.ToList();
        var context = new PackingContext(container, effective, items);
        var unpacked = new List<UnpackedPackage>();

        foreach (var item in ItemOrderings.Apply(items, effective.Ordering))
        {
            if (context.IsOutOfTime)
            {
                unpacked.Add(new UnpackedPackage(
                    item,
                    UnpackedReason.TimeBudgetExceeded,
                    "Packing stopped before this package was considered."));
                continue;
            }

            var (placement, reason) = context.FindPlacement(item, FirstFit);

            if (placement is not null)
            {
                context.Commit(placement);
            }
            else
            {
                unpacked.Add(new UnpackedPackage(item, reason, DescribeRejection(reason, item)));
            }
        }

        return new PackingResult(container, context.Placed, unpacked, Name, context.Elapsed);
    }

    private static string DescribeRejection(UnpackedReason reason, PackageItem item) => reason switch
    {
        UnpackedReason.TooLargeForContainer =>
            $"'{item.Name}' does not fit the container in any of its six orientations.",
        UnpackedReason.WeightCapacityExceeded =>
            $"Adding '{item.Name}' ({item.Weight}kg) would exceed the container's weight capacity.",
        UnpackedReason.SupportConstraint =>
            $"'{item.Name}' fits geometrically, but every position left it unsupported from below.",
        UnpackedReason.LoadBearingConstraint =>
            $"'{item.Name}' fits geometrically, but every position would overload a package beneath it.",
        UnpackedReason.TimeBudgetExceeded =>
            "Packing stopped before this package was considered.",
        _ => $"No remaining free space could accommodate '{item.Name}'."
    };
}
