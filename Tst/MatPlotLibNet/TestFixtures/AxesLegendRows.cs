// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>The series rows of an axes' legend asked outside a draw, for a test that has no renderer: the axes' own legend values and a colour-slot
/// table made for the occasion, where a draw is handed the table its renderer made once.</summary>
internal static class AxesLegendRows
{
    extension(Axes axes)
    {
        /// <summary>The series rows of <paramref name="axes"/> under <paramref name="theme"/>.</summary>
        /// <param name="theme">The theme the axes is drawn under.</param>
        /// <param name="directLabelsDrawn">Whether the renderer of these axes draws end-of-line labels.</param>
        internal IReadOnlyList<SeriesLegendEntry> LegendEntries(Theme theme, bool directLabelsDrawn) =>
            SeriesLegendEntry.RowsOf(axes, theme, directLabelsDrawn, axes.Legend.LegendValues, axes.ColorSlots(theme));
    }
}
