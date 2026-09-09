namespace SmartPackingSolution.Models;

/// <summary>
/// Represents the 3D dimensions of a physical object.
/// </summary>
/// <param name="Length">The length dimension in centimeters.</param>
/// <param name="Width">The width dimension in centimeters.</param>
/// <param name="Height">The height dimension in centimeters.</param>
public record Dimensions(double Length, double Width, double Height)
{
    /// <summary>
    /// Gets the total volume in cubic centimeters.
    /// </summary>
    public double Volume => Length * Width * Height;

    /// <summary>
    /// Gets the area of the base (length by width) in square centimeters.
    /// </summary>
    public double BaseArea => Length * Width;

    /// <summary>
    /// Gets the largest of the three dimensions.
    /// </summary>
    public double LongestSide => Math.Max(Length, Math.Max(Width, Height));

    /// <summary>
    /// Gets the smallest of the three dimensions.
    /// </summary>
    public double ShortestSide => Math.Min(Length, Math.Min(Width, Height));

    /// <summary>
    /// Validates that all dimensions are positive, finite values.
    /// </summary>
    /// <returns>True if all dimensions are greater than zero.</returns>
    public bool IsValid() =>
        Length > 0 && Width > 0 && Height > 0 &&
        double.IsFinite(Length) && double.IsFinite(Width) && double.IsFinite(Height);

    /// <summary>
    /// Creates a rotated copy of these dimensions.
    /// </summary>
    /// <param name="orientation">The orientation to apply.</param>
    /// <returns>New dimensions with the axes permuted.</returns>
    public Dimensions Rotate(Orientation orientation) => orientation switch
    {
        Orientation.LWH => this,
        Orientation.WLH => new Dimensions(Width, Length, Height),
        Orientation.LHW => new Dimensions(Length, Height, Width),
        Orientation.HLW => new Dimensions(Height, Length, Width),
        Orientation.WHL => new Dimensions(Width, Height, Length),
        Orientation.HWL => new Dimensions(Height, Width, Length),
        _ => this
    };

    /// <summary>
    /// Determines whether these dimensions fit inside <paramref name="other"/> in at
    /// least one of the six axis-aligned orientations.
    /// </summary>
    /// <param name="other">The enclosing dimensions.</param>
    /// <param name="tolerance">Slack allowed on each axis, to absorb rounding error.</param>
    /// <returns>True if some orientation fits.</returns>
    /// <remarks>
    /// Comparing the two sorted dimension triples pairwise answers this exactly, without
    /// enumerating orientations: the smallest side must fit the smallest, and so on.
    /// </remarks>
    public bool FitsInsideInSomeOrientation(Dimensions other, double tolerance = 0)
    {
        var mine = SortedAscending();
        var theirs = other.SortedAscending();

        return mine[0] <= theirs[0] + tolerance
            && mine[1] <= theirs[1] + tolerance
            && mine[2] <= theirs[2] + tolerance;
    }

    /// <summary>
    /// Returns the three dimensions sorted from smallest to largest.
    /// </summary>
    /// <returns>A three-element array in ascending order.</returns>
    public double[] SortedAscending()
    {
        var sorted = new[] { Length, Width, Height };
        Array.Sort(sorted);
        return sorted;
    }
}
