namespace SmartPackingSolution.Geometry;

/// <summary>
/// Measures how much of a candidate placement's base actually rests on something.
/// </summary>
/// <remarks>
/// Without this check a packer will happily place a box at the height of a neighbouring
/// stack but over empty floor, producing a layout that looks dense on paper and collapses
/// in reality. Support is the constraint that makes a 3D packing physically loadable.
/// </remarks>
public static class SupportCalculator
{
    /// <summary>
    /// Computes the fraction of <paramref name="candidate"/>'s base area that is supported.
    /// </summary>
    /// <param name="candidate">The placement under consideration.</param>
    /// <param name="supporters">
    /// Boxes that may be underneath. Only those whose top face is flush with the
    /// candidate's base contribute.
    /// </param>
    /// <param name="tolerance">How close two faces must be to count as contact.</param>
    /// <returns>A value from 0 (floating) to 1 (fully supported).</returns>
    public static double SupportRatio(
        in AxisAlignedBox candidate,
        IReadOnlyList<AxisAlignedBox> supporters,
        double tolerance)
    {
        ArgumentNullException.ThrowIfNull(supporters);

        var baseArea = candidate.FootprintArea;
        if (baseArea <= tolerance)
        {
            return 1;
        }

        // Resting on the container floor is full support.
        if (candidate.MinZ <= tolerance)
        {
            return 1;
        }

        var contacts = new List<AxisAlignedBox>();
        foreach (var supporter in supporters)
        {
            if (Math.Abs(supporter.MaxZ - candidate.MinZ) > tolerance)
            {
                continue;
            }

            if (candidate.FootprintIntersectionArea(supporter) > tolerance)
            {
                contacts.Add(supporter);
            }
        }

        if (contacts.Count == 0)
        {
            return 0;
        }

        return UnionFootprintArea(candidate, contacts) / baseArea;
    }

    /// <summary>
    /// Computes the area of the union of the supporters' footprints, clipped to the
    /// candidate's own footprint.
    /// </summary>
    /// <param name="candidate">The placement whose base is being covered.</param>
    /// <param name="contacts">The boxes touching the candidate's base.</param>
    /// <returns>The covered area in square centimeters.</returns>
    /// <remarks>
    /// Summing the individual overlaps would double-count where two supporters abut, so
    /// the union is computed exactly by coordinate compression: the distinct clipped
    /// edges cut the footprint into a grid of cells, and each cell is counted once if any
    /// supporter covers it. The contact set is a handful of boxes in practice, so the
    /// quadratic cell sweep is cheaper than maintaining a sweep-line structure.
    /// </remarks>
    private static double UnionFootprintArea(
        in AxisAlignedBox candidate,
        List<AxisAlignedBox> contacts)
    {
        var xs = new SortedSet<double> { candidate.MinX, candidate.MaxX };
        var ys = new SortedSet<double> { candidate.MinY, candidate.MaxY };

        foreach (var contact in contacts)
        {
            if (contact.MinX > candidate.MinX && contact.MinX < candidate.MaxX) { xs.Add(contact.MinX); }
            if (contact.MaxX > candidate.MinX && contact.MaxX < candidate.MaxX) { xs.Add(contact.MaxX); }
            if (contact.MinY > candidate.MinY && contact.MinY < candidate.MaxY) { ys.Add(contact.MinY); }
            if (contact.MaxY > candidate.MinY && contact.MaxY < candidate.MaxY) { ys.Add(contact.MaxY); }
        }

        var xEdges = xs.ToArray();
        var yEdges = ys.ToArray();
        var covered = 0.0;

        for (var i = 0; i < xEdges.Length - 1; i++)
        {
            var cellMinX = xEdges[i];
            var cellMaxX = xEdges[i + 1];
            var midX = (cellMinX + cellMaxX) / 2;

            for (var j = 0; j < yEdges.Length - 1; j++)
            {
                var cellMinY = yEdges[j];
                var cellMaxY = yEdges[j + 1];
                var midY = (cellMinY + cellMaxY) / 2;

                foreach (var contact in contacts)
                {
                    if (midX >= contact.MinX && midX <= contact.MaxX &&
                        midY >= contact.MinY && midY <= contact.MaxY)
                    {
                        covered += (cellMaxX - cellMinX) * (cellMaxY - cellMinY);
                        break;
                    }
                }
            }
        }

        return covered;
    }
}
