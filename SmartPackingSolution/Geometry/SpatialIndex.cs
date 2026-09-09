namespace SmartPackingSolution.Geometry;

/// <summary>
/// A uniform-grid index over the boxes placed so far, used to find the neighbours of a
/// candidate placement without scanning every placed package.
/// </summary>
/// <remarks>
/// The naive packer compared each candidate against every placed package, making the
/// overall run cubic in the number of packages. Modelling support and load bearing needs
/// several such queries per candidate, so a broad-phase index is what keeps a physically
/// realistic packer affordable at scale.
/// </remarks>
public sealed class SpatialIndex
{
    private readonly double _cellSize;
    private readonly Dictionary<(int X, int Y, int Z), List<int>> _cells = [];

    /// <summary>
    /// Initializes a new index.
    /// </summary>
    /// <param name="cellSize">
    /// The edge length of a grid cell. Best set near the typical package dimension:
    /// much smaller and boxes span many cells, much larger and cells stop discriminating.
    /// </param>
    public SpatialIndex(double cellSize)
    {
        _cellSize = cellSize > 0 ? cellSize : 1;
    }

    /// <summary>
    /// Registers a box under the identifier <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The caller's identifier for the box, typically a list index.</param>
    /// <param name="box">The box to index.</param>
    public void Add(int id, in AxisAlignedBox box)
    {
        var (minX, minY, minZ) = CellOf(box.MinX, box.MinY, box.MinZ);
        var (maxX, maxY, maxZ) = CellOf(box.MaxX, box.MaxY, box.MaxZ);

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    var key = (x, y, z);
                    if (!_cells.TryGetValue(key, out var bucket))
                    {
                        bucket = [];
                        _cells[key] = bucket;
                    }

                    bucket.Add(id);
                }
            }
        }
    }

    /// <summary>
    /// Returns the identifiers of every box that could intersect the query region.
    /// </summary>
    /// <param name="query">The region to search, typically a candidate placement.</param>
    /// <param name="margin">
    /// An outward expansion of the query, so boxes merely touching the region - the ones
    /// that support it or bear against it - are also returned.
    /// </param>
    /// <returns>Candidate identifiers, each returned once. May contain false positives.</returns>
    public IEnumerable<int> Query(in AxisAlignedBox query, double margin = 0)
    {
        var (minX, minY, minZ) = CellOf(query.MinX - margin, query.MinY - margin, query.MinZ - margin);
        var (maxX, maxY, maxZ) = CellOf(query.MaxX + margin, query.MaxY + margin, query.MaxZ + margin);

        var seen = new HashSet<int>();
        var results = new List<int>();

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    if (!_cells.TryGetValue((x, y, z), out var bucket))
                    {
                        continue;
                    }

                    foreach (var id in bucket)
                    {
                        if (seen.Add(id))
                        {
                            results.Add(id);
                        }
                    }
                }
            }
        }

        return results;
    }

    private (int X, int Y, int Z) CellOf(double x, double y, double z) =>
        ((int)Math.Floor(x / _cellSize),
         (int)Math.Floor(y / _cellSize),
         (int)Math.Floor(z / _cellSize));
}
