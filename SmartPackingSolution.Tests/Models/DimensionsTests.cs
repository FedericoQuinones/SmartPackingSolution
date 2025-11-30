namespace SmartPackingSolution.Tests.Models;

using FluentAssertions;
using SmartPackingSolution.Models;
using Xunit;

public class DimensionsTests
{
    [Fact]
    public void Constructor_WithValidDimensions_ShouldCreateInstance()
    {
        // Arrange & Act
        var dimensions = new Dimensions(100, 50, 30);

        // Assert
        dimensions.Length.Should().Be(100);
        dimensions.Width.Should().Be(50);
        dimensions.Height.Should().Be(30);
    }

    [Fact]
    public void Volume_ShouldCalculateCorrectly()
    {
        // Arrange
        var dimensions = new Dimensions(10, 5, 2);

        // Act
        var volume = dimensions.Volume;

        // Assert
        volume.Should().Be(100);
    }

    [Theory]
    [InlineData(10, 5, 2, true)]
    [InlineData(0, 5, 2, false)]
    [InlineData(10, 0, 2, false)]
    [InlineData(10, 5, 0, false)]
    [InlineData(-1, 5, 2, false)]
    public void IsValid_ShouldValidateCorrectly(double length, double width, double height, bool expected)
    {
        // Arrange
        var dimensions = new Dimensions(length, width, height);

        // Act
        var isValid = dimensions.IsValid();

        // Assert
        isValid.Should().Be(expected);
    }

    [Fact]
    public void Rotate_WithRotateXY_ShouldSwapLengthAndWidth()
    {
        // Arrange
        var dimensions = new Dimensions(100, 50, 30);

        // Act
        var rotated = dimensions.Rotate(RotationType.RotateXY);

        // Assert
        rotated.Length.Should().Be(50);
        rotated.Width.Should().Be(100);
        rotated.Height.Should().Be(30);
    }

    [Fact]
    public void Rotate_WithRotateXZ_ShouldSwapLengthAndHeight()
    {
        // Arrange
        var dimensions = new Dimensions(100, 50, 30);

        // Act
        var rotated = dimensions.Rotate(RotationType.RotateXZ);

        // Assert
        rotated.Length.Should().Be(30);
        rotated.Width.Should().Be(50);
        rotated.Height.Should().Be(100);
    }

    [Fact]
    public void Rotate_WithNone_ShouldReturnSameDimensions()
    {
        // Arrange
        var dimensions = new Dimensions(100, 50, 30);

        // Act
        var rotated = dimensions.Rotate(RotationType.None);

        // Assert
        rotated.Should().Be(dimensions);
    }
}