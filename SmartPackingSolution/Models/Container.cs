namespace SmartPackingSolution.Models;

/// <summary>
/// Represents a container for packing packages.
/// </summary>
public class Container
{
    /// <summary>
    /// Gets the identifier of this container.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of this container.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the dimensions of the container.
    /// </summary>
    public Dimensions Dimensions { get; }

    /// <summary>
    /// Gets the maximum weight capacity of the container in kilograms.
    /// </summary>
    public double MaxWeight { get; }

    /// <summary>
    /// Gets the total volume of the container.
    /// </summary>
    public double Volume => Dimensions.Volume;

    /// <summary>
    /// Initializes a new instance of the <see cref="Container"/> class.
    /// </summary>
    /// <param name="length">The length in centimeters.</param>
    /// <param name="width">The width in centimeters.</param>
    /// <param name="height">The height in centimeters.</param>
    /// <param name="maxWeight">The maximum weight capacity in kilograms.</param>
    /// <param name="name">An optional display name.</param>
    /// <param name="id">An external identifier. When null, a new GUID is generated.</param>
    /// <exception cref="ArgumentException">Thrown when dimensions or weight are invalid.</exception>
    public Container(
        double length,
        double width,
        double height,
        double maxWeight,
        string? name = null,
        string? id = null)
    {
        var dimensions = new Dimensions(length, width, height);

        if (!dimensions.IsValid())
        {
            throw new ArgumentException("All container dimensions must be greater than zero.");
        }

        if (maxWeight <= 0 || !double.IsFinite(maxWeight))
        {
            throw new ArgumentException("Maximum weight must be greater than zero.", nameof(maxWeight));
        }

        Dimensions = dimensions;
        MaxWeight = maxWeight;
        Id = id ?? Guid.NewGuid().ToString();
        Name = name ?? "Container";
    }

    /// <summary>
    /// Creates a copy of this container with a new identity, for multi-container packing.
    /// </summary>
    /// <param name="name">The name of the copy.</param>
    /// <returns>A new container with identical dimensions and capacity.</returns>
    public Container CloneWithName(string name) =>
        new(Dimensions.Length, Dimensions.Width, Dimensions.Height, MaxWeight, name);
}
