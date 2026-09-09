namespace SmartPackingSolution.Strategies;

/// <summary>
/// Controls how strictly the packing engine models physics, and how hard it searches.
/// </summary>
public sealed record PackingOptions
{
    /// <summary>
    /// Gets the shared default options.
    /// </summary>
    public static PackingOptions Default { get; } = new();

    /// <summary>
    /// Gets the slack, in centimeters, applied to every geometric comparison.
    /// </summary>
    public double Tolerance { get; init; } = 1e-6;

    /// <summary>
    /// Gets the grid step used to de-duplicate candidate positions, in centimeters.
    /// </summary>
    public double PositionGrid { get; init; } = 1e-4;

    /// <summary>
    /// Gets whether a package must rest on the floor or on other packages.
    /// </summary>
    /// <remarks>
    /// When false, packages may float in mid-air. Only turn this off to compare against
    /// naive packers; it produces layouts that cannot be loaded in reality.
    /// </remarks>
    public bool RequireSupport { get; init; } = true;

    /// <summary>
    /// Gets the fraction of a package's base area that must rest on the floor or on the
    /// top faces of other packages, between 0 and 1.
    /// </summary>
    public double MinimumSupportRatio { get; init; } = 0.75;

    /// <summary>
    /// Gets whether the accumulated weight resting on each package is checked against
    /// its <see cref="Models.PackageItem.MaxSupportedWeight"/>.
    /// </summary>
    public bool EnforceLoadBearing { get; init; } = true;

    /// <summary>
    /// Gets whether packages may be rotated at all, overriding per-package settings when false.
    /// </summary>
    public bool AllowRotation { get; init; } = true;

    /// <summary>
    /// Gets the order in which packages are offered to the placement search.
    /// </summary>
    public ItemOrdering Ordering { get; init; } = ItemOrdering.VolumeDescending;

    /// <summary>
    /// Gets the rule used to choose among feasible placements.
    /// </summary>
    public PlacementScore Score { get; init; } = PlacementScore.TightestFit;

    /// <summary>
    /// Gets the cap on candidate anchor positions retained per container.
    /// </summary>
    /// <remarks>
    /// A safety valve for pathological inputs. The anchor set is pruned to the most
    /// promising positions - lowest, then closest to the origin - when it exceeds this.
    /// </remarks>
    public int MaxCandidatePositions { get; init; } = 20000;

    /// <summary>
    /// Gets an optional wall-clock budget for the packing run.
    /// </summary>
    /// <remarks>
    /// When it elapses, packing stops and the remaining packages are reported with
    /// <see cref="Models.UnpackedReason.TimeBudgetExceeded"/>.
    /// </remarks>
    public TimeSpan? TimeBudget { get; init; }

    /// <summary>
    /// Gets the number of extra randomised orderings to try, keeping the best result.
    /// </summary>
    public int RandomRestarts { get; init; }

    /// <summary>
    /// Gets the seed used for randomised orderings, so runs are reproducible.
    /// </summary>
    public int RandomSeed { get; init; } = 20260909;

    /// <summary>
    /// Gets whether infeasible input throws instead of being reported as unpacked.
    /// </summary>
    /// <remarks>
    /// The default is false: a package that is too large, or one kilogram too heavy,
    /// is reported through <see cref="Models.PackingResult.UnpackedItems"/> with a
    /// reason, and everything that does fit is still packed.
    /// </remarks>
    public bool ThrowOnInfeasibleInput { get; init; }

    /// <summary>
    /// Gets the token used to cancel a packing run.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
}

/// <summary>
/// Determines the order in which packages are offered to the placement search.
/// </summary>
public enum ItemOrdering
{
    /// <summary>Largest volume first - the classic decreasing-first-fit ordering.</summary>
    VolumeDescending,

    /// <summary>Largest footprint first, which tends to build stable floors.</summary>
    BaseAreaDescending,

    /// <summary>Longest single side first, useful when a few awkward items dominate.</summary>
    MaxDimensionDescending,

    /// <summary>Heaviest first, which drives the centre of gravity down.</summary>
    HeaviestFirst,

    /// <summary>The order in which the caller supplied the packages.</summary>
    AsSupplied
}

/// <summary>
/// Determines how a feasible placement is scored, lowest score winning.
/// </summary>
public enum PlacementScore
{
    /// <summary>Take the lowest, then deepest, then leftmost feasible position.</summary>
    DeepestBottomLeft,

    /// <summary>Prefer placements that waste the least space around the package.</summary>
    TightestFit,

    /// <summary>Prefer placements with the greatest contact against walls and neighbours.</summary>
    MaxContactSurface
}
