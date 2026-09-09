namespace SmartPackingSolution.Tests.Validation;

using FluentAssertions;
using SmartPackingSolution.Models;
using SmartPackingSolution.Strategies;
using Xunit;

/// <summary>
/// Asserts the properties every layout must have, over randomised loads.
/// </summary>
/// <remarks>
/// These are the tests that make the engine's claims checkable rather than asserted. The
/// validator re-derives support and load transmission from the placements alone, sharing
/// no state with the packer, so a layout passing here is genuinely loadable rather than
/// merely self-consistent with the code that produced it.
/// </remarks>
public class PhysicalInvariantTests
{
    public static TheoryData<string, int> StrategiesAndSeeds()
    {
        var data = new TheoryData<string, int>();
        foreach (var name in new[] { "first-fit", "best-fit", "layer", "best-of" })
        {
            foreach (var seed in new[] { 1, 17, 101, 2718, 31337 })
            {
                data.Add(name, seed);
            }
        }

        return data;
    }

    private static IPackingStrategy Create(string name) => name switch
    {
        "first-fit" => new FirstFitDecreasingStrategy(),
        "layer" => new LayerBasedStrategy(),
        "best-of" => new BestOfStrategy(),
        _ => new BestFitDecreasingStrategy()
    };

    private static (Container Container, List<PackageItem> Packages) RandomLoad(int seed)
    {
        var rng = new Random(seed);
        var container = new Container(
            rng.Next(100, 301),
            rng.Next(80, 201),
            rng.Next(80, 201),
            rng.Next(500, 5000));

        var packages = Enumerable.Range(1, rng.Next(30, 81))
            .Select(i => new PackageItem(
                $"Box {i}",
                rng.Next(10, 61),
                rng.Next(10, 61),
                rng.Next(10, 61),
                Math.Round(rng.NextDouble() * 20 + 0.5, 2),
                (PackagePriority)rng.Next(0, 4),
                allowRotation: rng.Next(0, 4) > 0,
                keepUpright: rng.Next(0, 5) == 0))
            .ToList();

        return (container, packages);
    }

    [Theory]
    [MemberData(nameof(StrategiesAndSeeds))]
    public void EveryLayout_ShouldBePhysicallyValid(string strategyName, int seed)
    {
        // Arrange
        var (container, packages) = RandomLoad(seed);

        // Act
        var result = Create(strategyName).Pack(container, packages);
        var report = result.Validate();

        // Assert
        report.IsValid.Should().BeTrue(
            "layout from {0} on seed {1} must be loadable:{2}{3}",
            strategyName, seed, Environment.NewLine, report);
    }

    [Theory]
    [MemberData(nameof(StrategiesAndSeeds))]
    public void EveryPackage_ShouldBeEitherPackedOrExplained(string strategyName, int seed)
    {
        // Arrange
        var (container, packages) = RandomLoad(seed);

        // Act
        var result = Create(strategyName).Pack(container, packages);

        // Assert - a package must not simply vanish between input and output.
        var accountedFor = result.PackedItems.Select(p => p.Package.Id)
            .Concat(result.UnpackedItems.Select(u => u.Package.Id))
            .ToList();

        accountedFor.Should().BeEquivalentTo(packages.Select(p => p.Id));
        accountedFor.Should().OnlyHaveUniqueItems("a package cannot be both packed and rejected");
    }

    [Theory]
    [MemberData(nameof(StrategiesAndSeeds))]
    public void PackedWeight_ShouldNeverExceedContainerCapacity(string strategyName, int seed)
    {
        // Arrange
        var (container, packages) = RandomLoad(seed);

        // Act
        var result = Create(strategyName).Pack(container, packages);

        // Assert
        result.TotalWeight.Should().BeLessOrEqualTo(container.MaxWeight);
    }

    [Theory]
    [MemberData(nameof(StrategiesAndSeeds))]
    public void UprightPackages_ShouldKeepTheirOwnHeightVertical(string strategyName, int seed)
    {
        // Arrange
        var (container, packages) = RandomLoad(seed);

        // Act
        var result = Create(strategyName).Pack(container, packages);

        // Assert
        foreach (var placed in result.PackedItems.Where(p => p.Package.KeepUpright))
        {
            placed.ActualDimensions.Height.Should().Be(
                placed.Package.Dimensions.Height,
                "'{0}' is marked this-side-up", placed.Package.Name);
        }

        foreach (var placed in result.PackedItems.Where(p => !p.Package.AllowRotation))
        {
            placed.Orientation.Should().Be(Orientation.LWH,
                "'{0}' may not be rotated", placed.Package.Name);
        }
    }

    [Fact]
    public void Packing_ShouldBeDeterministic()
    {
        // Arrange
        var (container, packages) = RandomLoad(99);
        var strategy = new BestFitDecreasingStrategy();

        // Act
        var first = strategy.Pack(container, packages);
        var second = strategy.Pack(container, packages);

        // Assert - the same input must give the same layout, or nothing above this is
        // reproducible and a benchmark number means little.
        second.PackedItems.Select(p => (p.Package.Id, p.Position, p.Orientation))
            .Should().Equal(first.PackedItems.Select(p => (p.Package.Id, p.Position, p.Orientation)));
    }

    [Fact]
    public void TimeBudget_ShouldStopPackingAndExplainWhy()
    {
        // Arrange - a budget of zero cannot survive the first check.
        var (container, packages) = RandomLoad(5);
        var options = PackingOptions.Default with { TimeBudget = TimeSpan.Zero };

        // Act
        var result = new BestFitDecreasingStrategy().Pack(container, packages, options);

        // Assert
        result.UnpackedItems.Should().Contain(u => u.Reason == UnpackedReason.TimeBudgetExceeded);
        result.Validate().IsValid.Should().BeTrue("an interrupted run must still leave a valid layout");
    }
}
