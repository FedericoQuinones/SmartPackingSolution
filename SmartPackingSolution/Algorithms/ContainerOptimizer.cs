namespace SmartPackingSolution.Algorithms;

using SmartPackingSolution.Exceptions;
using SmartPackingSolution.Models;
using SmartPackingSolution.Strategies;

/// <summary>
/// Main optimizer class for packing packages into containers.
/// Provides high-level API for container packing operations.
/// </summary>
public class ContainerOptimizer
{
    private readonly IPackingStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerOptimizer"/> class.
    /// </summary>
    /// <param name="strategy">The packing strategy to use. If null, uses FirstFitDecreasing.</param>
    public ContainerOptimizer(IPackingStrategy? strategy = null)
    {
        _strategy = strategy ?? new FirstFitDecreasingStrategy();
    }

    /// <summary>
    /// Optimizes the packing of packages into a container.
    /// </summary>
    /// <param name="container">The container to pack into.</param>
    /// <param name="packages">The packages to pack.</param>
    /// <returns>A result containing the packing layout and statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when container or packages are null.</exception>
    /// <exception cref="PackingException">Thrown when the packing operation fails.</exception>
    /// <example>
    /// <code>
    /// var container = new Container(120, 100, 100, maxWeight: 1000);
    /// var packages = new List&lt;PackageItem&gt;
    /// {
    ///     new PackageItem("Box", 30, 30, 30, 5, PackagePriority.Medium)
    /// };
    /// var optimizer = new ContainerOptimizer();
    /// var result = optimizer.OptimizePacking(container, packages);
    /// </code>
    /// </example>
    public PackingResult OptimizePacking(Container container, IEnumerable<PackageItem> packages)
    {
        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        if (packages == null)
        {
            throw new ArgumentNullException(nameof(packages));
        }

        var packageList = packages.ToList();

        if (packageList.Count == 0)
        {
            return new PackingResult(container, Array.Empty<PlacedPackage>(), Array.Empty<PackageItem>());
        }

        ValidatePackages(packageList, container);

        try
        {
            return _strategy.Pack(container, packageList);
        }
        catch (Exception ex) when (ex is not PackingException)
        {
            throw new PackingException("An error occurred during packing optimization.", ex);
        }
    }

    private void ValidatePackages(List<PackageItem> packages, Container container)
    {
        var totalWeight = packages.Sum(p => p.Weight);

        if (totalWeight > container.MaxWeight)
        {
            throw new PackingException(
                $"Total package weight ({totalWeight}kg) exceeds container capacity ({container.MaxWeight}kg).");
        }

        foreach (var package in packages)
        {
            if (package.Dimensions.Length > container.Dimensions.Length &&
                package.Dimensions.Width > container.Dimensions.Width &&
                package.Dimensions.Height > container.Dimensions.Height)
            {
                throw new PackingException(
                    $"Package '{package.Name}' is too large for the container in all orientations.");
            }
        }
    }
}