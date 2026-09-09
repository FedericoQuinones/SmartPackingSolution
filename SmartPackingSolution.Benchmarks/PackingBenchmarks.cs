namespace SmartPackingSolution.Benchmarks;

using BenchmarkDotNet.Attributes;
using SmartPackingSolution.Models;
using SmartPackingSolution.Strategies;

/// <summary>
/// Measures how each strategy scales, so the README can quote numbers rather than
/// assert them.
/// </summary>
[MemoryDiagnoser]
public class PackingBenchmarks
{
    private Container _container = null!;
    private List<PackageItem> _packages = null!;
    private IPackingStrategy _strategy = null!;

    /// <summary>Gets or sets the number of packages offered.</summary>
    [Params(100, 1_000, 10_000)]
    public int PackageCount { get; set; }

    /// <summary>Gets or sets the strategy under test.</summary>
    [Params("first-fit", "best-fit", "layer")]
    public string Strategy { get; set; } = "best-fit";

    /// <summary>Prepares the problem and strategy for the current parameters.</summary>
    [GlobalSetup]
    public void Setup()
    {
        (_container, _packages) = LoadGenerator.Create(PackageCount);
        _strategy = Strategy switch
        {
            "first-fit" => new FirstFitDecreasingStrategy(),
            "layer" => new LayerBasedStrategy(),
            _ => new BestFitDecreasingStrategy()
        };
    }

    /// <summary>Packs the generated load.</summary>
    /// <returns>The number of packages placed, to keep the call from being elided.</returns>
    [Benchmark]
    public int Pack() => _strategy.Pack(_container, _packages).PackedItems.Count;
}
