namespace SmartPackingSolution.Strategies;

using System.Diagnostics;
using SmartPackingSolution.Constraints;
using SmartPackingSolution.Geometry;
using SmartPackingSolution.Models;

/// <summary>
/// Holds the state of a container being filled, and answers the one question every
/// strategy needs: where, if anywhere, can this package legally go?
/// </summary>
/// <remarks>
/// Geometry, support and load bearing live here rather than inside a strategy, so a new
/// heuristic only has to decide the order packages are offered in and how to score the
/// placements it is handed. Previously all of this was embedded in the one strategy that
/// existed, which meant a second strategy would have had to copy it.
/// </remarks>
public sealed class PackingContext
{
    private readonly List<PlacedPackage> _placed = [];
    private readonly List<AxisAlignedBox> _boxes = [];
    private readonly SpatialIndex _index;
    private readonly ExtremePointSet _anchors;
    private readonly LoadBearingGraph _loads;
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    /// <summary>
    /// How far outside a candidate box the neighbour query reaches.
    /// </summary>
    /// <remarks>
    /// Every relation the engine cares about - overlap, a supporter's top face, a
    /// neighbour's touching side - happens within the box itself or flush against it, so
    /// the query only has to reach far enough to cross a grid cell boundary when a face
    /// lands exactly on one. Expanding by a whole package length instead, as the first
    /// version did, pulled in most of the container on every candidate and made the
    /// broad-phase index pointless.
    /// </remarks>
    private const double NeighbourMargin = 1e-3;

    /// <summary>
    /// Initializes a context for one container.
    /// </summary>
    /// <param name="container">The container to fill.</param>
    /// <param name="options">The physical and search settings to apply.</param>
    /// <param name="items">
    /// The packages that will be offered, used only to size the spatial grid.
    /// </param>
    public PackingContext(Container container, PackingOptions options, IReadOnlyList<PackageItem> items)
    {
        Container = container ?? throw new ArgumentNullException(nameof(container));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(items);

        _index = new SpatialIndex(GridCellSize(container, items));
        _anchors = new ExtremePointSet(options.PositionGrid, options.Tolerance);
        _loads = new LoadBearingGraph(options.Tolerance);
    }

    /// <summary>Gets the container being filled.</summary>
    public Container Container { get; }

    /// <summary>Gets the options in force for this run.</summary>
    public PackingOptions Options { get; }

    /// <summary>Gets the packages placed so far, in placement order.</summary>
    public IReadOnlyList<PlacedPackage> Placed => _placed;

    /// <summary>Gets the total weight placed so far, in kilograms.</summary>
    public double TotalWeight { get; private set; }

    /// <summary>Gets the elapsed wall-clock time since the context was created.</summary>
    public TimeSpan Elapsed => _clock.Elapsed;

    /// <summary>
    /// Gets whether the configured time budget has elapsed or cancellation was requested.
    /// </summary>
    public bool IsOutOfTime =>
        Options.CancellationToken.IsCancellationRequested ||
        (Options.TimeBudget.HasValue && _clock.Elapsed > Options.TimeBudget.Value);

    /// <summary>
    /// Gets the candidate anchor positions, ordered lowest first.
    /// </summary>
    /// <returns>The ordered anchors.</returns>
    public IReadOnlyList<Position> Anchors() => _anchors.Ordered();

    /// <summary>
    /// Searches for a placement for <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The package to place.</param>
    /// <param name="firstFit">
    /// When true, return the first feasible placement in lowest-first order instead of
    /// scoring every candidate.
    /// </param>
    /// <param name="positionFilter">
    /// An optional restriction on which anchors may be used, used by layer-based packing.
    /// </param>
    /// <param name="ceilingZ">
    /// An optional cap on the height of the placement's top face, used by layer-based
    /// packing to keep a package inside the layer it is filling.
    /// </param>
    /// <returns>
    /// The chosen placement, or null with the reason no placement was possible.
    /// </returns>
    public (PlacedPackage? Placement, UnpackedReason Reason) FindPlacement(
        PackageItem item,
        bool firstFit = false,
        Func<Position, bool>? positionFilter = null,
        double? ceilingZ = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!item.Dimensions.FitsInsideInSomeOrientation(Container.Dimensions, Options.Tolerance))
        {
            return (null, UnpackedReason.TooLargeForContainer);
        }

        if (TotalWeight + item.Weight > Container.MaxWeight + Options.Tolerance)
        {
            return (null, UnpackedReason.WeightCapacityExceeded);
        }

        var orientations = Options.AllowRotation
            ? item.AllowedOrientations
            : Orientations.Fixed;

        PlacedPackage? best = null;
        var bestScore = double.PositiveInfinity;

        // Tracks how far the most promising candidate got, so the caller learns which
        // constraint actually blocked the package rather than a generic "no space".
        var sawGeometricFit = false;
        var sawSupportFailure = false;
        var sawLoadFailure = false;

        foreach (var anchor in _anchors.Ordered())
        {
            if (IsOutOfTime)
            {
                return (best, UnpackedReason.TimeBudgetExceeded);
            }

            if (positionFilter is not null && !positionFilter(anchor))
            {
                continue;
            }

            foreach (var orientation in orientations)
            {
                var dimensions = item.Dimensions.Rotate(orientation);
                var box = new AxisAlignedBox(anchor, dimensions);

                if (!box.IsInside(Container.Dimensions, Options.Tolerance))
                {
                    continue;
                }

                if (ceilingZ.HasValue && box.MaxZ > ceilingZ.Value + Options.Tolerance)
                {
                    continue;
                }

                var neighbours = _index.Query(box, NeighbourMargin).ToList();

                if (Intersects(box, neighbours))
                {
                    continue;
                }

                sawGeometricFit = true;

                if (Options.RequireSupport && !HasSupport(box, neighbours))
                {
                    sawSupportFailure = true;
                    continue;
                }

                if (Options.EnforceLoadBearing && !_loads.CanCarry(box, item, neighbours))
                {
                    sawLoadFailure = true;
                    continue;
                }

                var placement = new PlacedPackage(item, anchor, orientation);

                if (firstFit)
                {
                    return (placement, UnpackedReason.NoSpaceAvailable);
                }

                var score = ScoreOf(box, neighbours);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = placement;
                }
            }
        }

        if (best is not null)
        {
            return (best, UnpackedReason.NoSpaceAvailable);
        }

        var reason = (sawGeometricFit, sawSupportFailure, sawLoadFailure) switch
        {
            (true, _, true) => UnpackedReason.LoadBearingConstraint,
            (true, true, _) => UnpackedReason.SupportConstraint,
            _ => UnpackedReason.NoSpaceAvailable
        };

        return (null, reason);
    }

    /// <summary>
    /// Commits a placement returned by <see cref="FindPlacement"/> to the container.
    /// </summary>
    /// <param name="placement">The placement to record.</param>
    public void Commit(PlacedPackage placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        // Query the neighbours before indexing this placement, or the query returns the
        // placement's own id, which the load graph has not registered yet.
        var neighbours = _index
            .Query(placement.Bounds, NeighbourMargin)
            .ToList();

        var id = _placed.Count;
        _placed.Add(placement);
        _boxes.Add(placement.Bounds);
        _index.Add(id, placement.Bounds);
        _loads.Add(placement, neighbours);
        TotalWeight += placement.Package.Weight;

        _anchors.Remove(placement.Position);
        _anchors.AddFrom(placement.Bounds, _boxes, Container.Dimensions);
        _anchors.Prune(Options.MaxCandidatePositions);
    }

    private bool Intersects(in AxisAlignedBox box, List<int> neighbours)
    {
        foreach (var index in neighbours)
        {
            if (box.Overlaps(_boxes[index], Options.Tolerance))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasSupport(in AxisAlignedBox box, List<int> neighbours)
    {
        if (box.MinZ <= Options.Tolerance)
        {
            return true;
        }

        var supporters = new List<AxisAlignedBox>(neighbours.Count);
        foreach (var index in neighbours)
        {
            supporters.Add(_boxes[index]);
        }

        return SupportCalculator.SupportRatio(box, supporters, Options.Tolerance)
            >= Options.MinimumSupportRatio - Options.Tolerance;
    }

    /// <summary>
    /// Scores a feasible placement, lower being better.
    /// </summary>
    /// <param name="box">The box the package would occupy.</param>
    /// <param name="neighbours">Nearby placed package indices.</param>
    /// <returns>The score under the configured <see cref="PlacementScore"/> rule.</returns>
    private double ScoreOf(in AxisAlignedBox box, List<int> neighbours)
    {
        // Keeping the load low dominates every rule: it is what keeps the centre of
        // gravity down and leaves the upper volume free for whatever comes next.
        var height = box.MinZ;

        return Options.Score switch
        {
            PlacementScore.DeepestBottomLeft => (height * 1e6) + (box.MinX * 1e3) + box.MinY,
            PlacementScore.MaxContactSurface => (height * 1e6) - ContactArea(box, neighbours),
            _ => (height * 1e6) + (box.MinX * 1e3) + box.MinY - ContactArea(box, neighbours)
        };
    }

    private double ContactArea(in AxisAlignedBox box, List<int> neighbours)
    {
        var area = 0.0;

        // Contact with the container walls and floor counts too: a package wedged into a
        // corner leaves a single large gap rather than several unusable slivers.
        if (box.MinX <= Options.Tolerance) { area += (box.MaxY - box.MinY) * (box.MaxZ - box.MinZ); }
        if (box.MinY <= Options.Tolerance) { area += (box.MaxX - box.MinX) * (box.MaxZ - box.MinZ); }
        if (box.MinZ <= Options.Tolerance) { area += box.FootprintArea; }
        if (box.MaxX >= Container.Dimensions.Length - Options.Tolerance) { area += (box.MaxY - box.MinY) * (box.MaxZ - box.MinZ); }
        if (box.MaxY >= Container.Dimensions.Width - Options.Tolerance) { area += (box.MaxX - box.MinX) * (box.MaxZ - box.MinZ); }

        foreach (var index in neighbours)
        {
            area += box.ContactArea(_boxes[index], Options.Tolerance);
        }

        return area;
    }

    /// <summary>
    /// Chooses a spatial grid cell size from the packages that will be offered.
    /// </summary>
    /// <param name="container">The container being filled.</param>
    /// <param name="items">The packages to be packed.</param>
    /// <returns>The cell edge length.</returns>
    /// <remarks>
    /// The median longest side keeps a typical package spanning a couple of cells: much
    /// smaller and each insertion touches many cells, much larger and every query
    /// degenerates to a full scan.
    /// </remarks>
    private static double GridCellSize(Container container, IReadOnlyList<PackageItem> items)
    {
        if (items.Count == 0)
        {
            return Math.Max(container.Dimensions.ShortestSide / 4, 1);
        }

        var sides = items.Select(i => i.Dimensions.LongestSide).OrderBy(v => v).ToArray();
        var median = sides[sides.Length / 2];

        return Math.Clamp(median, 1, Math.Max(container.Dimensions.LongestSide, 1));
    }
}
