namespace SmartPackingSolution.Algorithms;

using SmartPackingSolution.Models;

/// <summary>
/// The outcome of packing one set of packages across several containers.
/// </summary>
public class MultiPackingResult
{
    /// <summary>
    /// Gets the result for each container that was used, in the order they were filled.
    /// </summary>
    public IReadOnlyList<PackingResult> Containers { get; }

    /// <summary>
    /// Gets the packages that no container could take.
    /// </summary>
    public IReadOnlyList<UnpackedPackage> UnpackedItems { get; }

    /// <summary>
    /// Gets the number of containers used.
    /// </summary>
    public int ContainerCount => Containers.Count;

    /// <summary>
    /// Gets the total number of packages placed across all containers.
    /// </summary>
    public int PackedCount => Containers.Sum(c => c.PackedItems.Count);

    /// <summary>
    /// Gets the total weight placed across all containers, in kilograms.
    /// </summary>
    public double TotalWeight => Containers.Sum(c => c.TotalWeight);

    /// <summary>
    /// Gets the fraction of the used containers' combined volume that is occupied.
    /// </summary>
    public double OverallUtilization
    {
        get
        {
            var capacity = Containers.Sum(c => c.Container.Volume);
            if (capacity <= 0)
            {
                return 0;
            }

            return Containers.Sum(c => c.PackedItems.Sum(p => p.Package.Volume)) / capacity;
        }
    }

    /// <summary>
    /// Gets whether every package was placed somewhere.
    /// </summary>
    public bool IsFullyPacked => UnpackedItems.Count == 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiPackingResult"/> class.
    /// </summary>
    /// <param name="containers">The per-container results.</param>
    /// <param name="unpackedItems">The packages no container could take.</param>
    public MultiPackingResult(
        IEnumerable<PackingResult> containers,
        IEnumerable<UnpackedPackage> unpackedItems)
    {
        Containers = containers?.ToList() ?? throw new ArgumentNullException(nameof(containers));
        UnpackedItems = unpackedItems?.ToList() ?? throw new ArgumentNullException(nameof(unpackedItems));
    }
}
