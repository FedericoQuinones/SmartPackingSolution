# Contributing to SmartPackingSolution

## Overview
SmartPackingSolution is a professional 3D bin packing optimization library designed for efficient container space utilization with priority-based constraints.

## Coding Standards

### Documentation
- **All public APIs must have XML documentation comments** including:
  - Summary of purpose
  - Parameter descriptions
  - Return value descriptions
  - Example usage where applicable
  - Exception documentation

### Naming Conventions
- Classes: PascalCase (e.g., `PackageItem`, `ContainerOptimizer`)
- Interfaces: PascalCase with 'I' prefix (e.g., `IPackingStrategy`)
- Methods: PascalCase (e.g., `OptimizeLayout`, `CalculateVolume`)
- Properties: PascalCase
- Private fields: camelCase with underscore prefix (e.g., `_maxWeight`)
- Constants: PascalCase

### Code Quality
- **Unit test coverage**: Minimum 80% coverage for all algorithms
- **Null safety**: Use nullable reference types appropriately
- **SOLID principles**: Follow dependency injection and single responsibility
- **Performance**: Document time complexity for algorithms
- **Immutability**: Prefer immutable objects where possible

### Project Structure

```
SmartPackingSolution/
  Models/        Container, PackageItem, Dimensions, Position, Orientation, results
  Geometry/      AxisAlignedBox, SpatialIndex, ExtremePointSet, SupportCalculator
  Constraints/   LoadBearingGraph
  Strategies/    PackingContext, PackingOptions, and the four strategies
  Algorithms/    ContainerOptimizer, MultiContainerOptimizer
  Validation/    PackingValidator
  Reporting/     AsciiLayoutRenderer, PackingJsonExporter
```

Physics belongs in `PackingContext` and the types under `Geometry/` and `Constraints/`,
never inside a strategy. A strategy decides only the order packages are offered in and
how a feasible placement is scored; anything a second strategy would have to copy is in
the wrong place.

## Testing Requirements
- All algorithms must have comprehensive unit tests
- Integration tests for complete packing scenarios
- Performance benchmarks for large datasets
- **Any change to placement must keep the invariant tests green.** They assert, over
  randomised loads and for every strategy, that a layout passes `PackingValidator`:
  nothing out of bounds, overlapping, floating, or overloaded. A change that improves
  packed volume by relaxing one of those is not an improvement.
- Quote measured numbers, not estimates. `dotnet run -c Release --project
  SmartPackingSolution.Benchmarks -- --survey` reports packed volume and validity
  alongside the clock.

## Pull Request Process
1. Ensure all tests pass
2. Update XML documentation
3. Follow existing code style
4. Include benchmark results for algorithm changes