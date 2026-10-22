// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Serialization;

/// <summary>How the figure JSON describes a colour map that does not travel by the name of a map the library ships (that
/// one is the plain <c>colorMapName</c> field): <c>"type"</c> says which kind it is, the other fields carry what it is
/// made of. A kind this build does not know reads as this base record, which makes no map: the series draws through its
/// renderer's default, and the skip is reported through <see cref="ChartDiagnostics"/>. A description of a known kind
/// that cannot make a map (no colours, positions that do not pair up with them) is refused with an
/// <see cref="ArgumentException"/> that names the field, because the map would otherwise fail only when it is drawn.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(LinearColorMapDto), "linear")]
[JsonDerivedType(typeof(ListedColorMapDto), "listed")]
[JsonDerivedType(typeof(ReversedColorMapDto), "reversed")]
[JsonDerivedType(typeof(NamedColorMapDto), "named")]
internal record ColorMapDto
{
    /// <summary>The colour map this description makes, or <see langword="null"/> for a kind this build does not know.</summary>
    /// <exception cref="ArgumentException">The description cannot make a map; the exception names the field.</exception>
    internal virtual IColorMap? Restore()
    {
        ChartDiagnostics.Emit(new ChartDiagnostic(nameof(ChartSerializer),
            "A colour map of a kind this build does not know was skipped during deserialization; the series draws through its default colour map.", null));
        return null;
    }
}

/// <summary>Describes a <see cref="LinearColorMap"/> built in code.</summary>
/// <param name="Name">The map's name.</param>
/// <param name="Colors">Its colours, from the low end to the high end; at least one.</param>
/// <param name="Positions">Where each colour stands in [0, 1], strictly rising, one per colour; absent when the colours
/// are evenly spaced.</param>
/// <param name="UnderColor">The colour of a value below the range; absent to clamp to the first colour.</param>
/// <param name="OverColor">The colour of a value above the range; absent to clamp to the last colour.</param>
/// <param name="BadColor">The colour of a missing value; absent for transparent.</param>
internal sealed record LinearColorMapDto(string? Name, Color[]? Colors, double[]? Positions, Color? UnderColor, Color? OverColor, Color? BadColor)
    : ColorMapDto
{
    internal override IColorMap Restore()
    {
        if (Colors is not { Length: > 0 } colors)
        {
            throw new ArgumentException("A linear colour map needs at least one colour.", "colors");
        }

        if (Positions is { } positions)
        {
            if (positions.Length != colors.Length)
            {
                throw new ArgumentException($"{positions.Length} positions were given for {colors.Length} colours; there must be one position per colour.", "positions");
            }

            LinearColorMap.ThrowIfNotStops(positions, "positions");
        }

        return new LinearColorMap(Name ?? string.Empty, colors, Positions) { UnderColor = UnderColor, OverColor = OverColor, BadColor = BadColor };
    }
}

/// <summary>Describes a <see cref="ListedColorMap"/> built in code.</summary>
/// <param name="Name">The map's name.</param>
/// <param name="Colors">Its colours, from the low end to the high end; at least one.</param>
/// <param name="UnderColor">The colour of a value below the range; absent to clamp to the first colour.</param>
/// <param name="OverColor">The colour of a value above the range; absent to clamp to the last colour.</param>
/// <param name="BadColor">The colour of a missing value; absent for transparent.</param>
internal sealed record ListedColorMapDto(string? Name, Color[]? Colors, Color? UnderColor, Color? OverColor, Color? BadColor) : ColorMapDto
{
    internal override IColorMap Restore() =>
        Colors is { Length: > 0 } colors
            ? new ListedColorMap(Name ?? string.Empty, colors) { UnderColor = UnderColor, OverColor = OverColor, BadColor = BadColor }
            : throw new ArgumentException("A listed colour map needs at least one colour.", "colors");
}

/// <summary>Describes a <see cref="ReversedColorMap"/>: the map it reverses, described in turn.</summary>
/// <param name="Map">The description of the map it reverses.</param>
internal sealed record ReversedColorMapDto(ColorMapDto? Map) : ColorMapDto
{
    internal override IColorMap? Restore() =>
        (Map ?? throw new ArgumentException("A reversed colour map needs the map it reverses.", "map")).Restore() is { } inner
            ? new ReversedColorMap(inner)
            : null;
}

/// <summary>Names a registered colour map inside another description (the map a reversed map reverses). At the top of a
/// series or a colour bar a name stands in <c>colorMapName</c> instead, the field it always stood in.</summary>
/// <param name="Name">The name the map is registered under; a name nobody registered reads as no map.</param>
internal sealed record NamedColorMapDto(string? Name) : ColorMapDto
{
    internal override IColorMap? Restore() =>
        ColorMapRegistry.Get(Name ?? throw new ArgumentException("A named colour map needs its name.", "name"));
}
