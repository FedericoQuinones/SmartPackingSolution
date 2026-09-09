using BenchmarkDotNet.Running;
using SmartPackingSolution.Benchmarks;
using SmartPackingSolution.Strategies;

// `--survey` prints packed volume and wall-clock time for every strategy and size in one
// pass. BenchmarkDotNet measures time precisely but discards the result, and the quality
// of a layout matters as much as the speed of producing it.
if (args.Contains("--survey"))
{
    var sizes = args.Where(a => int.TryParse(a, out _)).Select(int.Parse).ToArray();
    Survey.Run(sizes.Length > 0 ? sizes : [100, 1_000, 10_000]);
    return;
}

BenchmarkRunner.Run<PackingBenchmarks>(args: args);

internal static class Survey
{
    public static void Run(int[] sizes)
    {
        IPackingStrategy[] strategies =
        [
            new FirstFitDecreasingStrategy(),
            new BestFitDecreasingStrategy(),
            new LayerBasedStrategy(),
            new BestOfStrategy()
        ];

        Console.WriteLine($"{"packages",9} {"strategy",-22} {"packed",7} {"volume",8} {"time",10} {"valid",6}");
        Console.WriteLine(new string('-', 68));

        foreach (var count in sizes)
        {
            var (container, packages) = LoadGenerator.Create(count);

            foreach (var strategy in strategies)
            {
                var result = strategy.Pack(container, packages);
                var report = result.Validate();

                Console.WriteLine(
                    $"{count,9} {strategy.Name,-22} {result.PackedItems.Count,7} " +
                    $"{result.SpaceUtilization,8:P1} " +
                    $"{$"{result.Elapsed.TotalMilliseconds:N0} ms",10} " +
                    $"{(report.IsValid ? "yes" : $"{report.Violations.Count}"),6}");

                // Flushed per line so a long run can be watched, and so a run that has to
                // be cut short still leaves the sizes it did finish.
                Console.Out.Flush();
            }

            Console.WriteLine();
        }
    }
}
