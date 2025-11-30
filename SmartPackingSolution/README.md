# SmartPackingSolution

A high-performance 3D bin packing optimization library for .NET 10+ that intelligently arranges packages of varying sizes and priorities within containers to maximize space utilization.

## Features

- **3D Bin Packing Algorithm**: Efficiently pack items with different dimensions (length, width, height)
- **Priority-Based Constraints**: Respect weight distribution rules (e.g., heavy items on bottom)
- **Multiple Packing Strategies**: Choose from various algorithms (First-Fit, Best-Fit, Layer-based)
- **Weight Management**: Automatic validation of stacking constraints based on item fragility
- **Rotation Support**: Optional rotation of packages to find optimal placement
- **Volume Optimization**: Maximize container space utilization
- **Thread-Safe**: Designed for concurrent packing operations

## Installation

dotnet add package SmartPackingSolution
Or add the reference manually in your `.csproj`:
<ItemGroup> <PackageReference Include="SmartPackingSolution" Version="1.0.0" /> </ItemGroup>

## Quick Start
using SmartPackingSolution; using SmartPackingSolution.Models; using SmartPackingSolution.Algorithms;
// Define container dimensions var container = new Container(120, 100, 100, maxWeight: 1000);
// Create packages with dimensions, weight, and priority var packages = new List<PackageItem> { new PackageItem("Desk", 80, 60, 75, weight: 50, priority: PackagePriority.Heavy), new PackageItem("Water Bottle", 10, 10, 25, weight: 1, priority: PackagePriority.Fragile), new PackageItem("Books", 30, 20, 15, weight: 10, priority: PackagePriority.Medium) };
// Optimize packing var optimizer = new ContainerOptimizer(); var result = optimizer.OptimizePacking(container, packages);
// Check results Console.WriteLine($"Packed: {result.PackedItems.Count}/{packages.Count}"); Console.WriteLine($"Space Utilization: {result.SpaceUtilization:P2}"); Console.WriteLine($"Total Weight: {result.TotalWeight}kg");

## Testing Requirements
- All algorithms must have comprehensive unit tests
- Integration tests for complete packing scenarios
- Performance benchmarks for large datasets

## Pull Request Process
1. Ensure all tests pass
2. Update XML documentation
3. Follow existing code style
4. Include benchmark results for algorithm changes

## Package Priority System

- **Heavy**: Must be placed at the bottom (e.g., furniture, appliances)
- **Medium**: Standard items with no special constraints
- **Fragile**: Cannot support weight above (e.g., glass, electronics)
- **Light**: Can be placed on top of most items

## Algorithms

### Layer-Based Packing
Organizes items in horizontal layers, optimal for uniform box sizes.

### First-Fit Decreasing
Sorts items by volume and places them in the first available position.

### Best-Fit
Finds the position that minimizes wasted space for each item.

## Performance

- **Time Complexity**: O(n² log n) for most scenarios
- **Memory**: O(n) where n is the number of packages
- **Benchmarks**: Can process 1000+ items in under 100ms

## License

MIT License - See LICENSE file for details

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for coding standards and guidelines
