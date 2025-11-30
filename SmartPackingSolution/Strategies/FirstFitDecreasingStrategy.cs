namespace SmartPackingSolution.Strategies;

using SmartPackingSolution.Models;

/// <summary>
/// Implements a First-Fit Decreasing packing strategy.
/// Sorts items by volume (largest first) and places each in the first available position.
/// Time Complexity: O(n² log n)
/// </summary>
public class FirstFitDecreasingStrategy : IPackingStrategy
{
    private readonly double _tolerance = 0.001;

    /// <inheritdoc/>
    public PackingResult Pack(Container container, IEnumerable<PackageItem> packages)
    {
        var sortedPackages = packages
            .OrderByDescending(p => p.Priority)
            .ThenByDescending(p => p.Volume)
            .ToList();

        var placedPackages = new List<PlacedPackage>();
        var unpackedItems = new List<PackageItem>();
        var totalWeight = 0.0;

        foreach (var package in sortedPackages)
        {
            if (totalWeight + package.Weight > container.MaxWeight)
            {
                unpackedItems.Add(package);
                continue;
            }

            var placement = FindFirstFitPosition(container, package, placedPackages);

            if (placement != null)
            {
                placedPackages.Add(placement);
                totalWeight += package.Weight;
            }
            else
            {
                unpackedItems.Add(package);
            }
        }

        return new PackingResult(container, placedPackages, unpackedItems);
    }

    private PlacedPackage? FindFirstFitPosition(
        Container container,
        PackageItem package,
        List<PlacedPackage> existingPackages)
    {
        var rotations = package.AllowRotation
            ? new[] { RotationType.None, RotationType.RotateXY, RotationType.RotateXZ, RotationType.RotateYZ }
            : new[] { RotationType.None };

        var positions = GenerateCandidatePositions(existingPackages, container);

        foreach (var position in positions.OrderBy(p => p.Z).ThenBy(p => p.X).ThenBy(p => p.Y))
        {
            foreach (var rotation in rotations)
            {
                var dimensions = package.Dimensions.Rotate(rotation);

                if (!FitsInContainer(position, dimensions, container))
                {
                    continue;
                }

                var testPlacement = new PlacedPackage(package, position, rotation);

                if (IsValidPlacement(testPlacement, existingPackages))
                {
                    return testPlacement;
                }
            }
        }

        return null;
    }

    private List<Position> GenerateCandidatePositions(List<PlacedPackage> existingPackages, Container container)
    {
        var positions = new HashSet<Position> { Position.Origin };

        foreach (var placed in existingPackages)
        {
            positions.Add(new Position(
                placed.Position.X + placed.ActualDimensions.Length,
                placed.Position.Y,
                placed.Position.Z));

            positions.Add(new Position(
                placed.Position.X,
                placed.Position.Y + placed.ActualDimensions.Width,
                placed.Position.Z));

            positions.Add(new Position(
                placed.Position.X,
                placed.Position.Y,
                placed.Position.Z + placed.ActualDimensions.Height));
        }

        return positions.ToList();
    }

    private bool FitsInContainer(Position position, Dimensions dimensions, Container container)
    {
        return position.X + dimensions.Length <= container.Dimensions.Length + _tolerance &&
               position.Y + dimensions.Width <= container.Dimensions.Width + _tolerance &&
               position.Z + dimensions.Height <= container.Dimensions.Height + _tolerance;
    }

    private bool IsValidPlacement(PlacedPackage newPlacement, List<PlacedPackage> existingPackages)
    {
        foreach (var existing in existingPackages)
        {
            if (newPlacement.OverlapsWith(existing))
            {
                return false;
            }

            if (!CanStackOn(newPlacement, existing))
            {
                return false;
            }
        }

        return true;
    }

    private bool CanStackOn(PlacedPackage upper, PlacedPackage lower)
    {
        if (upper.Position.Z <= lower.Position.Z + lower.ActualDimensions.Height - _tolerance)
        {
            return true;
        }

        var upperPriority = upper.Package.Priority;
        var lowerPriority = lower.Package.Priority;

        if (lowerPriority == PackagePriority.Fragile)
        {
            return false;
        }

        if (upperPriority == PackagePriority.Heavy && lowerPriority != PackagePriority.Heavy)
        {
            return false;
        }

        return true;
    }
}