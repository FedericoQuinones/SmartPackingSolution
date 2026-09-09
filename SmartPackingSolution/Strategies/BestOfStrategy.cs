namespace SmartPackingSolution.Strategies;

using System.Collections.Concurrent;
using SmartPackingSolution.Models;

/// <summary>
/// Runs several strategies and orderings in parallel and keeps whichever layout came out best.
/// </summary>
/// <remarks>
/// <para>
/// No single heuristic wins on every load: layer packing dominates on uniform cartons and
/// loses badly on mixed furniture, and the best item ordering depends on whether a few
/// awkward pieces or the sheer count of small ones is the binding constraint. Since the
/// runs are independent and packages are immutable, trying them all costs wall-clock time
/// roughly equal to the slowest one, and the answer is never worse than the best single
/// heuristic.
/// </para>
/// <para>
/// Candidates are compared by packed volume, with load balance breaking ties: between two
/// layouts holding the same goods, the one whose centre of gravity sits lower and more
/// central is the one that survives the journey.
/// </para>
/// </remarks>
public class BestOfStrategy : IPackingStrategy
{
    private readonly IReadOnlyList<IPackingStrategy> _strategies;
    private readonly IReadOnlyList<ItemOrdering> _orderings;

    /// <summary>
    /// Initializes a new instance using the default strategies and orderings.
    /// </summary>
    public BestOfStrategy()
        : this(
            [new BestFitDecreasingStrategy(), new FirstFitDecreasingStrategy(), new LayerBasedStrategy()],
            [ItemOrdering.VolumeDescending, ItemOrdering.BaseAreaDescending, ItemOrdering.HeaviestFirst])
    {
    }

    /// <summary>
    /// Initializes a new instance over an explicit set of candidates.
    /// </summary>
    /// <param name="strategies">The strategies to run.</param>
    /// <param name="orderings">The item orderings to try with each strategy.</param>
    public BestOfStrategy(
        IReadOnlyList<IPackingStrategy> strategies,
        IReadOnlyList<ItemOrdering> orderings)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        ArgumentNullException.ThrowIfNull(orderings);

        if (strategies.Count == 0)
        {
            throw new ArgumentException("At least one strategy is required.", nameof(strategies));
        }

        if (orderings.Count == 0)
        {
            throw new ArgumentException("At least one ordering is required.", nameof(orderings));
        }

        _strategies = strategies;
        _orderings = orderings;
    }

    /// <inheritdoc/>
    public string Name => "Best-Of";

    /// <inheritdoc/>
    public PackingResult Pack(
        Container container,
        IEnumerable<PackageItem> packages,
        PackingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(packages);

        var effective = options ?? PackingOptions.Default;
        var items = packages.ToList();

        var runs = (
            from strategy in _strategies
            from ordering in _orderings
            select (strategy, ordering)).ToList();

        var results = new ConcurrentBag<PackingResult>();

        Parallel.ForEach(
            runs,
            new ParallelOptions { CancellationToken = CancellationToken.None },
            run => results.Add(run.strategy.Pack(container, items, effective with { Ordering = run.ordering })));

        var best = results
            .OrderByDescending(r => r.SpaceUtilization)
            .ThenByDescending(r => r.LoadBalanceScore)
            .ThenBy(r => r.Elapsed)
            .First();

        return new PackingResult(
            container,
            best.PackedItems,
            best.UnpackedItems,
            $"{Name} ({best.StrategyName})",
            results.Max(r => r.Elapsed));
    }
}
