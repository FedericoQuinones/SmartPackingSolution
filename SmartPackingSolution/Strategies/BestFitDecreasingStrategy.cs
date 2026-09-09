namespace SmartPackingSolution.Strategies;

/// <summary>
/// Places each package, largest first, at the position that wastes the least space.
/// </summary>
/// <remarks>
/// Every feasible combination of anchor and orientation is scored, preferring low
/// placements that press against the walls and their neighbours, and the best one wins.
/// This costs more per package than first-fit but packs noticeably denser, which is why
/// it is the default strategy.
/// </remarks>
public class BestFitDecreasingStrategy : SequentialPackingStrategy
{
    /// <inheritdoc/>
    public override string Name => "Best-Fit Decreasing";

    /// <inheritdoc/>
    protected override bool FirstFit => false;

    /// <inheritdoc/>
    protected override PackingOptions Configure(PackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options;
    }
}
