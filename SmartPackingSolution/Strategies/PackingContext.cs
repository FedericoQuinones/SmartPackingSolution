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

    // Reused across candidates for the same reason the index avoids allocating.
    private readonly List<int> _neighbourBuffer = [];
    private readonly List<AxisAlignedBox> _supporterBuffer = [];

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
    /// How much a placement's contact with its surroundings counts against its distance
    /// from the origin corner, when breaking ties under <see cref="PlacementScore.TightestFit"/>.
    /// </summary>
    /// <remarks>
    /// Calibrated over eight randomised loads: ignoring contact entirely costs about three
    /// and a half points of packed volume, and weighting it as heavily as position costs
    /// about two. The curve is flat between 0.1 and 0.2.
    /// </remarks>
    private const double ContactWeight = 0.2;

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
        var bestZ = double.PositiveInfinity;
        var bestTieBreak = double.PositiveInfinity;

        // Tracks how far the most promising candidate got, so the caller learns which
        // constraint actually blocked the package rather than a generic "no space".
        var sawGeometricFit = false;
        var sawSupportFailure = false;
        var sawLoadFailure = false;

        foreach (var anchor in _anchors.Ordered())
        {
            // Anchors arrive lowest first and height dominates every scoring rule, so once
            // a placement has been found no anchor above it can beat it. Without this the
            // search reads the whole anchor set for every package, which is what made
            // large loads quadratic in the anchor count.
            if (anchor.Z > bestZ + Options.Tolerance)
            {
                break;
            }

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

                _index.Query(box, NeighbourMargin, _neighbourBuffer);
                var neighbours = _neighbourBuffer;

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

                // Compared as a pair rather than a single number: height first, then the
                // tie-break. A scalar would need the tie-break scaled small enough never to
                // outweigh a height difference, and no scale is safe for every container.
                var tieBreak = TieBreakOf(box, neighbours);
                var lower = anchor.Z < bestZ - Options.Tolerance;
                var level = Math.Abs(anchor.Z - bestZ) <= Options.Tolerance;

                if (lower || (level && tieBreak < bestTieBreak))
                {
                    bestZ = anchor.Z;
                    bestTieBreak = tieBreak;
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
        var neighbours = new List<int>();
        _index.Query(placement.Bounds, NeighbourMargin, neighbours);

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

        _supporterBuffer.Clear();
        foreach (var index in neighbours)
        {
            _supporterBuffer.Add(_boxes[index]);
        }

        return SupportCalculator.SupportRatio(box, _supporterBuffer, Options.Tolerance)
            >= Options.MinimumSupportRatio - Options.Tolerance;
    }

    /// <summary>
    /// Ranks two placements that sit at the same height, lower being better.
    /// </summary>
    /// <param name="box">The candidate box.</param>
    /// <param name="neighbours">Nearby placed package indices.</param>
    /// <returns>A value from -1 to 1 under the configured <see cref="PlacementScore"/> rule.</returns>
    /// <remarks>
    /// Height itself is not part of this. Keeping the load low dominates every rule - it
    /// keeps the centre of gravity down and leaves the upper volume free - so the caller
    /// compares height first and only consults this when heights match.
    /// </remarks>
    private double TieBreakOf(in AxisAlignedBox box, List<int> neighbours)
    {
        var footprint = Math.Max(box.FootprintArea, 1);
        var contact = Saturate(ContactArea(box, neighbours) / (6 * footprint));

        return Options.Score switch
        {
            PlacementScore.DeepestBottomLeft => Corner(box),
            PlacementScore.MaxContactSurface => -contact,
            _ => Corner(box) - (ContactWeight * contact)
        };
    }

    /// <summary>
    /// Ranks a placement by how close it is to the container's origin corner.
    /// </summary>
    /// <param name="box">The candidate box.</param>
    /// <returns>A value from 0 at the origin corner to 1 at the far corner.</returns>
    private double Corner(in AxisAlignedBox box)
    {
        var alongLength = Saturate(box.MinX / Math.Max(Container.Dimensions.Length, 1));
        var alongWidth = Saturate(box.MinY / Math.Max(Container.Dimensions.Width, 1));

        return (alongLength * 0.75) + (alongWidth * 0.25);
    }

    private static double Saturate(double value) => Math.Clamp(value, 0, 1);

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
