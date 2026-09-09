# SmartPackingSolution

A 3D bin packing library for .NET that produces container layouts you could actually load.

Most packing samples optimise a number. This one optimises a number *subject to physics*:
nothing floats in mid-air, nothing is crushed by what sits on it, and every layout can be
handed to an independent validator that re-derives support and load transmission from the
placements alone.

```csharp
var container = new Container(120, 100, 100, maxWeight: 1000);
var packages = new List<PackageItem>
{
    new PackageItem("Desk",         80, 60, 75, weight: 50, PackagePriority.Heavy),
    new PackageItem("Monitor box",  60, 50, 40, weight:  8, PackagePriority.Fragile),
    new PackageItem("Books",        30, 20, 15, weight: 10, PackagePriority.Medium)
};

var result = new ContainerOptimizer().OptimizePacking(container, packages);

Console.WriteLine($"Packed {result.PackedItems.Count}/{packages.Count}");
Console.WriteLine($"Volume used: {result.SpaceUtilization:P1}");
Console.WriteLine(result.Validate());   // "Layout is physically valid."
```

## Install

```bash
dotnet add package SmartPackingSolution
```

Targets .NET 10. The source has no .NET 10-only dependencies, so it also builds against
.NET 9 with `-p:SpsTargetFramework=net9.0`.

## The physical model

Three constraints are enforced while packing, not checked afterwards.

**Support.** A package must rest on the container floor or on the top faces of other
packages. By default at least 75% of its base must be carried; the supported area is
computed as an exact rectangle union, so two supporters meeting edge to edge count once
rather than twice.

**Load bearing.** Each package declares how much weight it can carry
(`MaxSupportedWeight`, defaulted from its priority). Load travels down the support chain
and is split between supporters in proportion to the contact area each provides, so a tall
stack is checked against every package under it, not just the one directly below.

Fragility is not a separate rule: a `Fragile` package simply has a capacity of zero
kilograms, so nothing may rest on it — but only when something is genuinely above it in
XY, not merely at a greater height somewhere else in the container.

The check runs in both directions. Packing does not proceed strictly upwards: a package
can drop into a gap beneath something already placed and start carrying a share of the
load above it, so a placement is rejected if it could not bear what it ends up under.

**Orientation.** A rectangular box has six axis-aligned orientations, and all six are
searched. Set `allowRotation: false` to pin a package, or `keepUpright: true` for
"this side up" goods, which restricts it to the two orientations that keep its own height
vertical.

| Priority | Carries by default | Typical goods |
|---|---|---|
| `Fragile` | 0 kg | glass, electronics, produce |
| `Light` | 5 kg | pillows, clothing, foam |
| `Medium` | 30 kg | cartons, books, tools |
| `Heavy` | 200 kg | furniture, appliances, crates |

Override per package with the `maxSupportedWeight` constructor argument.

## Strategies

| Strategy | How it chooses | Best for |
|---|---|---|
| `BestFitDecreasingStrategy` | scores every anchor and orientation, preferring low placements that press against walls and neighbours | the default; mixed loads |
| `FirstFitDecreasingStrategy` | takes the first legal position, lowest first | throughput over density |
| `LayerBasedStrategy` | fills horizontal decks, each sized by its tallest package | uniform cartons |
| `BestOfStrategy` | runs the others across several orderings in parallel, keeps the best | when the load shape is unknown |

No single heuristic wins everywhere — on the demo's warehouse crate the layer packer
reaches 85.6% where first-fit manages 82.0%, and on mixed furniture the ranking reverses.
`BestOfStrategy` exists because the runs are independent and packages are immutable, so
trying them all costs about as long as the slowest one:

```csharp
var optimizer = new ContainerOptimizer(new BestOfStrategy());
```

Candidate layouts are compared by packed volume, with load balance breaking ties: between
two layouts holding the same goods, the one whose centre of gravity sits lower and more
central is the one that survives the journey.

## Options

```csharp
var options = PackingOptions.Default with
{
    MinimumSupportRatio = 0.9,                  // stricter than the 0.75 default
    Ordering            = ItemOrdering.HeaviestFirst,
    Score               = PlacementScore.MaxContactSurface,
    TimeBudget          = TimeSpan.FromSeconds(2),
    CancellationToken   = token
};

var result = new ContainerOptimizer(new BestFitDecreasingStrategy(), options)
    .OptimizePacking(container, packages);
```

`RequireSupport` and `EnforceLoadBearing` can be turned off to compare against packers
that ignore physics. Turning them off produces layouts that cannot be loaded.

## What comes back

Packing never throws because a package does not fit. Every package is either placed or
returned with the reason it was not:

```csharp
foreach (var rejected in result.UnpackedItems)
{
    Console.WriteLine($"{rejected.Package.Name}: {rejected.Reason} — {rejected.Details}");
}
```

`TooLargeForContainer`, `WeightCapacityExceeded`, `NoSpaceAvailable`,
`LoadBearingConstraint`, `SupportConstraint`, `TimeBudgetExceeded`. Set
`PackingOptions.ThrowOnInfeasibleInput` if you would rather fail fast than receive a
partial layout.

Alongside the placements, a result carries:

- `SpaceUtilization` — packed volume over container volume.
- `BoundingBoxUtilization` — packed volume over the bounding box of what was packed, which
  an oversized container cannot flatter.
- `CenterOfGravity` and `LoadBalanceScore` — how centred and how low the load sits.
- `MaxStackHeight`, `TotalWeight`, `Elapsed`, `StrategyName`.

## Validation

`result.Validate()` re-derives everything from the placements, sharing no state with the
packer that produced them, and reports out-of-bounds packages, overlaps, floating
packages, overloaded packages, anything stacked on a fragile item, and container overweight.

```csharp
var report = result.Validate();
if (!report.IsValid)
{
    Console.WriteLine(report);   // one line per violation
}
```

That independence is the point: it makes "the packer respects support and load bearing" a
property you can test on any layout, including one produced elsewhere. The test suite
asserts it over randomised loads for every strategy.

## Several containers

```csharp
var optimizer = new MultiContainerOptimizer();

// Open as many identical containers as the load needs.
MultiPackingResult fleet = optimizer.PackAll(vanTemplate, packages);

// Or fill a specific set in order.
MultiPackingResult mixed = optimizer.PackAll([truck, van, car], packages);

// Or find the smallest container in a catalogue that holds everything.
PackingResult? chosen = optimizer.SelectBestContainer(catalogue, packages);
```

## Seeing the layout

```csharp
Console.WriteLine(AsciiLayoutRenderer.Render(result));  // plan view + elevation + legend
File.WriteAllText("layout.json", PackingJsonExporter.ToJson(result));
```

```
Elevation - looking along the width (60 x 40 cm)
+----------------------------------------------------------------+
|................................CCCCCCCCCCCCCCCCCCCCCCCCCCC.....|
|AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACCCCCCCCCCCCCCCCCCCCCCCCCCC.....|
|AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACCCCCCCCCCCCCCCCCCCCCCCCCCC.....|
+----------------------------------------------------------------+
```

## Performance

Measured on this repository's benchmark loads — packages of 15–45 x 15–45 x 10–40 cm in
a container deliberately sized to about 45% of their combined volume, so the number
reflects the packing and not a generous container. Reproduce with
`dotnet run -c Release --project SmartPackingSolution.Benchmarks -- --survey`.

| Packages | Strategy | Packed volume | Time |
|---:|---|---:|---:|
| 100 | Best-Fit Decreasing | 77.1% | 34 ms |
| 100 | Layer-Based | 81.6% | 69 ms |
| 100 | Best-Of | 81.6% | 176 ms |
| 1 000 | Best-Fit Decreasing | 60.4% | 0.9 s |
| 1 000 | Best-Of | 60.4% | 2.8 s |
| 10 000 | Best-Fit Decreasing | 27.7% | 43 s |
| 10 000 | Best-Of | 27.7% | 104 s |

Every layout above passes the validator. Times are from one machine on .NET 10 and will
move with the hardware; the packed volumes are deterministic and will not.

Two things are worth reading off that table rather than glossing over.

**Density falls as the load grows.** This is the support requirement, not the search
budget: raising `MaxCandidatePositions` from 4 000 to 64 000 changes the result by
nothing at all, while relaxing `MinimumSupportRatio` from 0.75 to 0.25 recovers about
nine points at a thousand packages and under three at a hundred. As a container fills, its
upper surface gets ragged, and insisting that three quarters of every base be carried
rules out most of the perches on that surface. That is the cost of a layout you can
actually load, and it is adjustable:

```csharp
PackingOptions.Default with { MinimumSupportRatio = 0.5 }
```

**Ten thousand packages is past the comfortable range.** The search reads the anchor set
once per package and most packages in an overloaded container are rejected, so the cost
grows with the product of the two. Up to a couple of thousand packages that is under a
few seconds; beyond it, set a `TimeBudget` and take the partial layout, or split the load
across containers with `MultiContainerOptimizer`.

## Building it

```bash
dotnet build
dotnet test
dotnet run --project SmartPackingSolution.Demo              # all scenarios
dotnet run --project SmartPackingSolution.Demo -- grocery   # one of them
dotnet run -c Release --project SmartPackingSolution.Benchmarks -- --survey
dotnet run -c Release --project SmartPackingSolution.Benchmarks             # BenchmarkDotNet
```

## License

MIT — see [LICENSE](LICENSE).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).
