// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Rendering;

/// <summary>The radial scale of one polar axes: how far its rings reach and which rings it draws. The grid and every series
/// on the axes read it, so a ring labelled 10 stands exactly where a radius of 10 is drawn, whichever series draws it, and
/// two series share their scale. The extent is the farthest radius any series reaches, 5 % further out so the farthest mark
/// does not sit on the outer ring, or the limit the axes was given, exactly; a series whose cells end at a radius of their
/// own (a polar heatmap) fixes the extent at least that far, without the margin.</summary>
/// <param name="Extent">The radius at the outermost ring.</param>
/// <param name="Rings">The radii of the rings, in ascending order; none at the centre.</param>
/// <param name="Labels">The text of each ring, in one number format, beside <paramref name="Rings"/>.</param>
/// <param name="Feet">For a stack of polar bars (<see cref="BarMode.Stacked"/>), where each bar series of the axes starts, wedge
/// by wedge: the sum of the bars before it, aligned by index (the wedges of one angle are the same index of every series);
/// <see langword="null"/> when the bars are not stacked. Worked out here, read by the bar renderer, and never written into the
/// series, so a stacked render leaves nothing in the figure or its JSON.</param>
internal readonly record struct RadialScale(double Extent, double[] Rings, string[] Labels, IReadOnlyDictionary<PolarBarSeries, double[]>? Feet = null)
{
    /// <summary>Where <paramref name="bar"/> starts, wedge by wedge, when the bars are stacked; <see langword="null"/>
    /// when they are not.</summary>
    /// <param name="bar">A bar series of the axes.</param>
    internal double[]? FootOf(PolarBarSeries bar) => Feet is not null && Feet.TryGetValue(bar, out var feet) ? feet : null;

    /// <summary>The angle, from the right-hand spoke and counter-clockwise, at which the rings are labelled (matplotlib's
    /// <c>rlabel_position</c>), clear of the spokes the bars of a wind rose stand on.</summary>
    internal const double LabelAngleDegrees = 22.5;

    /// <summary>The margin the farthest mark leaves before the outer ring.</summary>
    private const double Margin = 1.05;

    /// <summary>The extent of the scale of <paramref name="series"/>: the limit when the axes was given one, otherwise the
    /// farthest any series reaches, hidden ones too (hiding a series does not move the rings), and 1 when none reaches
    /// anywhere.</summary>
    /// <param name="series">The series of the axes.</param>
    /// <param name="limit">The limit the axes' radial axis was given, or <see langword="null"/>.</param>
    /// <param name="stackTop">How high the stack of polar bars reaches; 0 when they are not stacked.</param>
    internal static double ExtentOf(IReadOnlyList<ISeries> series, double? limit, double stackTop = 0)
    {
        if (limit is { } given && double.IsFinite(given) && given > 0) return given;

        double sticky = 0, marks = stackTop;
        foreach (var one in series)
        {
            if (one is not ChartSeries { RadialExtent: { } reach }) continue;
            if (reach.Sticky) sticky = Math.Max(sticky, reach.Max);
            else marks = Math.Max(marks, reach.Max);
        }

        double extent = Math.Max(sticky, marks * Margin);
        return extent > 0 ? extent : 1;
    }

    /// <summary>The scale a series makes alone, with no axes around it, for a renderer that is handed none: its extent and no
    /// rings.</summary>
    /// <param name="series">The series.</param>
    internal static RadialScale Alone(ISeries series) => new(ExtentOf([series], null), [], []);

    /// <summary>The scale of an axes.</summary>
    /// <param name="series">The series of the axes.</param>
    /// <param name="axis">The axes' radial axis: its limit and its tick formatter.</param>
    /// <param name="maxRadiusPx">The radius of the outermost ring in pixels: the rings ask for as many ticks as fit along it.</param>
    /// <param name="tickFontPx">The size of the tick font.</param>
    /// <param name="barMode">How the bars of the axes are placed: stacked bars stand on one another and the scale reaches the
    /// top of the stack.</param>
    internal static RadialScale For(IReadOnlyList<ISeries> series, Axis axis, double maxRadiusPx, double tickFontPx,
        BarMode barMode = BarMode.Grouped)
    {
        var stack = barMode == BarMode.Stacked ? StackOf(series) : null;
        double extent = ExtentOf(series, axis.Max, stack?.Top ?? 0);
        double[] rings = [.. MaxNLocator.ForLength(maxRadiusPx, tickFontPx, MaxNLocator.VerticalLabelSpan)
            .Locate(0, extent).Where(ring => ring > 0 && ring <= extent)];
        string[] labels = axis.TickFormatter is { } formatter ? [.. rings.Select(formatter.Format)] : rings.Labelled();
        return new(extent, rings, labels, stack?.Feet);
    }

    /// <summary>The stack of the bar series of the axes: where each stands and how high the stack reaches. A radius that is
    /// not drawn (missing, infinite or below zero) adds nothing. One bar series is not a stack.</summary>
    private static PolarStack? StackOf(IReadOnlyList<ISeries> series)
    {
        var bars = series.OfType<PolarBarSeries>().ToList();
        if (bars.Count < 2) return null;

        double[][] layers = [.. bars.Select(bar => bar.R.Select(radius => double.IsFinite(radius) && radius > 0 ? radius : 0).ToArray())];
        var feet = layers.StackedFeet();
        var ends = layers.StackedEnds();
        return new PolarStack(bars.Zip(feet).ToDictionary(pair => pair.First, pair => pair.Second), ends.SelectMany(end => end).DefaultIfEmpty(0).Max());
    }

    /// <summary>The stack of the polar bars of an axes: where each bar series starts, and how high the stack reaches.</summary>
    private readonly record struct PolarStack(IReadOnlyDictionary<PolarBarSeries, double[]> Feet, double Top);
}
