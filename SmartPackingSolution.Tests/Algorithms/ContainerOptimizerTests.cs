namespace SmartPackingSolution.Tests.Algorithms;

using FluentAssertions;
using SmartPackingSolution.Algorithms;
using SmartPackingSolution.Exceptions;
using SmartPackingSolution.Models;
using SmartPackingSolution.Strategies;
using Xunit;

public class ContainerOptimizerTests
{
    [Fact]
    public void OptimizePacking_WithValidInput_ShouldReturnResult()
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var container = new Container(100, 100, 100, 1000);
        var packages = new List<PackageItem>
        {
            new PackageItem("Box1", 30, 20, 10, 5, PackagePriority.Medium)
        };

        // Act
        var result = optimizer.OptimizePacking(container, packages);

        // Assert
        result.Should().NotBeNull();
        result.Container.Should().Be(container);
    }

    [Fact]
    public void OptimizePacking_WithNullContainer_ShouldThrowException()
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var packages = new List<PackageItem>
        {
            new PackageItem("Box", 30, 20, 10, 5)
        };

        // Act
        var act = () => optimizer.OptimizePacking(null!, packages);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OptimizePacking_WithNullPackages_ShouldThrowException()
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var container = new Container(100, 100, 100, 1000);

        // Act
        var act = () => optimizer.OptimizePacking(container, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OptimizePacking_WithEmptyPackages_ShouldReturnEmptyResult()
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var container = new Container(100, 100, 100, 1000);
        var packages = new List<PackageItem>();

        // Act
        var result = optimizer.OptimizePacking(container, packages);

        // Assert
        result.PackedItems.Should().BeEmpty();
        result.UnpackedItems.Should().BeEmpty();
    }

    [Fact]
    public void OptimizePacking_WithExcessiveWeight_ShouldReportUnpackedRatherThanThrow()
    {
        // Arrange - one package fits within the capacity, the other does not.
        var optimizer = new ContainerOptimizer();
        var container = new Container(100, 100, 100, 10);
        var packages = new List<PackageItem>
        {
            new PackageItem("Light Box", 30, 20, 10, 8),
            new PackageItem("Heavy Box", 30, 20, 10, 50)
        };

        // Act
        var result = optimizer.OptimizePacking(container, packages);

        // Assert - refusing to produce any layout because one package is too heavy
        // throws away a perfectly good answer for the rest of the load.
        result.PackedItems.Should().ContainSingle(p => p.Package.Name == "Light Box");
        result.UnpackedItems.Should().ContainSingle()
            .Which.Reason.Should().Be(UnpackedReason.WeightCapacityExceeded);
        result.TotalWeight.Should().BeLessOrEqualTo(container.MaxWeight);
    }

    [Fact]
    public void OptimizePacking_WithExcessiveWeightAndThrowEnabled_ShouldThrowException()
    {
        // Arrange
        var options = PackingOptions.Default with { ThrowOnInfeasibleInput = true };
        var optimizer = new ContainerOptimizer(options: options);
        var container = new Container(100, 100, 100, 10);
        var packages = new List<PackageItem>
        {
            new PackageItem("Heavy Box", 30, 20, 10, 50)
        };

        // Act
        var act = () => optimizer.OptimizePacking(container, packages);

        // Assert
        act.Should().Throw<PackingException>().WithMessage("*weight*");
    }

    [Theory]
    [InlineData(200, 200, 200)]
    // 200x5x5 exceeds the container on a single axis only. The original check required
    // all three dimensions to exceed it, so a package like this slipped through as packable.
    [InlineData(200, 5, 5)]
    public void OptimizePacking_WithOversizedPackage_ShouldReportItAsTooLarge(
        double length, double width, double height)
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var container = new Container(100, 100, 100, 1000);
        var packages = new List<PackageItem>
        {
            new PackageItem("Huge Box", length, width, height, 5)
        };

        // Act
        var result = optimizer.OptimizePacking(container, packages);

        // Assert
        result.PackedItems.Should().BeEmpty();
        result.UnpackedItems.Should().ContainSingle()
            .Which.Reason.Should().Be(UnpackedReason.TooLargeForContainer);
    }

    [Theory]
    [InlineData(200, 200, 200)]
    [InlineData(200, 5, 5)]
    public void OptimizePacking_WithOversizedPackageAndThrowEnabled_ShouldThrowException(
        double length, double width, double height)
    {
        // Arrange
        var options = PackingOptions.Default with { ThrowOnInfeasibleInput = true };
        var optimizer = new ContainerOptimizer(options: options);
        var container = new Container(100, 100, 100, 1000);
        var packages = new List<PackageItem>
        {
            new PackageItem("Huge Box", length, width, height, 5)
        };

        // Act
        var act = () => optimizer.OptimizePacking(container, packages);

        // Assert
        act.Should().Throw<PackingException>().WithMessage("*too large*");
    }

    [Fact]
    public void OptimizePacking_WithMultiplePackages_ShouldPackEfficiently()
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var container = new Container(100, 100, 100, 1000);
        var packages = new List<PackageItem>
        {
            new PackageItem("Box1", 30, 20, 10, 5, PackagePriority.Heavy),
            new PackageItem("Box2", 25, 15, 10, 3, PackagePriority.Medium),
            new PackageItem("Box3", 20, 10, 5, 1, PackagePriority.Light)
        };

        // Act
        var result = optimizer.OptimizePacking(container, packages);

        // Assert
        result.PackedItems.Should().HaveCountGreaterThan(0);
        result.TotalWeight.Should().BeLessOrEqualTo(container.MaxWeight);
        result.SpaceUtilization.Should().BeGreaterThan(0);
    }

    [Fact]
    public void OptimizePacking_ShouldRespectPriorityOrdering()
    {
        // Arrange
        var optimizer = new ContainerOptimizer();
        var container = new Container(100, 100, 50, 1000);
        var packages = new List<PackageItem>
        {
            new PackageItem("Fragile", 30, 20, 10, 2, PackagePriority.Fragile),
            new PackageItem("Heavy", 40, 30, 20, 50, PackagePriority.Heavy)
        };

        // Act
        var result = optimizer.OptimizePacking(container, packages);

        // Assert
        result.PackedItems.Should().HaveCount(2);
        var heavyItem = result.PackedItems.First(p => p.Package.Priority == PackagePriority.Heavy);
        var fragileItem = result.PackedItems.First(p => p.Package.Priority == PackagePriority.Fragile);
        
        // Heavy item should be at bottom (lower Z coordinate)
        heavyItem.Position.Z.Should().BeLessOrEqualTo(fragileItem.Position.Z);
    }
}