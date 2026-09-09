using SmartPackingSolution.Algorithms;
using SmartPackingSolution.Models;
using SmartPackingSolution.Reporting;
using SmartPackingSolution.Strategies;

var scenarios = Scenarios.All();
var selected = args.Length > 0
    ? scenarios.Where(s => s.Name.Contains(args[0], StringComparison.OrdinalIgnoreCase)).ToList()
    : scenarios;

if (selected.Count == 0)
{
    Console.WriteLine($"No scenario matches '{args[0]}'. Available: {string.Join(", ", scenarios.Select(s => s.Name))}");
    return 1;
}

Console.WriteLine("SmartPackingSolution - 3D bin packing");
Console.WriteLine();

foreach (var scenario in selected)
{
    RunScenario(scenario);
}

return 0;

static void RunScenario(Scenario scenario)
{
    var rule = new string('=', 78);
    Console.WriteLine(rule);
    Console.WriteLine(scenario.Name);
    Console.WriteLine(scenario.Description);
    Console.WriteLine(rule);
    Console.WriteLine();

    var offered = scenario.Packages.Sum(p => p.Volume) / scenario.Container.Volume;
    Console.WriteLine(
        $"Container : {scenario.Container.Dimensions.Length:F0} x {scenario.Container.Dimensions.Width:F0} " +
        $"x {scenario.Container.Dimensions.Height:F0} cm, max {scenario.Container.MaxWeight:F0} kg");
    Console.WriteLine($"Offered   : {scenario.Packages.Count} packages, {offered:P0} of container volume");
    Console.WriteLine();

    IPackingStrategy[] strategies =
    [
        new FirstFitDecreasingStrategy(),
        new BestFitDecreasingStrategy(),
        new LayerBasedStrategy(),
        new BestOfStrategy()
    ];

    Console.WriteLine($"{"Strategy",-30} {"Packed",7} {"Volume",8} {"Compact",8} {"Balance",8} {"Time",8} {"Valid",6}");
    Console.WriteLine(new string('-', 78));

    PackingResult? best = null;

    foreach (var strategy in strategies)
    {
        var result = new ContainerOptimizer(strategy).OptimizePacking(scenario.Container, scenario.Packages);
        var report = result.Validate();

        Console.WriteLine(
            $"{result.StrategyName,-30} " +
            $"{$"{result.PackedItems.Count}/{scenario.Packages.Count}",7} " +
            $"{result.SpaceUtilization,8:P1} " +
            $"{result.BoundingBoxUtilization,8:P1} " +
            $"{result.LoadBalanceScore,8:F3} " +
            $"{$"{result.Elapsed.TotalMilliseconds:F0} ms",8} " +
            $"{(report.IsValid ? "yes" : $"{report.Violations.Count} bad"),6}");

        // Same rule BestOfStrategy uses: packed volume first, load balance breaking ties.
        if (best is null ||
            result.SpaceUtilization > best.SpaceUtilization ||
            (result.SpaceUtilization == best.SpaceUtilization &&
             result.LoadBalanceScore > best.LoadBalanceScore))
        {
            best = result;
        }
    }

    Console.WriteLine();

    if (best is null)
    {
        return;
    }

    Console.WriteLine($"Best layout: {best.StrategyName}");
    Console.WriteLine(
        $"  weight {best.TotalWeight:F1} / {best.Container.MaxWeight:F0} kg, " +
        $"stack height {best.MaxStackHeight:F0} cm, " +
        $"centre of gravity ({best.CenterOfGravity.X:F0}, {best.CenterOfGravity.Y:F0}, {best.CenterOfGravity.Z:F0}) cm");
    Console.WriteLine();
    Console.WriteLine(AsciiLayoutRenderer.Render(best));
    Console.WriteLine();

    if (best.UnpackedItems.Count > 0)
    {
        Console.WriteLine("Left behind");
        foreach (var group in best.UnpackedItems.GroupBy(u => u.Reason).OrderByDescending(g => g.Count()))
        {
            Console.WriteLine($"  {group.Count(),3}  {group.Key}");
            foreach (var item in group.Take(3))
            {
                Console.WriteLine($"       {item.Package.Name}");
            }

            if (group.Count() > 3)
            {
                Console.WriteLine($"       ... and {group.Count() - 3} more");
            }
        }

        Console.WriteLine();
    }

    var validation = best.Validate();
    Console.WriteLine($"Validation: {validation}");
    Console.WriteLine();
}

/// <summary>A named packing problem the demo can run.</summary>
internal sealed record Scenario(string Name, string Description, Container Container, List<PackageItem> Packages);

internal static class Scenarios
{
    public static List<Scenario> All() => [MovingTruck(), Warehouse(), GroceryDelivery()];

    private static Scenario MovingTruck() => new(
        "moving-truck",
        "Household move: mixed furniture and fragile goods in a box truck.",
        new Container(600, 240, 240, 5000, "Box truck"),
        [
            new PackageItem("Office desk", 150, 80, 75, 50, PackagePriority.Heavy),
            new PackageItem("Wardrobe", 120, 60, 200, 65, PackagePriority.Heavy, keepUpright: true),
            new PackageItem("Dining chair", 60, 60, 90, 15),
            new PackageItem("TV box", 120, 80, 20, 12, PackagePriority.Fragile),
            new PackageItem("Books box", 40, 30, 30, 20),
            new PackageItem("Floor lamp", 30, 30, 60, 3, PackagePriority.Fragile),
            new PackageItem("Mattress", 200, 150, 30, 25),
            new PackageItem("Pillows", 60, 60, 40, 2, PackagePriority.Light),
            new PackageItem("Mirror", 100, 10, 160, 8, PackagePriority.Fragile, keepUpright: true),
            new PackageItem("Toolbox", 50, 30, 30, 18, PackagePriority.Heavy)
        ]);

    private static Scenario Warehouse()
    {
        var rng = new Random(4242);
        var packages = Enumerable.Range(1, 120)
            .Select(i => new PackageItem(
                $"Carton {i}",
                rng.Next(15, 46),
                rng.Next(15, 46),
                rng.Next(10, 41),
                rng.Next(1, 6),
                PackagePriority.Heavy))
            .ToList();

        return new Scenario(
            "warehouse",
            "Outbound pallet crate, deliberately overloaded so the packing is what is measured.",
            new Container(120, 100, 100, 100000, "Pallet crate"),
            packages);
    }

    private static Scenario GroceryDelivery() => new(
        "grocery",
        "Last-mile grocery crate where the fragile goods have to survive the trip.",
        new Container(60, 40, 40, 30, "Delivery crate"),
        [
            new PackageItem("Water bottles", 30, 20, 25, 6, PackagePriority.Heavy),
            new PackageItem("Bread", 25, 15, 10, 0.5, PackagePriority.Fragile),
            new PackageItem("Eggs", 20, 15, 8, 0.7, PackagePriority.Fragile, keepUpright: true),
            new PackageItem("Canned goods", 15, 15, 12, 3),
            new PackageItem("Crisps", 30, 20, 25, 0.3, PackagePriority.Light),
            new PackageItem("Cereal", 25, 10, 30, 0.6, PackagePriority.Light)
        ]);
}
