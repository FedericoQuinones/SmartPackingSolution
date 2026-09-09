namespace SmartPackingSolution.Algorithms;

using SmartPackingSolution.Models;
using SmartPackingSolution.Strategies;

/// <summary>
/// Packs a set of packages across more than one container.
/// </summary>
/// <remarks>
/// A single container answers "what fits?"; a shipment asks "how many trucks, and which
/// size?". Containers are filled in the order given and each one is offered only what the
/// previous ones could not take, which is the standard first-fit reduction of bin packing
/// to repeated single-bin packing.
/// </remarks>
public class MultiContainerOptimizer
{
    private readonly IPackingStrategy _strategy;
    private readonly PackingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiContainerOptimizer"/> class.
    /// </summary>
    /// <param name="strategy">
    /// The packing strategy to use. Defaults to <see cref="BestFitDecreasingStrategy"/>.
    /// </param>
    /// <param name="options">The physical and search settings. Defaults are used when null.</param>
    public MultiContainerOptimizer(IPackingStrategy? strategy = null, PackingOptions? options = null)
    {
        _strategy = strategy ?? new BestFitDecreasingStrategy();
        _options = options ?? PackingOptions.Default;
    }

    /// <summary>
    /// Fills the given containers in order, passing what will not fit to the next one.
    /// </summary>
    /// <param name="containers">The containers available, in the order to use them.</param>
    /// <param name="packages">The packages to distribute.</param>
    /// <returns>A per-container breakdown plus anything left over.</returns>
    public MultiPackingResult PackAll(IEnumerable<Container> containers, IEnumerable<PackageItem> packages)
    {
        ArgumentNullException.ThrowIfNull(containers);
        ArgumentNullException.ThrowIfNull(packages);

        var results = new List<PackingResult>();
        var remaining = packages.ToList();

        foreach (var container in containers)
        {
            if (remaining.Count == 0)
            {
                break;
            }

            var result = _strategy.Pack(container, remaining, _options);
            results.Add(result);

            // Everything this container refused moves on, including packages it judged
            // too large: the containers need not be the same size, so a later one may
            // still take them.
            remaining = [.. result.UnpackedItems.Select(u => u.Package)];

            if (result.PackedItems.Count == 0)
            {
                // This container took nothing, so it contributes only an empty result.
                results.RemoveAt(results.Count - 1);
            }
        }

        var leftovers = remaining
            .Select(p => new UnpackedPackage(
                p,
                UnpackedReason.NoSpaceAvailable,
                $"No remaining container had room for '{p.Name}'."))
            .ToList();

        return new MultiPackingResult(results, leftovers);
    }

    /// <summary>
    /// Opens as many copies of a container as it takes to hold everything.
    /// </summary>
    /// <param name="template">The container type to replicate.</param>
    /// <param name="packages">The packages to distribute.</param>
    /// <param name="maxContainers">An upper bound on how many containers may be opened.</param>
    /// <returns>A per-container breakdown plus anything that fits in no container at all.</returns>
    public MultiPackingResult PackAll(
        Container template,
        IEnumerable<PackageItem> packages,
        int maxContainers = 100)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxContainers);

        var results = new List<PackingResult>();
        var remaining = packages.ToList();
        var impossible = new List<UnpackedPackage>();

        while (remaining.Count > 0 && results.Count < maxContainers)
        {
            var container = template.CloneWithName($"{template.Name} #{results.Count + 1}");
            var result = _strategy.Pack(container, remaining, _options);

            if (result.PackedItems.Count == 0)
            {
                // An empty container means nothing left can ever be placed; stop rather
                // than opening containers forever.
                break;
            }

            results.Add(result);

            var stillUnpacked = result.UnpackedItems.ToList();
            impossible.AddRange(stillUnpacked.Where(u => u.Reason == UnpackedReason.TooLargeForContainer));
            remaining = [.. stillUnpacked
                .Where(u => u.Reason != UnpackedReason.TooLargeForContainer)
                .Select(u => u.Package)];
        }

        var leftovers = impossible
            .Concat(remaining.Select(p => new UnpackedPackage(
                p,
                UnpackedReason.NoSpaceAvailable,
                $"'{p.Name}' was still unplaced after {results.Count} container(s).")))
            .ToList();

        return new MultiPackingResult(results, leftovers);
    }

    /// <summary>
    /// Chooses the smallest container from a catalogue that holds the whole load.
    /// </summary>
    /// <param name="catalogue">The container types available.</param>
    /// <param name="packages">The packages that must all fit.</param>
    /// <returns>
    /// The packing result for the smallest container that fits everything, or null if
    /// none does.
    /// </returns>
    public PackingResult? SelectBestContainer(
        IEnumerable<Container> catalogue,
        IEnumerable<PackageItem> packages)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        ArgumentNullException.ThrowIfNull(packages);

        var items = packages.ToList();

        foreach (var container in catalogue.OrderBy(c => c.Volume))
        {
            var result = _strategy.Pack(container, items, _options);

            if (result.IsFullyPacked)
            {
                return result;
            }
        }

        return null;
    }
}
