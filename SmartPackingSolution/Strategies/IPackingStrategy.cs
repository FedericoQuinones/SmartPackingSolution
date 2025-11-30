namespace SmartPackingSolution.Strategies;

using SmartPackingSolution.Models;

/// <summary>
/// Defines the contract for packing strategy implementations.
/// </summary>
public interface IPackingStrategy
{
    /// <summary>
    /// Attempts to pack a collection of packages into a container.
    /// </summary>
    /// <param name="container">The container to pack items into.</param>
    /// <param name="packages">The packages to pack.</param>
    /// <returns>A packing result containing placed and unplaced items.</returns>
    PackingResult Pack(Container container, IEnumerable<PackageItem> packages);
}