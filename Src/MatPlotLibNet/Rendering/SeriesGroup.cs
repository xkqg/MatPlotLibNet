// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering;

/// <summary>The group a series is drawn in, as a value a 3-D scene's depth queue opens around each run of the series' marks
/// (<see cref="DepthQueue3D"/>): the axes that owns the series, the series and its identity slot, the same group
/// <see cref="SeriesPass"/> opens for a series drawn at its turn.</summary>
/// <param name="Axes">The axes that owns the series.</param>
/// <param name="Series">The series.</param>
/// <param name="Slot">The series' identity slot, which its group carries.</param>
internal readonly record struct SeriesGroup(Axes Axes, ISeries Series, int Slot)
{
    /// <summary>Opens the group on <paramref name="ctx"/>: the series' group of a pass whose series place their marks themselves (no
    /// mapping for a data cursor).</summary>
    /// <param name="ctx">The surface the series is drawn on.</param>
    /// <returns><see langword="true"/> when a group was opened; the caller closes it.</returns>
    internal bool OpenOn(IRenderContext ctx) => ctx.BeginSeriesGroup(Axes, Series, Slot);
}
