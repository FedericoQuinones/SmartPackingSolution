namespace SmartPackingSolution.Strategies;

/// <summary>
/// Places each package, largest first, at the first position that can legally hold it.
/// </summary>
/// <remarks>
/// The fastest of the strategies: it stops scanning as soon as a position works, rather
/// than scoring every candidate. Use it when throughput matters more than density.
/// </remarks>
public class FirstFitDecreasingStrategy : SequentialPackingStrategy
{
    /// <inheritdoc/>
    public override string Name => "First-Fit Decreasing";

    /// <inheritdoc/>
    protected override bool FirstFit => true;
}
