namespace SmartPackingSolution.Validation;

using SmartPackingSolution.Geometry;
using SmartPackingSolution.Models;
using SmartPackingSolution.Strategies;

/// <summary>
/// Checks a finished layout against the physical constraints the packer claims to honour.
/// </summary>
/// <remarks>
/// The validator deliberately re-derives everything from the placements alone, sharing no
/// state with the packer that produced them. That independence is the point: it turns
/// "the algorithm respects support and load bearing" from an assertion about the code
/// into a property that can be tested on any layout, including ones from other packers.
/// </remarks>
public static class PackingValidator
{
    /// <summary>
    /// Validates a packing result.
    /// </summary>
    /// <param name="result">The layout to check.</param>
    /// <param name="options">
    /// The settings the layout was produced under, which set the tolerance and the
    /// required support ratio. Defaults are used when null.
    /// </param>
    /// <returns>A report listing every violation found.</returns>
    public static PackingValidationReport Validate(PackingResult result, PackingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        options ??= PackingOptions.Default;
        var tolerance = Math.Max(options.Tolerance, 1e-6);
        var violations = new List<PackingViolation>();
        var placed = result.PackedItems;

        if (result.TotalWeight > result.Container.MaxWeight + tolerance)
        {
            violations.Add(new PackingViolation(
                ViolationKind.ContainerOverweight,
                $"Packed weight {result.TotalWeight:F2}kg exceeds container capacity " +
                $"{result.Container.MaxWeight:F2}kg."));
        }

        for (var i = 0; i < placed.Count; i++)
        {
            var item = placed[i];

            if (!item.Bounds.IsInside(result.Container.Dimensions, tolerance))
            {
                violations.Add(new PackingViolation(
                    ViolationKind.OutOfBounds,
                    $"'{item.Package.Name}' at ({item.Position.X:F1}, {item.Position.Y:F1}, " +
                    $"{item.Position.Z:F1}) extends outside the container.",
                    item));
            }

            for (var j = i + 1; j < placed.Count; j++)
            {
                if (item.Bounds.Overlaps(placed[j].Bounds, tolerance))
                {
                    violations.Add(new PackingViolation(
                        ViolationKind.Overlap,
                        $"'{item.Package.Name}' overlaps '{placed[j].Package.Name}'.",
                        item,
                        placed[j]));
                }
            }
        }

        if (options.RequireSupport)
        {
            violations.AddRange(FindUnsupported(placed, options.MinimumSupportRatio, tolerance));
        }

        if (options.EnforceLoadBearing)
        {
            violations.AddRange(FindOverloaded(placed, tolerance));
        }

        return new PackingValidationReport(violations);
    }

    private static IEnumerable<PackingViolation> FindUnsupported(
        IReadOnlyList<PlacedPackage> placed,
        double minimumRatio,
        double tolerance)
    {
        foreach (var item in placed)
        {
            if (item.Bounds.MinZ <= tolerance)
            {
                continue;
            }

            var supporters = placed
                .Where(other => !ReferenceEquals(other, item))
                .Select(other => other.Bounds)
                .ToList();

            var ratio = SupportCalculator.SupportRatio(item.Bounds, supporters, tolerance);

            if (ratio < minimumRatio - tolerance)
            {
                yield return new PackingViolation(
                    ViolationKind.Floating,
                    $"'{item.Package.Name}' at height {item.Position.Z:F1}cm has only " +
                    $"{ratio:P0} of its base supported (minimum {minimumRatio:P0}).",
                    item);
            }
        }
    }

    private static IEnumerable<PackingViolation> FindOverloaded(
        IReadOnlyList<PlacedPackage> placed,
        double tolerance)
    {
        var load = ComputeLoads(placed, tolerance);

        for (var i = 0; i < placed.Count; i++)
        {
            var item = placed[i];
            var capacity = item.Package.MaxSupportedWeight;

            if (load[i] > capacity + tolerance)
            {
                var kind = item.Package.Priority == PackagePriority.Fragile
                    ? ViolationKind.StackedOnFragile
                    : ViolationKind.Overloaded;

                yield return new PackingViolation(
                    kind,
                    $"'{item.Package.Name}' carries {load[i]:F2}kg but supports only " +
                    $"{capacity:F2}kg.",
                    item);
            }
        }
    }

    /// <summary>
    /// Computes the weight resting on every placement by settling the load downwards.
    /// </summary>
    /// <param name="placed">The placements to analyse.</param>
    /// <param name="tolerance">How close two faces must be to count as contact.</param>
    /// <returns>The load in kilograms carried by each placement, indexed alongside it.</returns>
    /// <remarks>
    /// Items are processed from the top down, so by the time an item is reached, every
    /// item resting on it has already passed its own weight plus whatever it carries
    /// downwards. Each item splits its load between its supporters in proportion to the
    /// contact area each provides.
    /// </remarks>
    private static double[] ComputeLoads(IReadOnlyList<PlacedPackage> placed, double tolerance)
    {
        var load = new double[placed.Count];
        var order = Enumerable.Range(0, placed.Count)
            .OrderByDescending(i => placed[i].Bounds.MinZ)
            .ToArray();

        foreach (var i in order)
        {
            var item = placed[i];
            var transmitted = item.Package.Weight + load[i];

            var contacts = new List<(int Index, double Area)>();
            var totalArea = 0.0;

            for (var j = 0; j < placed.Count; j++)
            {
                if (j == i)
                {
                    continue;
                }

                if (Math.Abs(placed[j].Bounds.MaxZ - item.Bounds.MinZ) > tolerance)
                {
                    continue;
                }

                var area = item.Bounds.FootprintIntersectionArea(placed[j].Bounds);
                if (area > tolerance)
                {
                    contacts.Add((j, area));
                    totalArea += area;
                }
            }

            if (totalArea <= tolerance)
            {
                continue;
            }

            foreach (var (index, area) in contacts)
            {
                load[index] += transmitted * (area / totalArea);
            }
        }

        return load;
    }
}
