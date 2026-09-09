namespace SmartPackingSolution.Models;

/// <summary>
/// Explains why a package could not be placed in a container.
/// </summary>
public enum UnpackedReason
{
    /// <summary>The package does not fit the container in any orientation, even when empty.</summary>
    TooLargeForContainer,

    /// <summary>Adding the package would exceed the container's maximum weight.</summary>
    WeightCapacityExceeded,

    /// <summary>No remaining free volume could accommodate the package.</summary>
    NoSpaceAvailable,

    /// <summary>Every candidate placement would overload an item beneath it.</summary>
    LoadBearingConstraint,

    /// <summary>Every candidate placement left the package insufficiently supported from below.</summary>
    SupportConstraint,

    /// <summary>Packing stopped early because the configured time budget elapsed.</summary>
    TimeBudgetExceeded
}
