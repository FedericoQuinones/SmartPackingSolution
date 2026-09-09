namespace SmartPackingSolution.Tests.Integration;

using FluentAssertions;
using SmartPackingSolution.Algorithms;
using SmartPackingSolution.Models;
using SmartPackingSolution.Strategies;
using SmartPackingSolution.Validation;
using Xunit;
using Xunit.Abstractions;

public class RealWorldScenarioTests(ITestOutputHelper output)
{
    [Fact]
    public void Scenario_MovingTruck_ShouldPackFurnitureAndFragileItems()
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var truck = new Container(600, 240, 240, 5000, "Moving truck");

        var packages = new List<PackageItem>
        {
            new PackageItem("Desk", 150, 80, 75, 50, PackagePriority.Heavy),
            new PackageItem("Chair", 60, 60, 90, 15, PackagePriority.Medium),
            new PackageItem("TV Box", 120, 80, 20, 12, PackagePriority.Fragile),
            new PackageItem("Books Box", 40, 30, 30, 20, PackagePriority.Medium),
            new PackageItem("Lamp", 30, 30, 60, 3, PackagePriority.Fragile),
            new PackageItem("Mattress", 200, 150, 30, 25, PackagePriority.Medium),
            new PackageItem("Pillows", 60, 60, 40, 2, PackagePriority.Light)
        };

        // Act
        var result = optimizer.OptimizePacking(truck, packages);
        var report = result.Validate();

        // Assert - the original version of this test looped over fragile and heavy items
        // with a bare `continue` in the body, so it asserted nothing at all. The validator
        // checks the property it was reaching for, and every other one besides.
        result.IsFullyPacked.Should().BeTrue();
        report.IsValid.Should().BeTrue("{0}", report);
        result.TotalWeight.Should().BeLessOrEqualTo(truck.MaxWeight);

        report.CountOf(ViolationKind.StackedOnFragile).Should().Be(0);
        report.CountOf(ViolationKind.Floating).Should().Be(0);
    }

    [Fact]
    public void Scenario_WarehouseShipment_ShouldMaximizeSpaceUtilization()
    {
        // Arrange - the container is deliberately too small for the load, so utilization
        // measures the packing rather than how generously the container was sized.
        var optimizer = new ContainerOptimizer();
        var container = new Container(120, 100, 100, 100000, "Pallet crate");

        var rng = new Random(4242);
        var packages = Enumerable.Range(1, 120)
            .Select(i => new PackageItem(
                $"Package {i}",
                rng.Next(15, 46),
                rng.Next(15, 46),
                rng.Next(10, 41),
                rng.Next(1, 6),
                PackagePriority.Heavy))
            .ToList();

        // Act
        var result = optimizer.OptimizePacking(container, packages);
        var report = result.Validate();

        // Assert
        report.IsValid.Should().BeTrue("{0}", report);
        result.SpaceUtilization.Should().BeGreaterThan(0.80,
            "a physically valid layout of sturdy boxes should still fill most of the crate");

        output.WriteLine($"Packed: {result.PackedItems.Count}/{packages.Count}");
        output.WriteLine($"Space utilization: {result.SpaceUtilization:P2}");
        output.WriteLine($"Load balance: {result.LoadBalanceScore:F3}");
        output.WriteLine($"Elapsed: {result.Elapsed.TotalMilliseconds:F0} ms");
    }

    [Fact]
    public void Scenario_GroceryDelivery_ShouldProtectFragileItems()
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var deliveryBox = new Container(60, 40, 40, 30, "Delivery crate");

        var packages = new List<PackageItem>
        {
            new PackageItem("Water Bottles (6-pack)", 30, 20, 25, 6, PackagePriority.Heavy),
            new PackageItem("Bread", 25, 15, 10, 0.5, PackagePriority.Fragile),
            new PackageItem("Eggs", 20, 15, 8, 0.7, PackagePriority.Fragile),
            new PackageItem("Canned Goods", 15, 15, 12, 3, PackagePriority.Medium),
            new PackageItem("Chips", 30, 20, 25, 0.3, PackagePriority.Light)
        };

        // Act
        var result = optimizer.OptimizePacking(deliveryBox, packages);

        // Assert
        result.PackedItems.Should().Contain(p => p.Package.Name == "Bread");
        result.PackedItems.Should().Contain(p => p.Package.Name == "Eggs");
        result.Validate().IsValid.Should().BeTrue();

        // Nothing rests on the bread or the eggs - checked by footprint, not by height.
        foreach (var fragile in result.PackedItems.Where(p => p.Package.Priority == PackagePriority.Fragile))
        {
            result.PackedItems.Where(p => p.RestsOn(fragile))
                .Should().BeEmpty("'{0}' is fragile", fragile.Package.Name);
        }
    }

    [Fact]
    public void Scenario_MixedFleet_ShouldSpreadTheLoadAcrossContainers()
    {
        // Arrange - more freight than any single van can take.
        var optimizer = new MultiContainerOptimizer();
        var van = new Container(300, 180, 180, 1200, "Van");

        var packages = Enumerable.Range(1, 60)
            .Select(i => new PackageItem($"Crate {i}", 80, 60, 60, 20, PackagePriority.Heavy))
            .ToList();

        // Act
        var result = optimizer.PackAll(van, packages);

        // Assert
        result.IsFullyPacked.Should().BeTrue();
        result.PackedCount.Should().Be(packages.Count);
        result.ContainerCount.Should().BeGreaterThan(1, "the load does not fit in one van");

        foreach (var container in result.Containers)
        {
            container.Validate().IsValid.Should().BeTrue("{0}", container.Validate());
            container.TotalWeight.Should().BeLessOrEqualTo(van.MaxWeight);
        }

        output.WriteLine($"Vans used: {result.ContainerCount}");
        output.WriteLine($"Overall utilization: {result.OverallUtilization:P2}");
    }

    [Fact]
    public void Scenario_ContainerSelection_ShouldPickTheSmallestThatFits()
    {
        // Arrange
        var optimizer = new MultiContainerOptimizer();
        var catalogue = new List<Container>
        {
            new Container(60, 40, 40, 100, "Small"),
            new Container(120, 80, 80, 500, "Medium"),
            new Container(240, 160, 160, 2000, "Large")
        };

        var packages = Enumerable.Range(1, 8)
            .Select(i => new PackageItem($"Box {i}", 40, 40, 40, 5, PackagePriority.Heavy))
            .ToList();

        // Act
        var result = optimizer.SelectBestContainer(catalogue, packages);

        // Assert
        result.Should().NotBeNull();
        result!.Container.Name.Should().Be("Medium", "the small crate cannot hold eight 40cm cubes");
        result.IsFullyPacked.Should().BeTrue();
        result.Validate().IsValid.Should().BeTrue();
    }
}
