namespace SmartPackingSolution.Models;

using SmartPackingSolution.Geometry;

/// <summary>
/// Represents a package that has been successfully placed in a container.
/// </summary>
public class PlacedPackage
{
    /// <summary>
    /// Gets the original package item.
    /// </summary>
    public PackageItem Package { get; }

    /// <summary>
    /// Gets the position of the package's minimum corner within the container.
    /// </summary>
    public Position Position { get; }

    /// <summary>
    /// Gets the orientation the package was placed in.
    /// </summary>
    public Orientation Orientation { get; }

    /// <summary>
    /// Gets the package's dimensions as oriented in the container.
    /// </summary>
    public Dimensions ActualDimensions { get; }

    /// <summary>
    /// Gets the axis-aligned bounding box occupied by this package.
    /// </summary>
    public AxisAlignedBox Bounds { get; }

    /// <summary>
    /// Gets the height of the package's top face within the container.
    /// </summary>
    public double TopZ => Bounds.MaxZ;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlacedPackage"/> class.
    /// </summary>
    /// <param name="package">The package that was placed.</param>
    /// <param name="position">The position in the container.</param>
    /// <param name="orientation">The orientation applied.</param>
    public PlacedPackage(PackageItem package, Position position, Orientation orientation = Orientation.LWH)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        Position = position ?? throw new ArgumentNullException(nameof(position));
        Orientation = orientation;
        ActualDimensions = package.Dimensions.Rotate(orientation);
        Bounds = new AxisAlignedBox(position, ActualDimensions);
    }

    /// <summary>
    /// Checks if this package overlaps with another placed package.
    /// </summary>
    /// <param name="other">The other placed package to check.</param>
    /// <param name="tolerance">Contact slack; packages that merely touch do not overlap.</param>
    /// <returns>True if the packages share interior volume.</returns>
    public bool OverlapsWith(PlacedPackage other, double tolerance = 1e-6)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Bounds.Overlaps(other.Bounds, tolerance);
    }

    /// <summary>
    /// Determines whether this package rests directly on top of another, in the sense
    /// that their footprints overlap and this package's base meets the other's top face.
    /// </summary>
    /// <param name="other">The package that may be underneath.</param>
    /// <param name="tolerance">How close the two faces must be to count as contact.</param>
    /// <returns>True if this package is supported by <paramref name="other"/>.</returns>
    public bool RestsOn(PlacedPackage other, double tolerance = 1e-6)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Math.Abs(Bounds.MinZ - other.Bounds.MaxZ) <= tolerance
            && Bounds.FootprintIntersectionArea(other.Bounds) > tolerance;
    }
}
