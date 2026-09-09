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

    [Theory]
    // A rectangular box has exactly six axis-aligned orientations. The original enum
    // defined only four, so two of them - and any placement that needed one - were
    // unreachable.
    [InlineData(Orientation.LWH, 100, 50, 30)]
    [InlineData(Orientation.WLH, 50, 100, 30)]
    [InlineData(Orientation.LHW, 100, 30, 50)]
    [InlineData(Orientation.HLW, 30, 100, 50)]
    [InlineData(Orientation.WHL, 50, 30, 100)]
    [InlineData(Orientation.HWL, 30, 50, 100)]
    public void Rotate_ShouldPermuteAxes(Orientation orientation, double length, double width, double height)
    {
        // Arrange
        var dimensions = new Dimensions(100, 50, 30);

        // Act
        var rotated = dimensions.Rotate(orientation);

        // Assert
        rotated.Should().Be(new Dimensions(length, width, height));
    }

    [Fact]
    public void Rotate_ShouldCoverEveryPermutationExactlyOnce()
    {
        // Arrange
        var dimensions = new Dimensions(100, 50, 30);

        // Act
        var results = Orientations.All.Select(dimensions.Rotate).ToList();

        // Assert
        results.Should().HaveCount(6).And.OnlyHaveUniqueItems();
        results.Should().OnlyContain(r => r.Volume == dimensions.Volume);
    }

    [Fact]
    public void Rotate_WithLWH_ShouldReturnSameDimensions()
    {
        // Arrange
        var dimensions = new Dimensions(100, 50, 30);

        // Act
        var rotated = dimensions.Rotate(Orientation.LWH);

        // Assert
        rotated.Should().Be(dimensions);
    }

    [Theory]
    // The container is 100x100x100, so a 200x5x5 package fits in no orientation even
    // though only one of its dimensions exceeds the container's.
    [InlineData(200, 5, 5, false)]
    [InlineData(200, 200, 200, false)]
    [InlineData(150, 90, 90, false)]
    [InlineData(90, 150, 90, false)]
    [InlineData(99, 99, 99, true)]
    [InlineData(30, 100, 60, true)]
    public void FitsInsideInSomeOrientation_ShouldConsiderEveryOrientation(
        double length, double width, double height, bool expected)
    {
        // Arrange
        var container = new Dimensions(100, 100, 100);
        var package = new Dimensions(length, width, height);

        // Act
        var fits = package.FitsInsideInSomeOrientation(container);

        // Assert
        fits.Should().Be(expected);
    }
}
