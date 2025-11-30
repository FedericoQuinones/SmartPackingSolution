namespace SmartPackingSolution.Tests.Models;

using FluentAssertions;
using SmartPackingSolution.Models;
using Xunit;

public class PlacedPackageTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var package = new PackageItem("Box", 30, 20, 10, 5);
        var position = new Position(10, 20, 30);

        // Act
        var placed = new PlacedPackage(package, position, RotationType.None);

        // Assert
        placed.Package.Should().Be(package);
        placed.Position.Should().Be(position);
        placed.Rotation.Should().Be(RotationType.None);
        placed.ActualDimensions.Should().Be(package.Dimensions);
    }

    [Fact]
    public void Constructor_WithRotation_ShouldApplyRotationToDimensions()
    {
        // Arrange
        var package = new PackageItem("Box", 30, 20, 10, 5);
        var position = new Position(0, 0, 0);

        // Act
        var placed = new PlacedPackage(package, position, RotationType.RotateXY);

        // Assert
        placed.ActualDimensions.Length.Should().Be(20);
        placed.ActualDimensions.Width.Should().Be(30);
        placed.ActualDimensions.Height.Should().Be(10);
    }

    [Fact]
    public void OverlapsWith_WhenPackagesOverlap_ShouldReturnTrue()
    {
        // Arrange
        var package1 = new PackageItem("Box1", 30, 20, 10, 5);
        var package2 = new PackageItem("Box2", 30, 20, 10, 5);
        var placed1 = new PlacedPackage(package1, new Position(0, 0, 0));
        var placed2 = new PlacedPackage(package2, new Position(10, 10, 5));

        // Act
        var overlaps = placed1.OverlapsWith(placed2);

        // Assert
        overlaps.Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_WhenPackagesDoNotOverlap_ShouldReturnFalse()
    {
        // Arrange
        var package1 = new PackageItem("Box1", 30, 20, 10, 5);
        var package2 = new PackageItem("Box2", 30, 20, 10, 5);
        var placed1 = new PlacedPackage(package1, new Position(0, 0, 0));
        var placed2 = new PlacedPackage(package2, new Position(50, 50, 50));

        // Act
        var overlaps = placed1.OverlapsWith(placed2);

        // Assert
        overlaps.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullPackage_ShouldThrowException()
    {
        // Act
        var act = () => new PlacedPackage(null!, new Position(0, 0, 0));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullPosition_ShouldThrowException()
    {
        // Arrange
        var package = new PackageItem("Box", 30, 20, 10, 5);

        // Act
        var act = () => new PlacedPackage(package, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}