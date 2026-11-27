// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>What the one loop of a series pass (<see cref="SeriesPass.DrawSteps{THost}"/>) asks of the renderer it draws for: the colour slot
/// of each series, what belongs inside a series' group beside its marks, and the drawing of every step that is not a series (the hook
/// each kind of axes answers for its own layers). Every axes renderer is one (<see cref="AxesRenderer"/>), so a Y axis stacked past the
/// primary and the top X axis draw their series through the renderer of their axes, as its primary series are drawn.</summary>
internal interface ILayerHost
{
    /// <summary>The colour slot of each series of the axes in identity order (<see cref="SeriesSlotExtensions.ColorSlots"/>).</summary>
    int[] ColorSlots { get; }

    /// <summary>Draws what belongs to the series at identity slot <paramref name="seriesIndex"/> beyond its own marks, while its group
    /// is open (the Cartesian renderer's end label).</summary>
    /// <param name="seriesIndex">The series' identity slot.</param>
    void DrawInsideSeriesGroup(int seriesIndex);

    /// <summary>Draws step <paramref name="at"/> of <paramref name="steps"/>, a step that is not a series (the grid, the spans, a run of
    /// patches, the reference lines, the drawing tools, the tick marks), and says the last step it drew, so a step that draws the steps
    /// after it too (a run of patches in one clip) is not drawn twice.</summary>
    /// <param name="pass">The pass the steps belong to.</param>
    /// <param name="steps">The steps, in the order they are drawn in.</param>
    /// <param name="at">The step to draw.</param>
    /// <returns>The index of the last step drawn.</returns>
    int DrawLayerAt(in SeriesPass pass, ReadOnlySpan<LayerStep> steps, int at);
}
