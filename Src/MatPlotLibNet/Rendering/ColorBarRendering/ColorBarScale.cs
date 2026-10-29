// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>The ticks of a colour bar and where each stands along it: the one place a colour bar decides which values it
/// names. The ticks come from a tick locator, as an axis's do, so a range of 0 to 11 reads 0, 2, 4, 6, 8 and 10 and not
/// fifths of 11; how many it asks for depends on how long the bar is, so a short bar is not crowded.</summary>
/// <param name="Min">The value at the start of the gradient (the bottom of a vertical bar, the left of a horizontal one).</param>
/// <param name="Max">The value at the end of the gradient.</param>
/// <param name="Ticks">The tick values inside the range, in ascending order; none when the range cannot be drawn.</param>
/// <param name="Labels">The text of each tick, in one number format, beside <paramref name="Ticks"/>.</param>
internal readonly record struct ColorBarScale(double Min, double Max, double[] Ticks, string[] Labels)
{
    /// <summary>The ticks of a bar <paramref name="lengthPx"/> long whose labels are set in a font of
    /// <paramref name="tickFontPx"/>.</summary>
    /// <param name="min">The value at the start of the gradient; it may be above <paramref name="max"/>.</param>
    /// <param name="max">The value at the end of the gradient.</param>
    /// <param name="lengthPx">The length of the bar along which the ticks stand.</param>
    /// <param name="tickFontPx">The size of the tick font.</param>
    /// <param name="vertical">Whether the bar is vertical, where a label takes the height of its font, and not horizontal,
    /// where it takes about its width.</param>
    /// <returns>The scale; with no tick when the range is not finite or has no width.</returns>
    internal static ColorBarScale For(double min, double max, double lengthPx, double tickFontPx, bool vertical)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || min == max) return new(min, max, [], []);

        double lo = Math.Min(min, max), hi = Math.Max(min, max);
        double[] ticks = [.. MaxNLocator.ForLength(lengthPx, tickFontPx, vertical ? MaxNLocator.VerticalLabelSpan : MaxNLocator.HorizontalLabelSpan)
            .Locate(lo, hi).Where(tick => tick >= lo && tick <= hi)];
        return new(min, max, ticks, ticks.Labelled());
    }

    /// <summary>The widest label the ticks of this range can have over every number of ticks a bar of any length asks for,
    /// which the layout needs because it reserves the room before it knows how long the bar turns out to be.</summary>
    /// <param name="min">The value at the start of the gradient.</param>
    /// <param name="max">The value at the end of the gradient.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="font">The tick font.</param>
    /// <returns>The widest label width in pixels; 0 when the range cannot be drawn.</returns>
    internal static double WidestLabel(double min, double max, IRenderContext ctx, Styling.Font font)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || min == max) return 0;

        double lo = Math.Min(min, max), hi = Math.Max(min, max), widest = 0;
        for (int most = MaxNLocator.FewestTicks; most <= MaxNLocator.MostTicks; most++)
        {
            double[] ticks = [.. new MaxNLocator(most).Locate(lo, hi).Where(tick => tick >= lo && tick <= hi)];
            foreach (var label in ticks.Labelled())
                widest = Math.Max(widest, ctx.MeasureText(label, font).Width);
        }
        return widest;
    }

    /// <summary>The fraction of the gradient's length, from its start (0) to its end (1), at which
    /// <paramref name="value"/> stands.</summary>
    /// <param name="value">A value in the range of the bar.</param>
    internal double FractionOf(double value) => (value - Min) / (Max - Min);
}

/// <summary>The range a colour bar covers.</summary>
internal static class ColorBarRangeExtensions
{
    extension(ColorBar cb)
    {
        /// <summary>The range the bar covers: that of the series that provides one, when it has a width, otherwise the bar's
        /// own <see cref="ColorBar.Min"/> and <see cref="ColorBar.Max"/>; a range with no width to 1e-10 is 0 to 1. The axes'
        /// bar, the figure's bar and the layout that reserves room for either read it, so they cannot disagree.</summary>
        /// <param name="provider">The first series of the chart that provides a range, or <see langword="null"/>.</param>
        internal MinMaxRange RangeOf(IColorBarDataProvider? provider)
        {
            double min = cb.Min, max = cb.Max;
            if (provider is not null)
            {
                var (dMin, dMax) = provider.GetColorBarRange();
                if (dMin < dMax) { min = dMin; max = dMax; }
            }
            return Math.Abs(max - min) < 1e-10 ? new MinMaxRange(0, 1) : new MinMaxRange(min, max);
        }
    }
}
