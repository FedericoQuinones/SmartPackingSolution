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
    private readonly PackingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerOptimizer"/> class.
    /// </summary>
    /// <param name="strategy">
    /// The packing strategy to use. Defaults to <see cref="BestFitDecreasingStrategy"/>.
    /// </param>
    /// <param name="options">The physical and search settings. Defaults are used when null.</param>
    public ContainerOptimizer(IPackingStrategy? strategy = null, PackingOptions? options = null)
    {
        _strategy = strategy ?? new BestFitDecreasingStrategy();
        _options = options ?? PackingOptions.Default;
    }

    /// <summary>
    /// Optimizes the packing of packages into a container.
    /// </summary>
    /// <param name="container">The container to pack into.</param>
    /// <param name="packages">The packages to pack.</param>
    /// <returns>A result containing the packing layout and statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when container or packages are null.</exception>
    /// <exception cref="PackingException">
    /// Thrown when the packing operation fails, or when the input is infeasible and
    /// <see cref="PackingOptions.ThrowOnInfeasibleInput"/> is set.
    /// </exception>
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
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(packages);

        var packageList = packages.ToList();

        if (packageList.Count == 0)
        {
            return new PackingResult(
                container,
                Array.Empty<PlacedPackage>(),
                Array.Empty<UnpackedPackage>(),
                _strategy.Name);
        }

        if (_options.ThrowOnInfeasibleInput)
        {
            ValidateFeasible(packageList, container);
        }

        try
        {
            return _strategy.Pack(container, packageList, _options);
        }
        catch (Exception ex) when (ex is not PackingException and not OperationCanceledException)
        {
            throw new PackingException("An error occurred during packing optimization.", ex);
        }
    }

    /// <summary>
    /// Rejects input that cannot possibly be packed, for callers that opt in to failing
    /// fast rather than receiving a partial result.
    /// </summary>
    /// <param name="packages">The list of packages.</param>
    /// <param name="container">The target container.</param>
    /// <exception cref="PackingException">
    /// Thrown when the total weight exceeds capacity, or a package fits in no orientation.
    /// </exception>
    /// <remarks>
    /// This is opt-in because throwing is usually the wrong answer: one package a kilogram
    /// over capacity should not deny the caller a layout for everything else. By default
    /// such packages are reported through <see cref="PackingResult.UnpackedItems"/> with
    /// an <see cref="UnpackedReason"/>.
    /// </remarks>
    private static void ValidateFeasible(List<PackageItem> packages, Container container)
    {
        var totalWeight = packages.Sum(p => p.Weight);

        if (totalWeight > container.MaxWeight)
        {
            throw new PackingException(
                $"Total package weight ({totalWeight}kg) exceeds container capacity ({container.MaxWeight}kg).");
        }

        foreach (var package in packages)
        {
            // Comparing the sorted dimension triples answers "does any orientation fit"
            // exactly. The previous check required all three dimensions to exceed the
            // container's, so a 200x5x5 package in a 100x100x100 container passed.
            if (!package.Dimensions.FitsInsideInSomeOrientation(container.Dimensions))
            {
                throw new PackingException(
                    $"Package '{package.Name}' is too large for the container in all orientations.");
            }
        }
    }
}
