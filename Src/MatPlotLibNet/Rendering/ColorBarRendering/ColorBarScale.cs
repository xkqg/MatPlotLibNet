// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>The ticks of a colour bar and where each stands along it: the one place a colour bar decides which values it
/// names and where they stand. The ticks come from a tick locator, as an axis's do, so a range of 0 to 11 reads 0, 2, 4, 6, 8
/// and 10 and not fifths of 11; how many it asks for depends on how long the bar is, so a short bar is not crowded.
/// <para>A value stands where the chart's normaliser sends its colour: the gradient is the colour map laid out evenly along the
/// bar, and the colour of a value is the map's at the normaliser's fraction of it, so under a log normaliser the tick of 200 in
/// 1 to 1000 stands at 0.767 of the bar and not at its 0.199 share of the range. A tick that would stand within one label of the
/// one before it is left out.</para>
/// <para>A normaliser that sends the values between two boundaries to one colour (<see cref="IDiscreteNormalizer"/>) makes the
/// bar bands: a tick on each boundary, the boundaries evenly spaced, and a gradient of one colour per band.</para></summary>
/// <param name="Min">The value at the start of the gradient (the bottom of a vertical bar, the left of a horizontal one).</param>
/// <param name="Max">The value at the end of the gradient.</param>
/// <param name="Ticks">The tick values, in ascending order; none when the range cannot be drawn.</param>
/// <param name="Labels">The text of each tick, in one number format, beside <paramref name="Ticks"/>.</param>
/// <param name="Fractions">Where each tick stands along the gradient, from its start (0) to its end (1), beside
/// <paramref name="Ticks"/>.</param>
/// <param name="Bins">The number of bands of a bar of bands; 0 for a bar of continuous colour.</param>
internal readonly record struct ColorBarScale(double Min, double Max, double[] Ticks, string[] Labels, double[] Fractions, int Bins)
{
    /// <summary>The ticks of a bar <paramref name="lengthPx"/> long whose labels are set in <paramref name="tickFont"/>.</summary>
    /// <param name="min">The value at the start of the gradient; it may be above <paramref name="max"/>.</param>
    /// <param name="max">The value at the end of the gradient.</param>
    /// <param name="lengthPx">The length of the bar along which the ticks stand.</param>
    /// <param name="vertical">Whether the bar is vertical, where a label takes the height of its font, and not horizontal,
    /// where it takes its width.</param>
    /// <param name="normalizer">The normaliser the chart colours its values with.</param>
    /// <param name="ctx">The context the width of a label is measured with.</param>
    /// <param name="tickFont">The tick font.</param>
    /// <returns>The scale; with no tick when the range is not finite or has no width.</returns>
    internal static ColorBarScale For(double min, double max, double lengthPx, bool vertical,
        INormalizer normalizer, IRenderContext ctx, Styling.Font tickFont)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || min == max) return new(min, max, [], [], [], 0);

        double[] ticks, fractions;
        int bins = 0;
        if (normalizer is IDiscreteNormalizer { Boundaries.Length: >= 2 } bands)
        {
            bins = bands.Boundaries.Length - 1;
            ticks = bands.Boundaries;
            fractions = [.. Enumerable.Range(0, ticks.Length).Select(k => (double)k / bins)];
        }
        else
        {
            // The ticks are chosen in the value space, as an axis chooses them, and placed where the normaliser puts them. The
            // normaliser takes an ascending range, so a range from high to low is placed in the ascending one and mirrored.
            double lo = Math.Min(min, max), hi = Math.Max(min, max);
            ticks = [.. MaxNLocator.ForLength(lengthPx, tickFont.Size, vertical ? MaxNLocator.VerticalLabelSpan : MaxNLocator.HorizontalLabelSpan)
                .Locate(lo, hi).Where(tick => tick >= lo && tick <= hi)];
            fractions = [.. ticks.Select(tick => min > max ? 1 - normalizer.Normalize(tick, lo, hi) : normalizer.Normalize(tick, lo, hi))];
        }

        string[] labels = ticks.Labelled();
        int[] kept = KeptTicks(fractions, labels, lengthPx, vertical, ctx, tickFont);
        return new(min, max, [.. kept.Select(i => ticks[i])], [.. kept.Select(i => labels[i])], [.. kept.Select(i => fractions[i])], bins);
    }

    /// <summary>The ticks that stay, as indices into the ticks in the order they stand in: walking along the bar from its
    /// start, a tick stays when it stands a label or more past the one before it that stayed. A label takes the height of its
    /// font on a vertical bar, and its width on a horizontal one. A bar of continuous colour under a linear normaliser spaces its
    /// ticks further apart than that to begin with, so none is left out there.</summary>
    private static int[] KeptTicks(double[] fractions, string[] labels, double lengthPx, bool vertical, IRenderContext ctx, Styling.Font tickFont)
    {
        var kept = new List<int>();
        foreach (int i in Enumerable.Range(0, fractions.Length).OrderBy(i => fractions[i]))
        {
            if (kept.Count > 0)
            {
                double gap = (fractions[i] - fractions[kept[^1]]) * lengthPx;
                double needed = vertical ? tickFont.Size : ctx.MeasureText(labels[i], tickFont).Width;
                if (gap < needed) continue;
            }

            kept.Add(i);
        }

        kept.Sort();
        return [.. kept];
    }

    /// <summary>The widest label the ticks of this range can have over every number of ticks a bar of any length asks for,
    /// which the layout needs because it reserves the room before it knows how long the bar turns out to be. A tick is only
    /// ever left out, so what is drawn is never wider than this. A bar of bands names its boundaries, and they count too.</summary>
    /// <param name="min">The value at the start of the gradient.</param>
    /// <param name="max">The value at the end of the gradient.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="font">The tick font.</param>
    /// <param name="normalizer">The normaliser the chart colours its values with.</param>
    /// <returns>The widest label width in pixels; 0 when the range cannot be drawn.</returns>
    internal static double WidestLabel(double min, double max, IRenderContext ctx, Styling.Font font, INormalizer normalizer)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || min == max) return 0;

        double lo = Math.Min(min, max), hi = Math.Max(min, max), widest = 0;
        for (int most = MaxNLocator.FewestTicks; most <= MaxNLocator.MostTicks; most++)
        {
            double[] ticks = [.. new MaxNLocator(most).Locate(lo, hi).Where(tick => tick >= lo && tick <= hi)];
            foreach (var label in ticks.Labelled())
                widest = Math.Max(widest, ctx.MeasureText(label, font).Width);
        }

        if (normalizer is IDiscreteNormalizer bands)
            foreach (var label in bands.Boundaries.Labelled())
                widest = Math.Max(widest, ctx.MeasureText(label, font).Width);
        return widest;
    }

    /// <summary>The fraction of the colour map one step of the gradient is drawn in. A bar of continuous colour draws a step in
    /// the colour at the edge it is sampled at, as it always did; a bar of bands draws it in the colour of the band its middle
    /// falls in, the colour the normaliser gives the values of that band, so a band is one colour from edge to edge.</summary>
    /// <param name="edge">The fraction along the bar at the edge of the step the gradient samples.</param>
    /// <param name="middle">The fraction along the bar at the middle of the step.</param>
    internal double GradientFraction(double edge, double middle) =>
        Bins == 0 ? edge : Math.Min(Math.Floor(middle * Bins), Bins - 1) / Bins;
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
