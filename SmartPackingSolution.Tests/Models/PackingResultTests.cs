namespace SmartPackingSolution.Tests.Models;

using FluentAssertions;
using SmartPackingSolution.Models;
using Xunit;

public class PackingResultTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var container = new Container(100, 100, 100, 1000);
        var package = new PackageItem("Box", 30, 20, 10, 5);
        var placed = new PlacedPackage(package, new Position(0, 0, 0));
        var packedItems = new List<PlacedPackage> { placed };
        var unpackedItems = new List<PackageItem>();

        // Act
        var result = new PackingResult(container, packedItems, unpackedItems);

        // Assert
        result.Container.Should().Be(container);
        result.PackedItems.Should().HaveCount(1);
        result.UnpackedItems.Should().BeEmpty();
        result.TotalWeight.Should().Be(5);
        result.IsFullyPacked.Should().BeTrue();
    }

    [Fact]
    public void SpaceUtilization_ShouldCalculateCorrectly()
    {
        // Arrange
        var container = new Container(100, 100, 100, 1000); // Volume = 1,000,000
        var package = new PackageItem("Box", 50, 50, 50, 5); // Volume = 125,000
        var placed = new PlacedPackage(package, new Position(0, 0, 0));
        var packedItems = new List<PlacedPackage> { placed };

        // Act
        var result = new PackingResult(container, packedItems, new List<PackageItem>());

        // Assert
        result.SpaceUtilization.Should().BeApproximately(0.125, 0.001);
    }

    [Fact]
    public void IsFullyPacked_WithUnpackedItems_ShouldReturnFalse()
    {
        // Arrange
        var container = new Container(100, 100, 100, 1000);
        var package1 = new PackageItem("Box1", 30, 20, 10, 5);
        var package2 = new PackageItem("Box2", 200, 200, 200, 5);
        var placed = new PlacedPackage(package1, new Position(0, 0, 0));

        // Act
        var result = new PackingResult(
            container,
            new List<PlacedPackage> { placed },
            new List<PackageItem> { package2 });

        // Assert
        result.IsFullyPacked.Should().BeFalse();
    }

    [Fact]
    public void TotalWeight_ShouldSumAllPackedItems()
    {
        // Arrange
        var container = new Container(100, 100, 100, 1000);
        var package1 = new PackageItem("Box1", 30, 20, 10, 5);
        var package2 = new PackageItem("Box2", 30, 20, 10, 10);
        var placed1 = new PlacedPackage(package1, new Position(0, 0, 0));
        var placed2 = new PlacedPackage(package2, new Position(40, 0, 0));

        // Act
        var result = new PackingResult(
            container,
            new List<PlacedPackage> { placed1, placed2 },
            new List<PackageItem>());

        // Assert
        result.TotalWeight.Should().Be(15);
    }
}