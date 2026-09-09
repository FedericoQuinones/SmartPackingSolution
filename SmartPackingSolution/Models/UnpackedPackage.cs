namespace SmartPackingSolution.Models;

/// <summary>
/// A package that could not be placed, together with the reason it was rejected.
/// </summary>
/// <param name="Package">The package that was not placed.</param>
/// <param name="Reason">The category of failure.</param>
/// <param name="Details">An optional human-readable explanation.</param>
public record UnpackedPackage(PackageItem Package, UnpackedReason Reason, string? Details = null);
