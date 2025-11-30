namespace SmartPackingSolution.Models;

/// <summary>
/// Represents a 3D position within a container.
/// </summary>
/// <param name="X">The X coordinate in centimeters.</param>
/// <param name="Y">The Y coordinate in centimeters.</param>
/// <param name="Z">The Z coordinate (height) in centimeters.</param>
public record Position(double X, double Y, double Z)
{
    /// <summary>
    /// Gets the origin position (0, 0, 0).
    /// </summary>
    public static Position Origin => new(0, 0, 0);

    /// <summary>
    /// Calculates the distance from this position to another position.
    /// </summary>
    /// <param name="other">The target position.</param>
    /// <returns>The Euclidean distance between positions.</returns>
    public double DistanceTo(Position other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}