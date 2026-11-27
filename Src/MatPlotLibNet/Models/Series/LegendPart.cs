// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>One entry a series has in its legend: a series is one entry, except the one that colours and names its own parts
/// (a stacked area with named layers), which is an entry for each part. Every part is the same series: it carries the
/// series' index, so a click on any of them reaches the series.</summary>
/// <param name="Label">What the legend prints; an empty label draws no entry.</param>
/// <param name="Swatch">How the part is drawn.</param>
/// <param name="ColorSlotOffset">How far past the series' first colour slot the part's colour stands (the part's position).</param>
/// <param name="Color">The colour the part is drawn in when its caller named one; <see langword="null"/> draws it in the colour of its
/// slot, the theme's colour at the part's position.</param>
internal readonly record struct LegendPart(string Label, LegendSwatch Swatch, int ColorSlotOffset = 0, Color? Color = null);

/// <summary>The parts a series that names and colours each of its own parts says it has (a waffle's categories, a chord's groups,
/// a pie's slices).</summary>
internal static class LegendPartExtensions
{
    extension(IEnumerable<string?> names)
    {
        /// <summary>A part for each name that has text, a filled rectangle, standing at its name's position in the colour its caller
        /// gave at that position (<paramref name="colors"/>), else the theme's colour there. A name with no text draws no row, and the
        /// parts after it keep their positions.</summary>
        /// <param name="colors">The colour of each part by position, <see langword="null"/> or shorter than the names for the parts
        /// that name none.</param>
        /// <param name="swatch">The handle of every part; <see langword="null"/> for a plain filled rectangle (<see cref="LegendSwatch.Patch"/>).</param>
        /// <returns>The parts, in the order of the names.</returns>
        internal IReadOnlyList<LegendPart> AsLegendParts(Color[]? colors = null, LegendSwatch? swatch = null) =>
            [.. names.Select((name, position) => new LegendPart(name ?? string.Empty, swatch ?? LegendSwatch.Patch, position,
                colors.ElementOrNull(position)))
                .Where(part => !string.IsNullOrEmpty(part.Label))];
    }
}
