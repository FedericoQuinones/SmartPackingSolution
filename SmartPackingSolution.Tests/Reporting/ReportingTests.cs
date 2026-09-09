namespace SmartPackingSolution.Tests.Reporting;

using System.Text.Json;
using FluentAssertions;
using SmartPackingSolution.Models;
using SmartPackingSolution.Reporting;
using SmartPackingSolution.Strategies;
using Xunit;

public class ReportingTests
{
    private static PackingResult Pack()
    {
        var container = new Container(100, 60, 60, 500, "Test crate");
        var packages = new List<PackageItem>
        {
            new PackageItem("Crate", 50, 50, 30, 20, PackagePriority.Heavy),
            new PackageItem("Carton", 40, 40, 20, 5),
            new PackageItem("Sack", 30, 30, 20, 2, PackagePriority.Light)
        };

        return new BestFitDecreasingStrategy().Pack(container, packages);
    }

    [Fact]
    public void Render_ShouldDrawBothViewsAndALegendEntryPerPackage()
    {
        var result = Pack();

        var rendered = AsciiLayoutRenderer.Render(result);

        rendered.Should().Contain("Plan view").And.Contain("Elevation").And.Contain("Legend");

        foreach (var item in result.PackedItems)
        {
            rendered.Should().Contain(item.Package.Name);
        }
    }

    [Fact]
    public void Render_WithNothingPacked_ShouldSaySo()
    {
        var container = new Container(10, 10, 10, 10);
        var empty = new PackingResult(container, [], []);

        AsciiLayoutRenderer.Render(empty).Should().Be("(nothing packed)");
    }

    [Fact]
    public void Render_ShouldPutTheContainerFloorAtTheBottomOfTheElevation()
    {
        var container = new Container(100, 100, 100, 500);
        var packages = new List<PackageItem>
        {
            new PackageItem("Floor slab", 100, 100, 20, 10, PackagePriority.Heavy, allowRotation: false)
        };

        var result = new BestFitDecreasingStrategy().Pack(container, packages);
        var lines = AsciiLayoutRenderer.Render(result).Split(Environment.NewLine);

        var elevation = lines
            .SkipWhile(l => !l.StartsWith("Elevation", StringComparison.Ordinal))
            .Where(l => l.StartsWith('|'))
            .ToList();

        elevation.Should().NotBeEmpty();
        elevation[^1].Should().Contain("A", "the slab sits on the container floor");
        elevation[0].Should().NotContain("A", "nothing is stacked above it");
    }

    [Fact]
    public void ToJson_ShouldRoundTripTheLayout()
    {
        var result = Pack();

        var json = PackingJsonExporter.ToJson(result);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("container").GetProperty("name").GetString().Should().Be("Test crate");
        root.GetProperty("packedItems").GetArrayLength().Should().Be(result.PackedItems.Count);
        root.GetProperty("metrics").GetProperty("spaceUtilization").GetDouble()
            .Should().BeApproximately(result.SpaceUtilization, 1e-9);

        var first = root.GetProperty("packedItems")[0];
        first.GetProperty("orientation").GetString().Should().NotBeNullOrEmpty(
            "orientations serialise by name, not as an opaque integer");
        first.GetProperty("id").GetString().Should().NotBeNullOrEmpty(
            "every key is camelCase, including the shorthand anonymous members");
        first.GetProperty("position").GetProperty("x").GetDouble()
            .Should().Be(result.PackedItems[0].Position.X);
    }

    [Fact]
    public void ToJson_ShouldRecordWhyPackagesWereRejected()
    {
        var container = new Container(50, 50, 50, 500);
        var packages = new List<PackageItem>
        {
            new PackageItem("Oversized", 200, 200, 200, 5)
        };

        var json = PackingJsonExporter.ToJson(new BestFitDecreasingStrategy().Pack(container, packages));

        json.Should().Contain("TooLargeForContainer");
    }
}
