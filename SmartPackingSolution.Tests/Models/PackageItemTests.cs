namespace SmartPackingSolution.Tests.Models;

using FluentAssertions;
using SmartPackingSolution.Models;
using Xunit;

public class PackageItemTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var package = new PackageItem("Box", 100, 50, 30, 10, PackagePriority.Medium);

        // Assert
        package.Name.Should().Be("Box");
        package.Dimensions.Length.Should().Be(100);
        package.Dimensions.Width.Should().Be(50);
        package.Dimensions.Height.Should().Be(30);
        package.Weight.Should().Be(10);
        package.Priority.Should().Be(PackagePriority.Medium);
        package.AllowRotation.Should().BeTrue();
        package.Id.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ShouldThrowException(string name)
    {
        // Act
        var act = () => new PackageItem(name, 100, 50, 30, 10);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidWeight_ShouldThrowException(double weight)
    {
        // Act
        var act = () => new PackageItem("Box", 100, 50, 30, weight);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Weight*");
    }

    [Theory]
    [InlineData(0, 50, 30)]
    [InlineData(100, 0, 30)]
    [InlineData(100, 50, 0)]
    [InlineData(-1, 50, 30)]
    public void Constructor_WithInvalidDimensions_ShouldThrowException(double length, double width, double height)
    {
        // Act
        var act = () => new PackageItem("Box", length, width, height, 10);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*dimensions*");
    }

    [Fact]
    public void Volume_ShouldCalculateCorrectly()
    {
        // Arrange
        var package = new PackageItem("Box", 10, 5, 2, 10);

        // Act
        var volume = package.Volume;

        // Assert
        volume.Should().Be(100);
    }

    [Fact]
    public void Constructor_WithoutRotationParameter_ShouldDefaultToTrue()
    {
        // Act
        var package = new PackageItem("Box", 100, 50, 30, 10);

        // Assert
        package.AllowRotation.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithRotationDisabled_ShouldSetToFalse()
    {
        // Act
        var package = new PackageItem("Box", 100, 50, 30, 10, allowRotation: false);

        // Assert
        package.AllowRotation.Should().BeFalse();
    }
}