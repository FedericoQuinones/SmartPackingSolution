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
    /// Calculates the Euclidean distance from this position to another.
    /// </summary>
    /// <param name="other">The target position.</param>
    /// <returns>The distance between the positions.</returns>
    public double DistanceTo(Position other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Snaps each coordinate to a multiple of <paramref name="step"/>.
    /// </summary>
    /// <param name="step">The grid step; must be greater than zero.</param>
    /// <returns>The snapped position.</returns>
    /// <remarks>
    /// Candidate positions are produced by repeatedly adding package dimensions, so two
    /// anchors that represent the same physical corner can differ in their last bits.
    /// Record equality is exact, so de-duplicating raw positions silently fails and the
    /// candidate set grows without bound. Snapping to a grid first makes equality work.
    /// </remarks>
    public Position Quantize(double step) =>
        step <= 0
            ? this
            : new Position(
                Math.Round(X / step) * step,
                Math.Round(Y / step) * step,
                Math.Round(Z / step) * step);
}
