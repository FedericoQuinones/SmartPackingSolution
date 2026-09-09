namespace SmartPackingSolution.Strategies;

using SmartPackingSolution.Models;

/// <summary>
/// Fills the container in horizontal layers, completing each one before starting the next.
/// </summary>
/// <remarks>
/// The tallest remaining package sets the height of a layer; everything else that fits
/// within that band is packed alongside it before the floor moves up. For loads of
/// similar-sized boxes this beats a free-form packer, because it produces flat, fully
/// supported decks instead of a ragged surface that wastes the space above it. For loads
/// of wildly varying sizes it does worse, since short items cannot use the headroom of a
/// tall layer.
/// </remarks>
public class LayerBasedStrategy : IPackingStrategy
{
    /// <inheritdoc/>
    public string Name => "Layer-Based";

    /// <inheritdoc/>
    public PackingResult Pack(
        Container container,
        IEnumerable<PackageItem> packages,
        PackingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(packages);

        var effective = options ?? PackingOptions.Default;
        var items = packages.ToList();
        var context = new PackingContext(container, effective, items);

        var remaining = new List<PackageItem>(
            ItemOrderings.Apply(items, effective.Ordering));
        var unpacked = new List<UnpackedPackage>();

        var layerFloor = 0.0;

        while (remaining.Count > 0 && layerFloor < container.Dimensions.Height)
        {
            if (context.IsOutOfTime)
            {
                break;
            }

            var layerTop = OpenLayer(context, remaining, layerFloor, unpacked);

            if (layerTop is not null)
            {
                FillLayer(context, remaining, layerFloor, layerTop.Value);
            }

            // Advance to the lowest surface above the current deck. Taking the layer's own
            // top instead would strand every package that ended up shorter than the one
            // that set the layer height: the next deck would then only exist above that
            // single tallest package, and everything placed on it would be unsupported.
            var next = NextFloorAbove(context, layerFloor);

            if (next is null)
            {
                break;
            }

            layerFloor = next.Value;
        }

        foreach (var leftover in remaining)
        {
            unpacked.Add(new UnpackedPackage(
                leftover,
                context.IsOutOfTime ? UnpackedReason.TimeBudgetExceeded : UnpackedReason.NoSpaceAvailable,
                $"No layer had room for '{leftover.Name}'."));
        }

        return new PackingResult(container, context.Placed, unpacked, Name, context.Elapsed);
    }

    /// <summary>
    /// Places the first package that will start a layer at <paramref name="layerFloor"/>,
    /// and reports the height its top face sets for the layer.
    /// </summary>
    /// <param name="context">The container being filled.</param>
    /// <param name="remaining">Packages not yet placed; the one used is removed.</param>
    /// <param name="layerFloor">The height the layer starts at.</param>
    /// <param name="unpacked">Collects packages rejected outright.</param>
    /// <returns>The top of the new layer, or null if no package could start one.</returns>
    private static double? OpenLayer(
        PackingContext context,
        List<PackageItem> remaining,
        double layerFloor,
        List<UnpackedPackage> unpacked)
    {
        var tolerance = context.Options.Tolerance;

        for (var i = 0; i < remaining.Count; i++)
        {
            var item = remaining[i];
            var (placement, reason) = context.FindPlacement(
                item,
                firstFit: false,
                positionFilter: p => Math.Abs(p.Z - layerFloor) <= tolerance);

            if (placement is not null)
            {
                context.Commit(placement);
                remaining.RemoveAt(i);
                return placement.TopZ;
            }

            if (reason == UnpackedReason.TooLargeForContainer ||
                reason == UnpackedReason.WeightCapacityExceeded)
            {
                unpacked.Add(new UnpackedPackage(item, reason));
                remaining.RemoveAt(i);
                i--;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the lowest anchor surface strictly above the current deck.
    /// </summary>
    /// <param name="context">The container being filled.</param>
    /// <param name="layerFloor">The height of the current deck.</param>
    /// <returns>The next deck height, or null if nothing lies above.</returns>
    private static double? NextFloorAbove(PackingContext context, double layerFloor)
    {
        var tolerance = context.Options.Tolerance;
        double? best = null;

        foreach (var anchor in context.Anchors())
        {
            if (anchor.Z > layerFloor + tolerance && (best is null || anchor.Z < best.Value))
            {
                best = anchor.Z;
            }
        }

        return best;
    }

    /// <summary>
    /// Packs as much of what is left as will fit inside the current layer band.
    /// </summary>
    /// <param name="context">The container being filled.</param>
    /// <param name="remaining">Packages not yet placed; those used are removed.</param>
    /// <param name="layerFloor">The height the layer starts at.</param>
    /// <param name="layerTop">The height the layer ends at.</param>
    private static void FillLayer(
        PackingContext context,
        List<PackageItem> remaining,
        double layerFloor,
        double layerTop)
    {
        var tolerance = context.Options.Tolerance;

        for (var i = 0; i < remaining.Count; i++)
        {
            if (context.IsOutOfTime)
            {
                return;
            }

            // Anchors within the band include the tops of shorter packages already in this
            // layer, so a low item can carry another without breaching the layer ceiling.
            var (placement, _) = context.FindPlacement(
                remaining[i],
                firstFit: false,
                positionFilter: p => p.Z >= layerFloor - tolerance && p.Z < layerTop - tolerance,
                ceilingZ: layerTop);

            if (placement is null)
            {
                continue;
            }

            context.Commit(placement);
            remaining.RemoveAt(i);
            i--;
        }
    }
}
