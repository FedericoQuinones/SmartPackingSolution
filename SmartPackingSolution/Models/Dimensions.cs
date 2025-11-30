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
    /// Calculates the total volume of the dimensions.
    /// </summary>
    /// <returns>The volume in cubic centimeters.</returns>
    public double Volume => Length * Width * Height;

    /// <summary>
    /// Validates that all dimensions are positive values.
    /// </summary>
    /// <returns>True if all dimensions are greater than zero.</returns>
    public bool IsValid() => Length > 0 && Width > 0 && Height > 0;

    /// <summary>
    /// Creates a rotated version of these dimensions.
    /// </summary>
    /// <param name="rotation">The rotation configuration to apply.</param>
    /// <returns>New dimensions with rotated values.</returns>
    public Dimensions Rotate(RotationType rotation)
    {
        return rotation switch
        {
            RotationType.None => this,
            RotationType.RotateXY => new Dimensions(Width, Length, Height),
            RotationType.RotateXZ => new Dimensions(Height, Width, Length),
            RotationType.RotateYZ => new Dimensions(Length, Height, Width),
            _ => this
        };
    }
}