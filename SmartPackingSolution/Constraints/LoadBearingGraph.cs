namespace SmartPackingSolution.Constraints;

using SmartPackingSolution.Geometry;
using SmartPackingSolution.Models;

/// <summary>
/// Tracks how much weight rests on each placed package, and rejects placements that
/// would crush something.
/// </summary>
/// <remarks>
/// <para>
/// This replaces pairwise priority comparison, which was wrong in two directions. It
/// compared only the Z coordinate, so a fragile item near the floor blocked every
/// placement above its top face anywhere in the container - including on the far side,
/// metres away, with nothing between them. And because it only looked at the item
/// immediately below, an unbounded stack of medium items could accumulate on a single
/// medium item without ever tripping a check.
/// </para>
/// <para>
/// Here, load is transmitted down the support chain and split between supporters in
/// proportion to the base area each one carries, which is how the weight actually
/// travels. Fragility falls out of the same mechanism: a fragile package has a capacity
/// of zero kilograms, so nothing may rest on it - but only when something is genuinely
/// above it in XY.
/// </para>
/// <para>
/// A placement is checked in both directions. Packing does not proceed strictly upwards:
/// a package can drop into a gap beneath something already placed, and by closing that
/// gap it starts carrying a share of the load above. Checking only what a package rests
/// on lets a fragile item slide underneath a loaded stack unnoticed.
/// </para>
/// </remarks>
public sealed class LoadBearingGraph
{
    private readonly double _tolerance;
    private readonly List<PlacedPackage> _packages = [];
    private readonly List<double> _loadOn = [];
    private readonly List<List<(int Index, double Share)>> _supporters = [];

    /// <summary>
    /// Initializes a new graph.
    /// </summary>
    /// <param name="tolerance">How close two faces must be to count as contact.</param>
    public LoadBearingGraph(double tolerance)
    {
        _tolerance = tolerance;
    }

    /// <summary>
    /// Gets the weight, in kilograms, currently resting on the package at
    /// <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index of a registered package.</param>
    /// <returns>The accumulated load in kilograms.</returns>
    public double LoadOn(int index) => _loadOn[index];

    /// <summary>
    /// Determines whether a candidate placement can be carried by what is beneath it, and
    /// whether it could carry whatever it would end up beneath.
    /// </summary>
    /// <param name="candidate">The box the package would occupy.</param>
    /// <param name="package">The package being placed.</param>
    /// <param name="neighbourIndices">Indices of nearby registered packages to consider.</param>
    /// <returns>True if no package would be overloaded, the candidate included.</returns>
    public bool CanCarry(in AxisAlignedBox candidate, PackageItem package, IEnumerable<int> neighbourIndices)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(neighbourIndices);

        var neighbours = neighbourIndices as IReadOnlyList<int> ?? neighbourIndices.ToList();

        // What the candidate would inherit from packages it slides underneath.
        var inherited = 0.0;
        foreach (var (_, share, transmitted) in OverheadShares(candidate, neighbours))
        {
            inherited += share * transmitted;
        }

        if (inherited > package.MaxSupportedWeight + _tolerance)
        {
            return false;
        }

        // Everything the candidate carries, its own weight included, presses downwards.
        var shares = SupporterShares(candidate, neighbours);
        if (shares.Count == 0)
        {
            return true;
        }

        var deltas = new Dictionary<int, double>();
        foreach (var (index, share) in shares)
        {
            Accumulate(deltas, index, (package.Weight + inherited) * share);
        }

        foreach (var (index, extra) in deltas)
        {
            if (_loadOn[index] + extra > _packages[index].Package.MaxSupportedWeight + _tolerance)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Registers a placement, propagates its weight down through its supporters, and
    /// re-links any package it has slid underneath.
    /// </summary>
    /// <param name="placed">The package that was placed.</param>
    /// <param name="neighbourIndices">Indices of nearby registered packages to consider.</param>
    /// <returns>The index assigned to the newly registered package.</returns>
    public int Add(PlacedPackage placed, IEnumerable<int> neighbourIndices)
    {
        ArgumentNullException.ThrowIfNull(placed);
        ArgumentNullException.ThrowIfNull(neighbourIndices);

        var neighbours = neighbourIndices as IReadOnlyList<int> ?? neighbourIndices.ToList();
        var overhead = OverheadShares(placed.Bounds, neighbours);
        var shares = SupporterShares(placed.Bounds, neighbours);

        var index = _packages.Count;
        _packages.Add(placed);
        _loadOn.Add(0);
        _supporters.Add(shares);

        // Take over a share of everything now resting on this package, releasing the
        // supporters that were carrying that share on their own.
        foreach (var (above, share, transmitted) in overhead)
        {
            foreach (var (oldSupporter, oldShare) in _supporters[above])
            {
                Propagate(oldSupporter, -transmitted * oldShare);
            }

            _supporters[above] = RecomputeShares(above, index, neighbours);

            foreach (var (newSupporter, newShare) in _supporters[above])
            {
                Propagate(newSupporter, transmitted * newShare);
            }
        }

        Propagate(index, placed.Package.Weight, includeSelf: false);

        return index;
    }

    /// <summary>
    /// Finds which registered packages carry a candidate box, and what fraction of the
    /// load each one takes.
    /// </summary>
    /// <param name="candidate">The box resting on the supporters.</param>
    /// <param name="neighbourIndices">Indices of nearby registered packages to consider.</param>
    /// <returns>Supporter indices paired with their share of the load, summing to one.</returns>
    private List<(int Index, double Share)> SupporterShares(
        in AxisAlignedBox candidate,
        IReadOnlyList<int> neighbourIndices)
    {
        var contacts = new List<(int Index, double Area)>();
        var totalArea = 0.0;

        foreach (var index in neighbourIndices)
        {
            var other = _packages[index].Bounds;
            if (Math.Abs(other.MaxZ - candidate.MinZ) > _tolerance)
            {
                continue;
            }

            var area = candidate.FootprintIntersectionArea(other);
            if (area > _tolerance)
            {
                contacts.Add((index, area));
                totalArea += area;
            }
        }

        return totalArea <= _tolerance
            ? []
            : [.. contacts.Select(c => (c.Index, c.Area / totalArea))];
    }

    /// <summary>
    /// Finds the registered packages a candidate box would slide underneath, the share of
    /// their load it would take on, and how much load each is passing down.
    /// </summary>
    /// <param name="candidate">The box being placed.</param>
    /// <param name="neighbourIndices">Indices of nearby registered packages to consider.</param>
    /// <returns>The package above, the candidate's share of it, and its transmitted load.</returns>
    private List<(int Above, double Share, double Transmitted)> OverheadShares(
        in AxisAlignedBox candidate,
        IReadOnlyList<int> neighbourIndices)
    {
        var results = new List<(int, double, double)>();

        foreach (var index in neighbourIndices)
        {
            var above = _packages[index].Bounds;
            if (Math.Abs(candidate.MaxZ - above.MinZ) > _tolerance)
            {
                continue;
            }

            var newArea = above.FootprintIntersectionArea(candidate);
            if (newArea <= _tolerance)
            {
                continue;
            }

            var existingArea = 0.0;
            foreach (var (supporter, _) in _supporters[index])
            {
                existingArea += above.FootprintIntersectionArea(_packages[supporter].Bounds);
            }

            var share = newArea / (existingArea + newArea);
            var transmitted = _packages[index].Package.Weight + _loadOn[index];

            results.Add((index, share, transmitted));
        }

        return results;
    }

    /// <summary>
    /// Recomputes a package's supporter shares once a new package has joined them.
    /// </summary>
    /// <param name="above">The package whose supporters changed.</param>
    /// <param name="newSupporter">The index of the package that joined.</param>
    /// <param name="neighbourIndices">Indices of nearby registered packages to consider.</param>
    /// <returns>The updated supporter shares.</returns>
    private List<(int Index, double Share)> RecomputeShares(
        int above,
        int newSupporter,
        IReadOnlyList<int> neighbourIndices)
    {
        var candidates = _supporters[above]
            .Select(s => s.Index)
            .Append(newSupporter)
            .Concat(neighbourIndices)
            .Distinct()
            .ToList();

        return SupporterShares(_packages[above].Bounds, candidates);
    }

    /// <summary>
    /// Adds a load to a package and passes it on to that package's own supporters, so the
    /// weight reaches the floor through every item in the chain.
    /// </summary>
    /// <param name="index">The package receiving the load.</param>
    /// <param name="load">The load in kilograms; negative values release load.</param>
    /// <param name="includeSelf">Whether the load counts against this package's own capacity.</param>
    private void Propagate(int index, double load, bool includeSelf = true)
    {
        if (includeSelf)
        {
            _loadOn[index] += load;
        }

        foreach (var (supporter, share) in _supporters[index])
        {
            Propagate(supporter, load * share);
        }
    }

    /// <summary>
    /// Records a load against a package and its supporters without mutating the graph,
    /// so a candidate placement can be tested before it is committed.
    /// </summary>
    /// <param name="accumulator">Running totals, keyed by package index.</param>
    /// <param name="index">The package receiving the load.</param>
    /// <param name="load">The load in kilograms.</param>
    private void Accumulate(Dictionary<int, double> accumulator, int index, double load)
    {
        accumulator[index] = accumulator.GetValueOrDefault(index) + load;

        foreach (var (supporter, share) in _supporters[index])
        {
            Accumulate(accumulator, supporter, load * share);
        }
    }
}
