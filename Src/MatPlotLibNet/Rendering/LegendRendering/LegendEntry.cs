// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>One row of a legend: the text it prints and the handle drawn before it. A row stands for a series
/// (<see cref="SeriesLegendEntry"/>) or for a patch (<see cref="PatchLegendEntry"/>). What differs between the two (the handle, the
/// index a click toggles, what makes two rows look alike) each kind says for itself, so the drawing, the hit-test, the margin
/// reservation and the legend of a figure read every row alike and never ask which kind it is.</summary>
/// <param name="Label">The text the legend prints for the row.</param>
internal abstract record LegendEntry(string Label)
{
    /// <summary>The index a click on the row toggles: the number its drawn group carries (<c>data-legend-index</c>) and its hit-test
    /// bounds carry, which the drawn group of what it stands for carries too (<c>data-series-index</c>). A series' identity slot, or
    /// for a patch the place after every series of its axes (<see cref="PatchLegendEntry"/>), so the two never meet.</summary>
    internal abstract int ToggleIndex { get; }

    /// <summary>Draws the row's handle in <paramref name="swatch"/> through <paramref name="legend"/>, the one drawer of handles,
    /// which draws each kind of row its own way.</summary>
    /// <param name="legend">The legend being drawn.</param>
    /// <param name="swatch">The handle's box: matplotlib's handle length wide and handle height high.</param>
    /// <param name="fontSize">The size of the legend's text, which the spacing of the markers of a handle is a share of.</param>
    internal abstract void DrawSwatch(LegendRenderer legend, Rect swatch, double fontSize);

    /// <summary>Whether the row looks like <paramref name="other"/> once both are drawn under <paramref name="theme"/>: two rows that
    /// look alike are the same row to the eye, which the legend of a figure lists once.</summary>
    /// <param name="other">The other row.</param>
    /// <param name="theme">The theme both rows are drawn under.</param>
    /// <returns><see langword="true"/> when the two rows look alike.</returns>
    internal abstract bool LooksLike(LegendEntry other, Theme theme);
}
