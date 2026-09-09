namespace SmartPackingSolution.Tests.Geometry;

using FluentAssertions;
using SmartPackingSolution.Geometry;
using SmartPackingSolution.Models;
using Xunit;

public class SupportCalculatorTests
{
    private const double Tolerance = 1e-6;

    private static AxisAlignedBox Box(double x, double y, double z, double l, double w, double h) =>
        new(new Position(x, y, z), new Dimensions(l, w, h));

    [Fact]
    public void PackagesOnTheFloor_AreFullySupported()
    {
        SupportCalculator.SupportRatio(Box(0, 0, 0, 10, 10, 10), [], Tolerance).Should().Be(1);
    }

    [Fact]
    public void PackagesOverEmptySpace_AreUnsupported()
    {
        var floating = Box(0, 0, 20, 10, 10, 10);
        var elsewhere = Box(90, 90, 0, 10, 10, 20);

        SupportCalculator.SupportRatio(floating, [elsewhere], Tolerance).Should().Be(0);
    }

    [Fact]
    public void PartialOverhang_IsMeasuredAsAFraction()
    {
        // Half the base rests on the supporter, half hangs over nothing.
        var candidate = Box(0, 0, 10, 10, 10, 10);
        var supporter = Box(0, 0, 0, 5, 10, 10);

        SupportCalculator.SupportRatio(candidate, [supporter], Tolerance).Should().BeApproximately(0.5, 1e-9);
    }

    [Fact]
    public void AbuttingSupporters_AreNotDoubleCounted()
    {
        // Two supporters meeting edge to edge cover the base exactly once. Summing their
        // individual overlaps instead of their union would report more than full support
        // wherever supporters share an edge.
        var candidate = Box(0, 0, 10, 10, 10, 10);
        var left = Box(0, 0, 0, 5, 10, 10);
        var right = Box(5, 0, 0, 5, 10, 10);

        SupportCalculator.SupportRatio(candidate, [left, right], Tolerance)
            .Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void OverlappingSupporters_AreCountedOnce()
    {
        var candidate = Box(0, 0, 10, 10, 10, 10);
        var wide = Box(0, 0, 0, 8, 10, 10);
        var overlapping = Box(4, 0, 0, 6, 10, 10);

        SupportCalculator.SupportRatio(candidate, [wide, overlapping], Tolerance)
            .Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void SupportersAtOtherHeights_DoNotCount()
    {
        var candidate = Box(0, 0, 10, 10, 10, 10);
        var tooLow = Box(0, 0, 0, 10, 10, 5);

        SupportCalculator.SupportRatio(candidate, [tooLow], Tolerance).Should().Be(0);
    }
}
