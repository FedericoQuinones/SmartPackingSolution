namespace SmartPackingSolution.Strategies;

using SmartPackingSolution.Models;

/// <summary>
/// Produces the sequence in which packages are offered to the placement search.
/// </summary>
public static class ItemOrderings
{
    /// <summary>
    /// Sorts packages according to the requested ordering.
    /// </summary>
    /// <param name="items">The packages to sort.</param>
    /// <param name="ordering">The ordering to apply.</param>
    /// <returns>The sorted packages.</returns>
    /// <remarks>
    /// Priority breaks ties but never leads. Sorting by priority first - as the original
    /// implementation did - offers a one-kilogram heavy item before a mattress, which
    /// throws away the whole point of a decreasing-first-fit heuristic. Where an item
    /// ends up vertically is now settled by the physics in
    /// <see cref="PackingContext"/>, not by the order it is offered in.
    /// </remarks>
    public static IReadOnlyList<PackageItem> Apply(
        IReadOnlyList<PackageItem> items,
        ItemOrdering ordering)
    {
        ArgumentNullException.ThrowIfNull(items);

        return ordering switch
        {
            ItemOrdering.AsSupplied => items,
            ItemOrdering.BaseAreaDescending =>
                [.. items.OrderByDescending(i => i.Dimensions.BaseArea)
                         .ThenByDescending(i => i.Priority)],
            ItemOrdering.MaxDimensionDescending =>
                [.. items.OrderByDescending(i => i.Dimensions.LongestSide)
                         .ThenByDescending(i => i.Volume)],
            ItemOrdering.HeaviestFirst =>
                [.. items.OrderByDescending(i => i.Weight)
                         .ThenByDescending(i => i.Volume)],
            _ =>
                [.. items.OrderByDescending(i => i.Volume)
                         .ThenByDescending(i => i.Priority)]
        };
    }
}
