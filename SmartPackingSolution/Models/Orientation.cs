namespace SmartPackingSolution.Models;

/// <summary>
/// Defines how a package's own dimensions map onto the container axes.
/// </summary>
/// <remarks>
/// A rectangular box has exactly six axis-aligned orientations (the 3! permutations
/// of its three dimensions). Each name lists which of the package's original
/// dimensions - <c>L</c>ength, <c>W</c>idth, <c>H</c>eight - ends up on the X, Y and
/// Z axis respectively. <see cref="LWH"/> is the unrotated orientation.
/// </remarks>
public enum Orientation
{
    /// <summary>Unrotated: length on X, width on Y, height on Z.</summary>
    LWH = 0,

    /// <summary>Yaw 90 degrees: width on X, length on Y, height on Z. Keeps the package upright.</summary>
    WLH = 1,

    /// <summary>Length on X, height on Y, width on Z. Tips the package onto its side.</summary>
    LHW = 2,

    /// <summary>Height on X, length on Y, width on Z. Tips the package onto its side.</summary>
    HLW = 3,

    /// <summary>Width on X, height on Y, length on Z. Tips the package onto its end.</summary>
    WHL = 4,

    /// <summary>Height on X, width on Y, length on Z. Tips the package onto its end.</summary>
    HWL = 5
}

/// <summary>
/// Provides the predefined sets of <see cref="Orientation"/> values used when packing.
/// </summary>
public static class Orientations
{
    /// <summary>
    /// All six axis-aligned orientations of a rectangular box.
    /// </summary>
    public static readonly IReadOnlyList<Orientation> All =
    [
        Orientation.LWH,
        Orientation.WLH,
        Orientation.LHW,
        Orientation.HLW,
        Orientation.WHL,
        Orientation.HWL
    ];

    /// <summary>
    /// The two orientations that keep the package's own height on the vertical axis.
    /// Use these for packages marked "this side up".
    /// </summary>
    public static readonly IReadOnlyList<Orientation> Upright =
    [
        Orientation.LWH,
        Orientation.WLH
    ];

    /// <summary>
    /// The single unrotated orientation, for packages that may not be turned at all.
    /// </summary>
    public static readonly IReadOnlyList<Orientation> Fixed = [Orientation.LWH];

    /// <summary>
    /// Selects the orientation set permitted for a package.
    /// </summary>
    /// <param name="allowRotation">Whether the package may be rotated at all.</param>
    /// <param name="keepUpright">Whether the package must stay upright when rotated.</param>
    /// <returns>The permitted orientations, most permissive first.</returns>
    public static IReadOnlyList<Orientation> For(bool allowRotation, bool keepUpright)
    {
        if (!allowRotation)
        {
            return Fixed;
        }

        return keepUpright ? Upright : All;
    }
}
