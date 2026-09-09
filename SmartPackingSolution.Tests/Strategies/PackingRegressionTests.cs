namespace SmartPackingSolution.Tests.Strategies;

using FluentAssertions;
using SmartPackingSolution.Models;
using SmartPackingSolution.Strategies;
using SmartPackingSolution.Validation;
using Xunit;

/// <summary>
/// One test per defect found in the original engine, each failing against that engine
/// and passing against the current one.
/// </summary>
public class PackingRegressionTests
{
    [Fact]
    public void FragilePackage_ShouldNotBlockPlacementsInOtherColumns()
    {
        // The old stacking rule compared only Z. A fragile package near the floor
        // therefore forbade every placement above its top face anywhere in the
        // container, even in a column that never touches it.
        var container = new Container(100, 50, 60, 1000);
        var packages = new List<PackageItem>
        {
            // Rotation is off so the container holds exactly two boxes per level and the
            // third has no choice but to stack, which is the situation under test.
            new PackageItem("Glassware", 50, 50, 20, 2, PackagePriority.Fragile, allowRotation: false),
            new PackageItem("Crate", 50, 50, 20, 8, PackagePriority.Heavy, allowRotation: false),
            new PackageItem("Toolbox", 50, 50, 20, 10, PackagePriority.Medium, allowRotation: false)
        };

        var result = new BestFitDecreasingStrategy().Pack(
            container,
            packages,
            PackingOptions.Default with { Ordering = ItemOrdering.AsSupplied });

        result.PackedItems.Should().HaveCount(3, "nothing physically prevents three boxes here");

        var glassware = result.PackedItems.Single(p => p.Package.Name == "Glassware");
        var toolbox = result.PackedItems.Single(p => p.Package.Name == "Toolbox");
        var crate = result.PackedItems.Single(p => p.Package.Name == "Crate");

        toolbox.Position.Z.Should().BeGreaterThan(0, "the third box has to go on top of one of the others");
        toolbox.RestsOn(crate).Should().BeTrue();
        toolbox.RestsOn(glassware).Should().BeFalse("nothing may rest on a fragile package");
        result.Validate().IsValid.Should().BeTrue();
    }

    [Fact]
    public void Packages_ShouldNeverBePlacedOverEmptySpace()
    {
        // The old engine generated an anchor at the top of every placed package but never
        // checked that a candidate was actually resting on anything, so packages ended up
        // suspended at the height of a neighbouring stack over open floor.
        var container = new Container(200, 200, 200, 100000);
        var rng = new Random(7);
        var packages = Enumerable.Range(1, 80)
            .Select(i => new PackageItem(
                $"Box {i}",
                rng.Next(20, 61),
                rng.Next(20, 61),
                rng.Next(20, 61),
                rng.Next(1, 4),
                PackagePriority.Heavy))
            .ToList();

        var result = new BestFitDecreasingStrategy().Pack(container, packages);
        var report = result.Validate();

        report.CountOf(ViolationKind.Floating).Should().Be(0, "{0}", report);
        result.PackedItems.Should().NotBeEmpty();
    }

    [Fact]
    public void Packing_ShouldUseOrientationsTheOldEnumCouldNotExpress()
    {
        // A 10x20x30 package fits a 30x10x20 slot only as HLW - one of the two of the six
        // axis-aligned orientations that the original four-value RotationType omitted.
        var container = new Container(30, 10, 20, 1000);
        var packages = new List<PackageItem>
        {
            new PackageItem("Awkward", 10, 20, 30, 5)
        };

        var result = new BestFitDecreasingStrategy().Pack(container, packages);

        result.PackedItems.Should().ContainSingle()
            .Which.Orientation.Should().Be(Orientation.HLW);
    }

    [Fact]
    public void CandidatePositions_ThatDifferByLessThanTolerance_ShouldDeduplicate()
    {
        // Anchors are produced by repeated addition, so two anchors describing the same
        // corner differ in their last bits. Record equality is exact, so the original
        // HashSet<Position> never de-duplicated them and the candidate set grew unbounded.
        var accumulated = 0.0;
        for (var i = 0; i < 10; i++)
        {
            accumulated += 0.1;
        }

        var byArithmetic = new Position(accumulated, 0, 0);
        var byLiteral = new Position(1.0, 0, 0);

        byArithmetic.Should().NotBe(byLiteral, "floating point addition does not land exactly on 1.0");

        var set = new HashSet<Position>
        {
            byArithmetic.Quantize(1e-4),
            byLiteral.Quantize(1e-4)
        };

        set.Should().ContainSingle("quantised positions collapse onto the same grid point");
    }

    [Fact]
    public void AccumulatedWeight_ShouldBeCheckedAgainstTheWholeSupportChain()
    {
        // The old rule only compared a package with the one directly beneath it, so an
        // unbounded stack could pile onto a single medium package without tripping a check.
        var container = new Container(100, 100, 200, 100000);
        var packages = new List<PackageItem>
        {
            new PackageItem("Base", 100, 100, 10, 1, PackagePriority.Medium, allowRotation: false)
        };

        packages.AddRange(Enumerable.Range(1, 10).Select(i =>
            new PackageItem($"Slab {i}", 100, 100, 10, 5, PackagePriority.Medium, allowRotation: false)));

        var result = new BestFitDecreasingStrategy().Pack(
            container,
            packages,
            PackingOptions.Default with { Ordering = ItemOrdering.AsSupplied });

        result.UnpackedItems.Should().Contain(u => u.Reason == UnpackedReason.LoadBearingConstraint,
            "a medium package carries 30kg, so the stack cannot keep growing at 5kg a slab");
        result.Validate().IsValid.Should().BeTrue();
    }
}
