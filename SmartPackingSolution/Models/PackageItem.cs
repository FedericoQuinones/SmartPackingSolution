namespace SmartPackingSolution.Models;

/// <summary>
/// Represents a package item to be placed in a container.
/// </summary>
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
    /// Gets or sets whether this package can be rotated during packing.
    /// </summary>
    public bool AllowRotation { get; set; }

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
    /// <param name="allowRotation">Whether rotation is allowed (default: true).</param>
    /// <exception cref="ArgumentException">Thrown when dimensions or weight are invalid.</exception>
    public PackageItem(
        string name,
        double length,
        double width,
        double height,
        double weight,
        PackagePriority priority = PackagePriority.Medium,
        bool allowRotation = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Package name cannot be empty.", nameof(name));
        }

        if (weight <= 0)
        {
            throw new ArgumentException("Weight must be greater than zero.", nameof(weight));
        }

        Id = Guid.NewGuid().ToString();
        Name = name;
        Dimensions = new Dimensions(length, width, height);
        Weight = weight;
        Priority = priority;
        AllowRotation = allowRotation;

        if (!Dimensions.IsValid())
        {
            throw new ArgumentException("All dimensions must be greater than zero.");
        }
    }
}