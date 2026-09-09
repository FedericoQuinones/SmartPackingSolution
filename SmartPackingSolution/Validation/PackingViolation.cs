namespace SmartPackingSolution.Validation;

using SmartPackingSolution.Models;

/// <summary>
/// The kinds of physical law a packing layout can break.
/// </summary>
public enum ViolationKind
{
    /// <summary>A package extends beyond the container walls.</summary>
    OutOfBounds,

    /// <summary>Two packages share the same volume.</summary>
    Overlap,

    /// <summary>A package is not resting on the floor or on other packages.</summary>
    Floating,

    /// <summary>More weight rests on a package than it can carry.</summary>
    Overloaded,

    /// <summary>Something is stacked on a fragile package.</summary>
    StackedOnFragile,

    /// <summary>The packed weight exceeds the container's capacity.</summary>
    ContainerOverweight
}

/// <summary>
/// A single problem found in a packing layout.
/// </summary>
/// <param name="Kind">The category of problem.</param>
/// <param name="Description">A human-readable explanation.</param>
/// <param name="Package">The package at fault, when the problem concerns one.</param>
/// <param name="Other">The second package involved, for problems concerning a pair.</param>
public record PackingViolation(
    ViolationKind Kind,
    string Description,
    PlacedPackage? Package = null,
    PlacedPackage? Other = null);

/// <summary>
/// The outcome of validating a packing layout.
/// </summary>
/// <param name="Violations">Every problem found, in the order they were detected.</param>
public record PackingValidationReport(IReadOnlyList<PackingViolation> Violations)
{
    /// <summary>
    /// Gets whether the layout is physically realisable.
    /// </summary>
    public bool IsValid => Violations.Count == 0;

    /// <summary>
    /// Counts the violations of a given kind.
    /// </summary>
    /// <param name="kind">The kind to count.</param>
    /// <returns>The number of matching violations.</returns>
    public int CountOf(ViolationKind kind) => Violations.Count(v => v.Kind == kind);

    /// <summary>
    /// Renders the report as readable lines.
    /// </summary>
    /// <returns>A summary suitable for a console or a test failure message.</returns>
    public override string ToString() =>
        IsValid
            ? "Layout is physically valid."
            : string.Join(Environment.NewLine, Violations.Select(v => $"[{v.Kind}] {v.Description}"));
}
