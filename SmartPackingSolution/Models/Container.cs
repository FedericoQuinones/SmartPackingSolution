namespace SmartPackingSolution.Models;

/// <summary>
/// Represents a container for packing packages.
/// </summary>
public class Container
{
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
    /// <exception cref="ArgumentException">Thrown when dimensions or weight are invalid.</exception>
    public Container(double length, double width, double height, double maxWeight)
    {
        Dimensions = new Dimensions(length, width, height);
        MaxWeight = maxWeight;

        if (!Dimensions.IsValid())
        {
            throw new ArgumentException("All container dimensions must be greater than zero.");
        }

        if (maxWeight <= 0)
        {
            throw new ArgumentException("Maximum weight must be greater than zero.", nameof(maxWeight));
        }
    }
}