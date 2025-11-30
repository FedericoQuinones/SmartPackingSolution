using SmartPackingSolution.Algorithms;
using SmartPackingSolution.Models;

Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
Console.WriteLine("║     SmartPackingSolution - 3D Bin Packing Demo      ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
Console.WriteLine();

// Define container
var container = new Container(120, 100, 100, maxWeight: 500);
Console.WriteLine($"📦 Container: {container.Dimensions.Length}x{container.Dimensions.Width}x{container.Dimensions.Height} cm");
Console.WriteLine($"⚖️  Max Weight: {container.MaxWeight}kg");
Console.WriteLine($"📊 Total Volume: {container.Volume:N0} cm³");
Console.WriteLine();

// Create packages
var packages = new List<PackageItem>
{
    new PackageItem("Office Desk", 150, 80, 75, weight: 50, priority: PackagePriority.Heavy),
    new PackageItem("Monitor Box", 60, 50, 40, weight: 8, priority: PackagePriority.Fragile),
    new PackageItem("Books Box", 40, 30, 30, weight: 15, priority: PackagePriority.Medium),
    new PackageItem("Keyboard", 45, 15, 5, weight: 1.5, priority: PackagePriority.Light),
    new PackageItem("Chair", 60, 60, 90, weight: 12, priority: PackagePriority.Medium),
    new PackageItem("Lamp", 25, 25, 50, weight: 3, priority: PackagePriority.Fragile),
    new PackageItem("Water Bottles", 30, 20, 25, weight: 6, priority: PackagePriority.Heavy),
    new PackageItem("Cushions", 50, 50, 30, weight: 2, priority: PackagePriority.Light)
};

Console.WriteLine($"📋 Packages to pack: {packages.Count}");
Console.WriteLine();

foreach (var package in packages)
{
    var priorityIcon = package.Priority switch
    {
        PackagePriority.Heavy => "🏋️",
        PackagePriority.Fragile => "🔴",
        PackagePriority.Light => "🪶",
        _ => "📦"
    };
    
    Console.WriteLine($"  {priorityIcon} {package.Name,-20} {package.Dimensions.Length}x{package.Dimensions.Width}x{package.Dimensions.Height} cm, {package.Weight}kg");
}

Console.WriteLine();
Console.WriteLine("⏳ Optimizing packing...");
Console.WriteLine();

// Optimize packing
var optimizer = new ContainerOptimizer();
var result = optimizer.OptimizePacking(container, packages);

// Display results
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("                    RESULTS                            ");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine();

Console.WriteLine($"✅ Packed Items: {result.PackedItems.Count}/{packages.Count}");
Console.WriteLine($"❌ Unpacked Items: {result.UnpackedItems.Count}");
Console.WriteLine($"📊 Space Utilization: {result.SpaceUtilization:P2}");
Console.WriteLine($"⚖️  Total Weight: {result.TotalWeight:F1}kg / {container.MaxWeight}kg ({(result.TotalWeight / container.MaxWeight):P1})");
Console.WriteLine($"🎯 Status: {(result.IsFullyPacked ? "✓ All items packed!" : "⚠ Some items couldn't fit")}");
Console.WriteLine();

if (result.PackedItems.Any())
{
    Console.WriteLine("📍 Packed Items Layout:");
    Console.WriteLine();
    
    foreach (var placed in result.PackedItems.OrderBy(p => p.Position.Z).ThenBy(p => p.Position.X))
    {
        var rotationInfo = placed.Rotation != RotationType.None 
            ? $" (Rotated: {placed.Rotation})" 
            : "";
            
        Console.WriteLine($"  • {placed.Package.Name,-20}");
        Console.WriteLine($"    Position: ({placed.Position.X:F0}, {placed.Position.Y:F0}, {placed.Position.Z:F0})");
        Console.WriteLine($"    Dimensions: {placed.ActualDimensions.Length:F0}x{placed.ActualDimensions.Width:F0}x{placed.ActualDimensions.Height:F0}{rotationInfo}");
        Console.WriteLine($"    Priority: {placed.Package.Priority}");
        Console.WriteLine();
    }
}

if (result.UnpackedItems.Any())
{
    Console.WriteLine("⚠️  Items that couldn't be packed:");
    Console.WriteLine();
    
    foreach (var unpacked in result.UnpackedItems)
    {
        Console.WriteLine($"  • {unpacked.Name} - {unpacked.Dimensions.Length}x{unpacked.Dimensions.Width}x{unpacked.Dimensions.Height} cm, {unpacked.Weight}kg");
    }
    Console.WriteLine();
}

Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();