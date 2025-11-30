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

## Testing Requirements
- All algorithms must have comprehensive unit tests
- Integration tests for complete packing scenarios
- Performance benchmarks for large datasets

## Pull Request Process
1. Ensure all tests pass
2. Update XML documentation
3. Follow existing code style
4. Include benchmark results for algorithm changes