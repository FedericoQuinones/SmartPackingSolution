namespace SmartPackingSolution.Geometry;

using SmartPackingSolution.Models;

/// <summary>
/// An axis-aligned bounding box in container space, used for every geometric test
/// performed while packing.
/// </summary>
/// <remarks>
/// All comparisons take an explicit tolerance. Packing arithmetic accumulates
/// coordinates by repeated addition, so two faces that are meant to be flush routinely
/// differ by a few ULPs; treating those as an overlap would reject valid placements.
/// </remarks>
public readonly record struct AxisAlignedBox
{
    /// <summary>
    /// Initializes a new box from its minimum corner and its size.
    /// </summary>
    /// <param name="origin">The minimum corner of the box.</param>
    /// <param name="size">The extent of the box along each axis.</param>
    public AxisAlignedBox(Position origin, Dimensions size)
    {
        MinX = origin.X;
        MinY = origin.Y;
        MinZ = origin.Z;
        MaxX = origin.X + size.Length;
        MaxY = origin.Y + size.Width;
        MaxZ = origin.Z + size.Height;
    }

    /// <summary>Gets the lower bound on the X axis.</summary>
    public double MinX { get; init; }

    /// <summary>Gets the lower bound on the Y axis.</summary>
    public double MinY { get; init; }

    /// <summary>Gets the lower bound on the Z axis.</summary>
    public double MinZ { get; init; }

    /// <summary>Gets the upper bound on the X axis.</summary>
    public double MaxX { get; init; }

    /// <summary>Gets the upper bound on the Y axis.</summary>
    public double MaxY { get; init; }

    /// <summary>Gets the upper bound on the Z axis.</summary>
    public double MaxZ { get; init; }

    /// <summary>Gets the height of the box's top face, in container coordinates.</summary>
    public double TopZ => MaxZ;

    /// <summary>Gets the height of the box's bottom face, in container coordinates.</summary>
    public double BottomZ => MinZ;

    /// <summary>Gets the area of the box's footprint on the XY plane.</summary>
    public double FootprintArea => (MaxX - MinX) * (MaxY - MinY);

    /// <summary>Gets the volume of the box.</summary>
    public double Volume => (MaxX - MinX) * (MaxY - MinY) * (MaxZ - MinZ);

    /// <summary>Gets the geometric centre of the box.</summary>
    public Position Center => new((MinX + MaxX) / 2, (MinY + MaxY) / 2, (MinZ + MaxZ) / 2);

    /// <summary>
    /// Determines whether this box shares interior volume with another.
    /// </summary>
    /// <param name="other">The box to test against.</param>
    /// <param name="tolerance">Contact slack; boxes that merely touch do not overlap.</param>
    /// <returns>True if the two boxes intersect in all three axes.</returns>
    public bool Overlaps(in AxisAlignedBox other, double tolerance) =>
        MinX < other.MaxX - tolerance && MaxX > other.MinX + tolerance &&
        MinY < other.MaxY - tolerance && MaxY > other.MinY + tolerance &&
        MinZ < other.MaxZ - tolerance && MaxZ > other.MinZ + tolerance;

    /// <summary>
    /// Computes the area of the overlap between the two boxes' footprints on the XY plane.
    /// </summary>
    /// <param name="other">The box to intersect with.</param>
    /// <returns>The shared footprint area, or zero if the footprints are disjoint.</returns>
    public double FootprintIntersectionArea(in AxisAlignedBox other)
    {
        var dx = Math.Min(MaxX, other.MaxX) - Math.Max(MinX, other.MinX);
        var dy = Math.Min(MaxY, other.MaxY) - Math.Max(MinY, other.MinY);

        return dx <= 0 || dy <= 0 ? 0 : dx * dy;
    }

    /// <summary>
    /// Computes the total area of the two boxes' contact on a shared face, on any axis.
    /// </summary>
    /// <param name="other">The box to measure contact with.</param>
    /// <param name="tolerance">How close two faces must be to count as touching.</param>
    /// <returns>The contact area in square centimeters.</returns>
    public double ContactArea(in AxisAlignedBox other, double tolerance)
    {
        var dx = Math.Min(MaxX, other.MaxX) - Math.Max(MinX, other.MinX);
        var dy = Math.Min(MaxY, other.MaxY) - Math.Max(MinY, other.MinY);
        var dz = Math.Min(MaxZ, other.MaxZ) - Math.Max(MinZ, other.MinZ);

        var touchesX = Math.Abs(MaxX - other.MinX) <= tolerance || Math.Abs(other.MaxX - MinX) <= tolerance;
        var touchesY = Math.Abs(MaxY - other.MinY) <= tolerance || Math.Abs(other.MaxY - MinY) <= tolerance;
        var touchesZ = Math.Abs(MaxZ - other.MinZ) <= tolerance || Math.Abs(other.MaxZ - MinZ) <= tolerance;

        var area = 0.0;
        if (touchesX && dy > 0 && dz > 0) { area += dy * dz; }
        if (touchesY && dx > 0 && dz > 0) { area += dx * dz; }
        if (touchesZ && dx > 0 && dy > 0) { area += dx * dy; }

        return area;
    }

    /// <summary>
    /// Determines whether this box lies entirely within the given container dimensions,
    /// anchored at the origin.
    /// </summary>
    /// <param name="bounds">The container's dimensions.</param>
    /// <param name="tolerance">Slack allowed on each face.</param>
    /// <returns>True if the box is fully inside the container.</returns>
    public bool IsInside(Dimensions bounds, double tolerance) =>
        MinX >= -tolerance && MinY >= -tolerance && MinZ >= -tolerance &&
        MaxX <= bounds.Length + tolerance &&
        MaxY <= bounds.Width + tolerance &&
        MaxZ <= bounds.Height + tolerance;
}
