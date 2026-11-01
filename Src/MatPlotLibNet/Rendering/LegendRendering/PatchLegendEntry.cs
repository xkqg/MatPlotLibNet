// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>The row of a labelled patch in a legend, as matplotlib lists one: its label and a rectangle the full handle long and
/// high in the patch's own style (<see cref="Patch.DrawnStyle"/>: face, edge, width, dash and hatch, the opacity inside each
/// colour), whatever the shape. A click on it toggles nothing: a patch is not a series.</summary>
/// <param name="Patch">The patch the row stands for.</param>
/// <param name="Label">The patch's label, which names it in the legend: never empty
/// (<see cref="LegendEntry"/>'s <c>PatchLegendEntries</c> lists only a patch that has one).</param>
internal sealed record PatchLegendEntry(Patch Patch, string Label) : LegendEntry(Label)
{
    /// <inheritdoc />
    /// <remarks>Always <see langword="null"/>: the row has no drawn group a toggle reads and no hit-test bounds.</remarks>
    internal override int? ToggleIndex => null;

    /// <inheritdoc />
    internal override void DrawSwatch(LegendRenderer legend, Rect swatch) => legend.DrawSwatchOf(this, swatch);

    /// <inheritdoc />
    /// <remarks>The label and the style the patch is drawn in.</remarks>
    internal override object LookUnder(Theme theme) => new Look(Label, Patch.DrawnStyle);

    /// <summary>What a patch row looks like once drawn.</summary>
    private readonly record struct Look(string Label, ShapeStyle Style);
}
