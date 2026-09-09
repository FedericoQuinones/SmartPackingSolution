namespace SmartPackingSolution.Reporting;

using System.Text.Json;
using System.Text.Json.Serialization;
using SmartPackingSolution.Models;

/// <summary>
/// Serialises a layout to JSON, so it can be handed to a viewer, stored, or diffed.
/// </summary>
public static class PackingJsonExporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,

        // Shorthand anonymous members keep their PascalCase names while explicitly named
        // ones do not, so without a policy the document mixes both conventions.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Converts a packing result to JSON.
    /// </summary>
    /// <param name="result">The layout to export.</param>
    /// <returns>The layout as an indented JSON document.</returns>
    public static string ToJson(PackingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var document = new
        {
            strategy = result.StrategyName,
            elapsedMs = result.Elapsed.TotalMilliseconds,
            container = new
            {
                result.Container.Id,
                result.Container.Name,
                length = result.Container.Dimensions.Length,
                width = result.Container.Dimensions.Width,
                height = result.Container.Dimensions.Height,
                maxWeight = result.Container.MaxWeight
            },
            metrics = new
            {
                packed = result.PackedItems.Count,
                unpacked = result.UnpackedItems.Count,
                spaceUtilization = result.SpaceUtilization,
                boundingBoxUtilization = result.BoundingBoxUtilization,
                totalWeight = result.TotalWeight,
                maxStackHeight = result.MaxStackHeight,
                loadBalanceScore = result.LoadBalanceScore,
                centerOfGravity = new
                {
                    result.CenterOfGravity.X,
                    result.CenterOfGravity.Y,
                    result.CenterOfGravity.Z
                }
            },
            packedItems = result.PackedItems.Select(p => new
            {
                id = p.Package.Id,
                name = p.Package.Name,
                priority = p.Package.Priority,
                weight = p.Package.Weight,
                orientation = p.Orientation,
                position = new { p.Position.X, p.Position.Y, p.Position.Z },
                size = new
                {
                    length = p.ActualDimensions.Length,
                    width = p.ActualDimensions.Width,
                    height = p.ActualDimensions.Height
                }
            }),
            unpackedItems = result.UnpackedItems.Select(u => new
            {
                id = u.Package.Id,
                name = u.Package.Name,
                reason = u.Reason,
                details = u.Details
            })
        };

        return JsonSerializer.Serialize(document, SerializerOptions);
    }
}
