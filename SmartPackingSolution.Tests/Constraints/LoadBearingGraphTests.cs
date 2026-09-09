namespace SmartPackingSolution.Tests.Constraints;

using FluentAssertions;
using SmartPackingSolution.Constraints;
using SmartPackingSolution.Geometry;
using SmartPackingSolution.Models;
using Xunit;

public class LoadBearingGraphTests
{
    private const double Tolerance = 1e-6;

    private static PlacedPackage Place(PackageItem item, double x, double y, double z) =>
        new(item, new Position(x, y, z));

    private static AxisAlignedBox BoxFor(PackageItem item, double x, double y, double z) =>
        new(new Position(x, y, z), item.Dimensions);

    [Fact]
    public void NothingMayRestOnAFragilePackage()
    {
        var graph = new LoadBearingGraph(Tolerance);
        var glass = new PackageItem("Glass", 10, 10, 10, 1, PackagePriority.Fragile);
        var book = new PackageItem("Book", 10, 10, 10, 1, PackagePriority.Medium);

        graph.Add(Place(glass, 0, 0, 0), []);

        graph.CanCarry(BoxFor(book, 0, 0, 10), book, [0]).Should().BeFalse();
    }

    [Fact]
    public void AFragilePackageBesideAnother_IsUnaffected()
    {
        // The rule this replaces compared only Z, so a fragile package forbade placements
        // at any greater height anywhere in the container.
        var graph = new LoadBearingGraph(Tolerance);
        var glass = new PackageItem("Glass", 10, 10, 10, 1, PackagePriority.Fragile);
        var crate = new PackageItem("Crate", 10, 10, 10, 5, PackagePriority.Heavy);
        var book = new PackageItem("Book", 10, 10, 10, 1, PackagePriority.Medium);

        graph.Add(Place(glass, 0, 0, 0), []);
        graph.Add(Place(crate, 50, 0, 0), [0]);

        graph.CanCarry(BoxFor(book, 50, 0, 10), book, [0, 1]).Should().BeTrue();
    }

    [Fact]
    public void LoadAccumulatesDownTheWholeChain()
    {
        var graph = new LoadBearingGraph(Tolerance);
        var baseItem = new PackageItem("Base", 10, 10, 10, 1, PackagePriority.Medium);
        graph.Add(Place(baseItem, 0, 0, 0), []);

        var indices = new List<int> { 0 };
        var height = 10.0;

        // Six 5kg slabs put 30kg on the base, exactly its capacity.
        for (var i = 0; i < 6; i++)
        {
            var slab = new PackageItem($"Slab {i}", 10, 10, 10, 5, PackagePriority.Medium);
            graph.CanCarry(BoxFor(slab, 0, 0, height), slab, indices).Should().BeTrue();
            indices.Add(graph.Add(Place(slab, 0, 0, height), indices));
            height += 10;
        }

        graph.LoadOn(0).Should().BeApproximately(30, 1e-9);

        var oneTooMany = new PackageItem("Slab 7", 10, 10, 10, 5, PackagePriority.Medium);
        graph.CanCarry(BoxFor(oneTooMany, 0, 0, height), oneTooMany, indices).Should().BeFalse();
    }

    [Fact]
    public void LoadIsSplitBetweenSupportersByContactArea()
    {
        var graph = new LoadBearingGraph(Tolerance);
        var wide = new PackageItem("Wide", 8, 10, 10, 1, PackagePriority.Heavy);
        var narrow = new PackageItem("Narrow", 2, 10, 10, 1, PackagePriority.Heavy);
        var deck = new PackageItem("Deck", 10, 10, 10, 10, PackagePriority.Heavy);

        graph.Add(Place(wide, 0, 0, 0), []);
        graph.Add(Place(narrow, 8, 0, 0), [0]);
        graph.Add(Place(deck, 0, 0, 10), [0, 1]);

        graph.LoadOn(0).Should().BeApproximately(8, 1e-9);
        graph.LoadOn(1).Should().BeApproximately(2, 1e-9);
    }

    [Fact]
    public void APackageSlidingUnderALoadedStack_MustBeAbleToCarryIt()
    {
        // Packing does not proceed strictly upwards. A package can drop into a gap beneath
        // something already placed and start carrying a share of the load above it, so a
        // check that only looks downwards lets a fragile item slip under a loaded stack.
        var graph = new LoadBearingGraph(Tolerance);
        var pillar = new PackageItem("Pillar", 5, 10, 10, 1, PackagePriority.Heavy);
        var deck = new PackageItem("Deck", 10, 10, 10, 20, PackagePriority.Heavy);
        var glass = new PackageItem("Glass", 5, 10, 10, 1, PackagePriority.Fragile);

        graph.Add(Place(pillar, 0, 0, 0), []);
        graph.Add(Place(deck, 0, 0, 10), [0]);

        // The gap at x=5..10, z=0..10 sits directly under the deck.
        graph.CanCarry(BoxFor(glass, 5, 0, 0), glass, [0, 1]).Should().BeFalse();
    }

    [Fact]
    public void APackageSlidingUnderAStack_RelievesTheOriginalSupporters()
    {
        var graph = new LoadBearingGraph(Tolerance);
        var pillar = new PackageItem("Pillar", 5, 10, 10, 1, PackagePriority.Heavy);
        var deck = new PackageItem("Deck", 10, 10, 10, 20, PackagePriority.Heavy);
        var filler = new PackageItem("Filler", 5, 10, 10, 1, PackagePriority.Heavy);

        graph.Add(Place(pillar, 0, 0, 0), []);
        graph.Add(Place(deck, 0, 0, 10), [0]);
        graph.LoadOn(0).Should().BeApproximately(20, 1e-9);

        graph.Add(Place(filler, 5, 0, 0), [0, 1]);

        graph.LoadOn(0).Should().BeApproximately(10, 1e-9, "the deck now rests on two pillars");
        graph.LoadOn(2).Should().BeApproximately(10, 1e-9);
    }
}
