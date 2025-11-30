namespace SmartPackingSolution.Models;

/// <summary>
/// Defines the possible rotation configurations for a package.
/// </summary>
public enum RotationType
{
    /// <summary>
    /// No rotation applied - original orientation.
    /// </summary>
    None = 0,

    /// <summary>
    /// Rotate around Z axis - swap Length and Width.
    /// </summary>
    RotateXY = 1,

    /// <summary>
    /// Rotate around Y axis - swap Length and Height.
    /// </summary>
    RotateXZ = 2,

    /// <summary>
    /// Rotate around X axis - swap Width and Height.
    /// </summary>
    RotateYZ = 3
}