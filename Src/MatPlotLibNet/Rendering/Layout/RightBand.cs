// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>What the right edge of one axes draws past its right spine, in drawing order: the axis side first (the
/// secondary Y axis or the mirrored primary Y ticks), then the end-of-line labels.
/// <see cref="AxesEdgeNeeds.MeasureRightBand"/> measures it.</summary>
/// <param name="SecondaryAxis">From the right spine to the far side of the secondary Y axis: its tick marks, padding
/// and widest tick label, then its title when it has one. 0 without a secondary Y axis.</param>
/// <param name="MirroredYTicks">From the right spine to the far side of the mirrored primary Y tick labels
/// (<see cref="EdgeBands.Right"/>). 0 when none are drawn.</param>
/// <param name="EndLabels">The gap before the end labels plus the widest of them. 0 when none is drawn.</param>
internal readonly record struct RightBand(double SecondaryAxis, double MirroredYTicks, double EndLabels)
{
    /// <summary>The axis side of the edge, where the end labels start: the secondary axis or the mirrored ticks,
    /// whichever reaches further. A drawn secondary axis owns the edge, so at most one of them is drawn; the wider one
    /// is charged because the secondary axis is reserved even when it has nothing to draw.</summary>
    public double Axis => Math.Max(SecondaryAxis, MirroredYTicks);

    /// <summary>What is drawn past the right spine: the axis side and the end labels stacked. A colour bar beside the plot in the
    /// library's margin stands past it.</summary>
    public double Past => Axis + EndLabels;

    /// <summary>The room the right edge needs: the axis side and the end labels stacked, plus the layout's pad on the right
    /// (<see cref="LayoutPads.Edges"/>) when either is present. 0 when nothing is drawn past the spine.</summary>
    /// <param name="pad">The pad the layout keeps right of what an axes needs.</param>
    /// <returns>The room, in pixels.</returns>
    public double WidthWith(double pad) => Past > 0 ? Past + pad : 0;

    /// <summary>The one composition of the band past the right spine of <paramref name="axes"/>: the bands of its right Y axes stacked with
    /// the container's gap between each pair (<see cref="AxisStackingExtensions.TotalStackedBand"/>, the rule the render stacks them by),
    /// the mirrored Y tick labels, then the end labels. The layout composes it from the bands it measures
    /// (<see cref="AxesEdgeNeeds.MeasureRightBand"/>), a render from the bands it placed (<see cref="Placed"/>).</summary>
    /// <param name="axes">The axes, whose Y axis container says the gap.</param>
    /// <param name="theme">The theme the axes is drawn under.</param>
    /// <param name="rightAxisBands">The band of each right Y axis, nearest the plot first.</param>
    /// <param name="mirroredYTicks">The band the mirrored Y tick labels take; 0 when none are drawn.</param>
    /// <param name="endLabels">The gap before the end labels plus the widest of them; 0 when none is drawn.</param>
    /// <returns>The band.</returns>
    internal static RightBand Of(Axes axes, Theme theme, IReadOnlyList<double> rightAxisBands, double mirroredYTicks, double endLabels) =>
        new(rightAxisBands.TotalStackedBand(axes.YAxes.TitleGapUnder(theme)), mirroredYTicks, endLabels);

    /// <summary>The band a render draws past the right spine (<see cref="Of"/>): the right slots it placed (<paramref name="slots"/>, each
    /// at its <see cref="PlacedAxis.Band"/>), or the mirrored Y tick labels, then the end labels.</summary>
    /// <param name="axes">The axes, whose Y axis container says the gap.</param>
    /// <param name="theme">The theme the axes is drawn under.</param>
    /// <param name="slots">Every Y axis slot the render placed; the right ones are read.</param>
    /// <param name="mirroredYTicks">The band the mirrored Y tick labels took; 0 when none were drawn.</param>
    /// <param name="endLabels">The gap before the end labels plus the widest of them; 0 when none is drawn.</param>
    /// <returns>The band.</returns>
    internal static RightBand Placed(Axes axes, Theme theme, IReadOnlyList<PlacedAxis> slots, double mirroredYTicks, double endLabels)
    {
        int count = 0;
        for (int i = 0; i < slots.Count; i++)
            if (ReferenceEquals(slots[i].Edge, EdgeTerms.Right)) count++;
        var bands = new double[count];
        for (int i = 0, k = 0; i < slots.Count; i++)
            if (ReferenceEquals(slots[i].Edge, EdgeTerms.Right)) bands[k++] = slots[i].Band;
        return Of(axes, theme, bands, mirroredYTicks, endLabels);
    }
}
