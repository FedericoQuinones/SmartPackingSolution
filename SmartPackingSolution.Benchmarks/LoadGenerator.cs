namespace SmartPackingSolution.Benchmarks;

using SmartPackingSolution.Models;

/// <summary>
/// Builds reproducible packing problems of a given size.
/// </summary>
internal static class LoadGenerator
{
    /// <summary>
    /// Creates a container and a package list whose volume overflows it, so the measured
    /// utilization reflects the packing rather than a generously sized container.
    /// </summary>
    /// <param name="count">The number of packages to generate.</param>
    /// <param name="seed">The seed, so a run is reproducible.</param>
    /// <returns>The generated problem.</returns>
    public static (Container Container, List<PackageItem> Packages) Create(int count, int seed = 20260909)
    {
        var rng = new Random(seed);

        // A realistic mix. Drawing priorities uniformly would make a quarter of the load
        // fragile, and since a fragile package carries nothing at all, load bearing rather
        // than geometry would end up being what the benchmark measures.
        PackagePriority[] mix =
        [
            PackagePriority.Heavy, PackagePriority.Heavy, PackagePriority.Heavy,
            PackagePriority.Medium, PackagePriority.Medium,
            PackagePriority.Light,
            PackagePriority.Fragile
        ];

        var packages = Enumerable.Range(1, count)
            .Select(i => new PackageItem(
                $"Box {i}",
                rng.Next(15, 46),
                rng.Next(15, 46),
                rng.Next(10, 41),
                rng.Next(1, 6),
                mix[rng.Next(mix.Length)]))
            .ToList();

        // Size the container from the load so that every size packs a comparably
        // overloaded container. Sizing it from the package count instead lets the
        // container outgrow the load, and utilization then measures nothing at all.
        var target = packages.Sum(p => p.Volume) * 0.45;
        var edge = Math.Cbrt(target / 1.2);
        var container = new Container(edge * 1.2, edge, edge, 1_000_000, "Benchmark crate");

        return (container, packages);
    }
}
