namespace SmartPackingSolution.Constraints;

using SmartPackingSolution.Geometry;
using SmartPackingSolution.Models;

/// <summary>
/// Tracks how much weight rests on each placed package, and rejects placements that
/// would crush something below.
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
    /// Determines whether a candidate placement can be carried by what is beneath it.
    /// </summary>
    /// <param name="candidate">The box the package would occupy.</param>
    /// <param name="weight">The package's own weight in kilograms.</param>
    /// <param name="neighbourIndices">Indices of nearby registered packages to consider.</param>
    /// <returns>True if no package in the support chain would be overloaded.</returns>
    public bool CanCarry(in AxisAlignedBox candidate, double weight, IEnumerable<int> neighbourIndices)
    {
        ArgumentNullException.ThrowIfNull(neighbourIndices);

        var shares = SupporterShares(candidate, neighbourIndices);
        if (shares.Count == 0)
        {
            return true;
        }

        var added = new Dictionary<int, double>();
        foreach (var (index, share) in shares)
        {
            Accumulate(added, index, weight * share);
        }

        foreach (var (index, extra) in added)
        {
            if (_loadOn[index] + extra > _packages[index].Package.MaxSupportedWeight + _tolerance)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Registers a placement and propagates its weight down through its supporters.
    /// </summary>
    /// <param name="placed">The package that was placed.</param>
    /// <param name="neighbourIndices">Indices of nearby registered packages to consider.</param>
    /// <returns>The index assigned to the newly registered package.</returns>
    public int Add(PlacedPackage placed, IEnumerable<int> neighbourIndices)
    {
        ArgumentNullException.ThrowIfNull(placed);
        ArgumentNullException.ThrowIfNull(neighbourIndices);

        var shares = SupporterShares(placed.Bounds, neighbourIndices);

        _packages.Add(placed);
        _loadOn.Add(0);
        _supporters.Add(shares);

        var added = new Dictionary<int, double>();
        foreach (var (index, share) in shares)
        {
            Accumulate(added, index, placed.Package.Weight * share);
        }

        foreach (var (index, extra) in added)
        {
            _loadOn[index] += extra;
        }

        return _packages.Count - 1;
    }

    /// <summary>
    /// Finds which registered packages carry a candidate box, and what fraction of its
    /// weight each one takes.
    /// </summary>
    /// <param name="candidate">The box resting on the supporters.</param>
    /// <param name="neighbourIndices">Indices of nearby registered packages to consider.</param>
    /// <returns>Supporter indices paired with their share of the load, summing to one.</returns>
    private List<(int Index, double Share)> SupporterShares(
        in AxisAlignedBox candidate,
        IEnumerable<int> neighbourIndices)
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

        if (totalArea <= _tolerance)
        {
            return [];
        }

        return [.. contacts.Select(c => (c.Index, c.Area / totalArea))];
    }

    /// <summary>
    /// Adds a load to a package and passes it on to that package's own supporters, so the
    /// weight reaches the floor through every item in the chain.
    /// </summary>
    /// <param name="accumulator">Running totals, keyed by package index.</param>
    /// <param name="index">The package receiving the load.</param>
    /// <param name="load">The load in kilograms.</param>
    private void Accumulate(Dictionary<int, double> accumulator, int index, double load)
    {
        accumulator[index] = accumulator.GetValueOrDefault(index) + load;

        foreach (var (supporterIndex, share) in _supporters[index])
        {
            Accumulate(accumulator, supporterIndex, load * share);
        }
    }
}
