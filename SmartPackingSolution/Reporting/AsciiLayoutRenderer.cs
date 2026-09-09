namespace SmartPackingSolution.Reporting;

using System.Text;
using SmartPackingSolution.Models;

/// <summary>
/// Draws a packed container as text, so a layout can be read at a glance instead of
/// inferred from a list of coordinates.
/// </summary>
public static class AsciiLayoutRenderer
{
    private const string Symbols = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>
    /// Renders a plan view and an elevation of the packed container, with a legend.
    /// </summary>
    /// <param name="result">The layout to draw.</param>
    /// <param name="width">The width of each view in characters.</param>
    /// <returns>The rendered views.</returns>
    public static string Render(PackingResult result, int width = 64)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 8);

        if (result.PackedItems.Count == 0)
        {
            return "(nothing packed)";
        }

        var symbols = AssignSymbols(result.PackedItems);
        var builder = new StringBuilder();

        builder.AppendLine($"Plan view - looking down on {result.Container.Name} " +
            $"({result.Container.Dimensions.Length:F0} x {result.Container.Dimensions.Width:F0} cm)");
        builder.AppendLine(RenderPlan(result, symbols, width));
        builder.AppendLine();

        builder.AppendLine($"Elevation - looking along the width " +
            $"({result.Container.Dimensions.Length:F0} x {result.Container.Dimensions.Height:F0} cm)");
        builder.AppendLine(RenderElevation(result, symbols, width));
        builder.AppendLine();

        builder.AppendLine("Legend");
        foreach (var (item, symbol) in symbols)
        {
            builder.AppendLine(
                $"  {symbol}  {item.Package.Name,-24} " +
                $"{item.ActualDimensions.Length:F0}x{item.ActualDimensions.Width:F0}x{item.ActualDimensions.Height:F0}cm " +
                $"at ({item.Position.X:F0}, {item.Position.Y:F0}, {item.Position.Z:F0}) " +
                $"{item.Package.Priority}");
        }

        return builder.ToString().TrimEnd();
    }

    private static Dictionary<PlacedPackage, char> AssignSymbols(IReadOnlyList<PlacedPackage> packed)
    {
        var map = new Dictionary<PlacedPackage, char>();

        for (var i = 0; i < packed.Count; i++)
        {
            map[packed[i]] = Symbols[i % Symbols.Length];
        }

        return map;
    }

    /// <summary>
    /// Draws the container from above, showing whichever package is uppermost at each point.
    /// </summary>
    /// <param name="result">The layout to draw.</param>
    /// <param name="symbols">The character assigned to each package.</param>
    /// <param name="width">The view width in characters.</param>
    /// <returns>The plan view.</returns>
    private static string RenderPlan(
        PackingResult result,
        Dictionary<PlacedPackage, char> symbols,
        int width)
    {
        var dimensions = result.Container.Dimensions;
        var height = Math.Max(4, (int)Math.Round(width * dimensions.Width / dimensions.Length / 2));

        return RenderGrid(width, height, (u, v) =>
        {
            var x = u * dimensions.Length;
            var y = v * dimensions.Width;

            PlacedPackage? top = null;
            foreach (var item in result.PackedItems)
            {
                if (x >= item.Bounds.MinX && x < item.Bounds.MaxX &&
                    y >= item.Bounds.MinY && y < item.Bounds.MaxY &&
                    (top is null || item.TopZ > top.TopZ))
                {
                    top = item;
                }
            }

            return top is null ? '.' : symbols[top];
        });
    }

    /// <summary>
    /// Draws the container from the side, showing the package nearest the viewer.
    /// </summary>
    /// <param name="result">The layout to draw.</param>
    /// <param name="symbols">The character assigned to each package.</param>
    /// <param name="width">The view width in characters.</param>
    /// <returns>The elevation, with the container floor at the bottom.</returns>
    private static string RenderElevation(
        PackingResult result,
        Dictionary<PlacedPackage, char> symbols,
        int width)
    {
        var dimensions = result.Container.Dimensions;
        var height = Math.Max(4, (int)Math.Round(width * dimensions.Height / dimensions.Length / 2));

        return RenderGrid(width, height, (u, v) =>
        {
            var x = u * dimensions.Length;

            // Rows run top to bottom on screen, but Z runs up from the container floor.
            var z = (1 - v) * dimensions.Height;

            PlacedPackage? nearest = null;
            foreach (var item in result.PackedItems)
            {
                if (x >= item.Bounds.MinX && x < item.Bounds.MaxX &&
                    z >= item.Bounds.MinZ && z < item.Bounds.MaxZ &&
                    (nearest is null || item.Bounds.MinY < nearest.Bounds.MinY))
                {
                    nearest = item;
                }
            }

            return nearest is null ? '.' : symbols[nearest];
        });
    }

    private static string RenderGrid(int width, int height, Func<double, double, char> sample)
    {
        var builder = new StringBuilder();
        builder.Append('+').Append('-', width).AppendLine("+");

        for (var row = 0; row < height; row++)
        {
            builder.Append('|');

            for (var column = 0; column < width; column++)
            {
                // Sample at the centre of each cell so edges do not bleed into neighbours.
                builder.Append(sample((column + 0.5) / width, (row + 0.5) / height));
            }

            builder.AppendLine("|");
        }

        builder.Append('+').Append('-', width).Append('+');
        return builder.ToString();
    }
}
