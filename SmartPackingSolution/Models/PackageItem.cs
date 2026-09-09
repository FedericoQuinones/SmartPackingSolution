namespace SmartPackingSolution.Models;

/// <summary>
/// Represents a package item to be placed in a container.
/// </summary>
/// <remarks>
/// Instances are immutable and therefore safe to share across concurrent packing runs.
/// </remarks>
public class PackageItem
{
    /// <summary>
    /// Gets the unique identifier for this package.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the name or description of the package.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the dimensions of the package.
    /// </summary>
    public Dimensions Dimensions { get; }

    /// <summary>
    /// Gets the weight of the package in kilograms.
    /// </summary>
    public double Weight { get; }

    /// <summary>
    /// Gets the priority level that determines stacking constraints.
    /// </summary>
    public PackagePriority Priority { get; }

    /// <summary>
    /// Gets whether this package can be rotated during packing.
    /// </summary>
    public bool AllowRotation { get; }

    /// <summary>
    /// Gets whether this package must stay upright ("this side up"), restricting it to
    /// the orientations that keep its own height on the vertical axis.
    /// </summary>
    public bool KeepUpright { get; }

    /// <summary>
    /// Gets the maximum weight in kilograms that may rest on top of this package.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="PackagePriorityExtensions.DefaultLoadCapacity"/> for the
    /// package's priority. A value of zero means nothing may be stacked on it.
    /// </remarks>
    public double MaxSupportedWeight { get; }

    /// <summary>
    /// Gets the orientations this package may be placed in, given its rotation settings.
    /// </summary>
    public IReadOnlyList<Orientation> AllowedOrientations { get; }

    /// <summary>
    /// Gets the volume of the package in cubic centimeters.
    /// </summary>
    public double Volume => Dimensions.Volume;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageItem"/> class.
    /// </summary>
    /// <param name="name">The name or description of the package.</param>
    /// <param name="length">The length dimension in centimeters.</param>
    /// <param name="width">The width dimension in centimeters.</param>
    /// <param name="height">The height dimension in centimeters.</param>
    /// <param name="weight">The weight in kilograms.</param>
    /// <param name="priority">The priority level for stacking.</param>
    /// <param name="allowRotation">Whether rotation is allowed.</param>
    /// <param name="keepUpright">Whether the package must remain upright when rotated.</param>
    /// <param name="maxSupportedWeight">
    /// The load this package can carry, in kilograms. When null, the default for
    /// <paramref name="priority"/> is used.
    /// </param>
    /// <param name="id">An external identifier. When null, a new GUID is generated.</param>
    /// <exception cref="ArgumentException">Thrown when dimensions or weight are invalid.</exception>
    public PackageItem(
        string name,
        double length,
        double width,
        double height,
        double weight,
        PackagePriority priority = PackagePriority.Medium,
        bool allowRotation = true,
        bool keepUpright = false,
        double? maxSupportedWeight = null,
        string? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Package name cannot be empty.", nameof(name));
        }

        if (weight <= 0 || !double.IsFinite(weight))
        {
            throw new ArgumentException("Weight must be greater than zero.", nameof(weight));
        }

        Dimensions = new Dimensions(length, width, height);

        if (!Dimensions.IsValid())
        {
            throw new ArgumentException("All dimensions must be greater than zero.");
        }

        if (maxSupportedWeight is < 0)
        {
            throw new ArgumentException(
                "Maximum supported weight cannot be negative.", nameof(maxSupportedWeight));
        }

        Id = id ?? Guid.NewGuid().ToString();
        Name = name;
        Weight = weight;
        Priority = priority;
        AllowRotation = allowRotation;
        KeepUpright = keepUpright;
        MaxSupportedWeight = maxSupportedWeight ?? priority.DefaultLoadCapacity();
        AllowedOrientations = Orientations.For(allowRotation, keepUpright);
    }
}
