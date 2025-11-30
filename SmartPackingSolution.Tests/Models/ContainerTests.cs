namespace SmartPackingSolution.Tests.Models;

using FluentAssertions;
using SmartPackingSolution.Models;
using Xunit;

public class ContainerTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var container = new Container(120, 100, 100, 1000);

        // Assert
        container.Dimensions.Length.Should().Be(120);
        container.Dimensions.Width.Should().Be(100);
        container.Dimensions.Height.Should().Be(100);
        container.MaxWeight.Should().Be(1000);
    }

    [Theory]
    [InlineData(0, 100, 100)]
    [InlineData(120, 0, 100)]
    [InlineData(120, 100, 0)]
    [InlineData(-1, 100, 100)]
    public void Constructor_WithInvalidDimensions_ShouldThrowException(double length, double width, double height)
    {
        // Act
        var act = () => new Container(length, width, height, 1000);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*dimensions*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidMaxWeight_ShouldThrowException(double maxWeight)
    {
        // Act
        var act = () => new Container(120, 100, 100, maxWeight);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*weight*");
    }

    [Fact]
    public void Volume_ShouldCalculateCorrectly()
    {
        // Arrange
        var container = new Container(10, 10, 10, 1000);

        // Act
        var volume = container.Volume;

        // Assert
        volume.Should().Be(1000);
    }
}