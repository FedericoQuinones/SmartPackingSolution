namespace SmartPackingSolution.Tests.Geometry;

using FluentAssertions;
using SmartPackingSolution.Geometry;
using SmartPackingSolution.Models;
using Xunit;

public class AxisAlignedBoxTests
{
    private static AxisAlignedBox Box(double x, double y, double z, double l, double w, double h) =>
        new(new Position(x, y, z), new Dimensions(l, w, h));

    [Fact]
    public void Overlaps_WhenBoxesShareVolume_ShouldReturnTrue()
    {
        Box(0, 0, 0, 10, 10, 10).Overlaps(Box(5, 5, 5, 10, 10, 10), 1e-6).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WhenBoxesOnlyTouch_ShouldReturnFalse()
    {
        // Packing stacks boxes face to face constantly; treating contact as overlap would
        // reject every legitimate placement.
        Box(0, 0, 0, 10, 10, 10).Overlaps(Box(10, 0, 0, 10, 10, 10), 1e-6).Should().BeFalse();
        Box(0, 0, 0, 10, 10, 10).Overlaps(Box(0, 0, 10, 10, 10, 10), 1e-6).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_WhenFacesDriftByLessThanTolerance_ShouldReturnFalse()
    {
        // Coordinates accumulate by repeated addition, so faces meant to be flush end up a
        // few ULPs apart. Without tolerance those drifts read as overlaps.
        var a = Box(0, 0, 0, 10, 10, 10);
        var b = Box(10 - 1e-9, 0, 0, 10, 10, 10);

        a.Overlaps(b, 1e-6).Should().BeFalse();
    }

    [Fact]
    public void FootprintIntersectionArea_ShouldMeasureTheSharedBase()
    {
        Box(0, 0, 0, 10, 10, 5).FootprintIntersectionArea(Box(5, 5, 5, 10, 10, 5)).Should().Be(25);
        Box(0, 0, 0, 10, 10, 5).FootprintIntersectionArea(Box(20, 20, 0, 10, 10, 5)).Should().Be(0);
    }

    [Fact]
    public void IsInside_ShouldRejectBoxesCrossingAnyWall()
    {
        var bounds = new Dimensions(100, 100, 100);

        Box(0, 0, 0, 100, 100, 100).IsInside(bounds, 1e-6).Should().BeTrue();
        Box(50, 0, 0, 60, 10, 10).IsInside(bounds, 1e-6).Should().BeFalse();
        Box(-1, 0, 0, 10, 10, 10).IsInside(bounds, 1e-6).Should().BeFalse();
    }

    [Fact]
    public void ContactArea_ShouldCountTouchingFacesOnly()
    {
        var seated = Box(0, 0, 0, 10, 10, 10);

        seated.ContactArea(Box(0, 0, 10, 10, 10, 10), 1e-6).Should().Be(100);
        seated.ContactArea(Box(0, 0, 50, 10, 10, 10), 1e-6).Should().Be(0);
    }

    [Fact]
    public void Center_ShouldBeTheGeometricMiddle()
    {
        Box(10, 20, 30, 10, 10, 10).Center.Should().Be(new Position(15, 25, 35));
    }
}
