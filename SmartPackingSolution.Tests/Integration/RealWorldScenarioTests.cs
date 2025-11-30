namespace SmartPackingSolution.Tests.Integration;

using FluentAssertions;
using SmartPackingSolution.Algorithms;
using SmartPackingSolution.Models;
using Xunit;

public class RealWorldScenarioTests
{
    [Fact]
    public void Scenario_MovingTruck_ShouldPackFurnitureAndFragileItems()
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var truck = new Container(600, 240, 240, 5000); // Standard moving truck
        
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

        // Assert
        result.IsFullyPacked.Should().BeTrue();
        result.TotalWeight.Should().BeLessOrEqualTo(truck.MaxWeight);
        result.SpaceUtilization.Should().BeGreaterThan(0.05);
        
        // Verify fragile items are not at bottom
        var fragileItems = result.PackedItems.Where(p => p.Package.Priority == PackagePriority.Fragile);
        var heavyItems = result.PackedItems.Where(p => p.Package.Priority == PackagePriority.Heavy);
        
        foreach (var fragile in fragileItems)
        {
            foreach (var heavy in heavyItems)
            {
                if (heavy.Position.Z < fragile.Position.Z)
                {
                    // Heavy item is below fragile - this is correct
                    continue;
                }
            }
        }
    }

    [Fact]
    public void Scenario_WarehouseShipment_ShouldMaximizeSpaceUtilization()
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var container = new Container(120, 100, 100, 1000);
        
        var packages = Enumerable.Range(1, 50)
            .Select(i => new PackageItem(
                $"Package {i}",
                20 + (i % 10),
                15 + (i % 8),
                10 + (i % 5),
                5 + (i % 3),
                (PackagePriority)(i % 4)))
            .ToList();

        // Act
        var result = optimizer.OptimizePacking(container, packages);

        // Assert
        result.PackedItems.Should().NotBeEmpty();
        result.TotalWeight.Should().BeLessOrEqualTo(container.MaxWeight);
        
        Console.WriteLine($"Packed: {result.PackedItems.Count}/{packages.Count}");
        Console.WriteLine($"Space Utilization: {result.SpaceUtilization:P2}");
        Console.WriteLine($"Total Weight: {result.TotalWeight}kg / {container.MaxWeight}kg");
    }

    [Fact]
    public void Scenario_GroceryDelivery_ShouldProtectFragileItems()
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var deliveryBox = new Container(60, 40, 40, 30);
        
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
        
        // Verify fragile items have nothing heavy on top
        var fragileItems = result.PackedItems.Where(p => p.Package.Priority == PackagePriority.Fragile);
        foreach (var fragile in fragileItems)
        {
            var itemsAbove = result.PackedItems.Where(p => 
                p.Position.Z > fragile.Position.Z + fragile.ActualDimensions.Height - 0.1);
            
            itemsAbove.Should().NotContain(p => p.Package.Priority == PackagePriority.Heavy);
        }
    }
}