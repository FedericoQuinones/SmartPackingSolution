namespace SmartPackingSolution.Models;

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
    /// Gets the position where the package was placed.
    /// </summary>
    public Position Position { get; }

    /// <summary>
    /// Gets the rotation applied to the package.
    /// </summary>
    public RotationType Rotation { get; }

    /// <summary>
    /// Gets the actual dimensions after rotation.
    /// </summary>
    public Dimensions ActualDimensions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlacedPackage"/> class.
    /// </summary>
    /// <param name="package">The package that was placed.</param>
    /// <param name="position">The position in the container.</param>
    /// <param name="rotation">The rotation applied.</param>
    public PlacedPackage(PackageItem package, Position position, RotationType rotation = RotationType.None)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        Position = position ?? throw new ArgumentNullException(nameof(position));
        Rotation = rotation;
        ActualDimensions = package.Dimensions.Rotate(rotation);
    }

    /// <summary>
    /// Checks if this package overlaps with another placed package.
    /// </summary>
    /// <param name="other">The other placed package to check.</param>
    /// <returns>True if the packages overlap in 3D space.</returns>
    public bool OverlapsWith(PlacedPackage other)
    {
        var x1Min = Position.X;
        var x1Max = Position.X + ActualDimensions.Length;
        var y1Min = Position.Y;
        var y1Max = Position.Y + ActualDimensions.Width;
        var z1Min = Position.Z;
        var z1Max = Position.Z + ActualDimensions.Height;

        var x2Min = other.Position.X;
        var x2Max = other.Position.X + other.ActualDimensions.Length;
        var y2Min = other.Position.Y;
        var y2Max = other.Position.Y + other.ActualDimensions.Width;
        var z2Min = other.Position.Z;
        var z2Max = other.Position.Z + other.ActualDimensions.Height;

        return x1Min < x2Max && x1Max > x2Min &&
               y1Min < y2Max && y1Max > y2Min &&
               z1Min < z2Max && z1Max > z2Min;
    }
}