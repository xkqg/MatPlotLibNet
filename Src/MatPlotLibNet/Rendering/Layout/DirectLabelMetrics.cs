// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>One end-of-line label: the series it names, the axis pair it is plotted against, its index in that pair's
/// series list, and the text that is drawn.</summary>
/// <param name="Series">The labelled series.</param>
/// <param name="Pair">The axes it is plotted against (<see cref="SeriesAxisPair.Primary"/> or
/// <see cref="SeriesAxisPair.SecondaryY"/>).</param>
/// <param name="IndexInPair">Its index across EVERY right-side slot's series, in slot order — the same number
/// <see cref="Rendering.SeriesSlotExtensions.SlotIndex"/> expects, not an index within one slot alone.</param>
/// <param name="AxisSlot">Which right-side slot (<see cref="Axes.RightYAxes"/>) this label's line is drawn against;
/// meaningless (always 0) for <see cref="SeriesAxisPair.Primary"/>.</param>
/// <param name="Text">The label as drawn: the series label, plus the legend-values suffix when it applies.</param>
/// <param name="EndX">The x value of the point the label is placed at (data space).</param>
/// <param name="EndY">The y value of that point (data space); the legend-values suffix shows it.</param>
internal readonly record struct DirectLabel(ISeries Series, SeriesAxisPair Pair, int IndexInPair, int AxisSlot,
                                            string Text, double EndX, double EndY);

/// <summary>The one owner of what the end-of-line labels of an axes say, where their lines end, the font they are drawn
/// in and the gap before them. The layout engine reserves the right-edge band with it and the label drawing draws with
/// it, so the two always measure the same text in the same font.</summary>
/// <remarks>A label replaces the series' legend entry, so it wears the legend's font (with
/// <see cref="Legend.FontSize"/> applied) and writes a value the way the legend does
/// (<see cref="AxesRenderer.WithLegendValue"/>). Which point is the end of a line is ONE rule, used for the label's
/// position, for the value it shows and for the legend's value (<see cref="MissingValueExtensions.LastDrawableIndex"/>):
/// the last point, in data order, whose x and y can be drawn and, here, whose x lies in the visible X range. A line
/// without such a point has no label. So a trailing NaN or a point cut off by the X limits never puts "NaN" in the text,
/// and the text names the value the label stands next to.</remarks>
internal sealed class DirectLabelMetrics
{
    /// <summary>The gap, in pixels, between what the label follows and the label's first glyph.</summary>
    internal const double Gap = 6;

    private readonly IRenderContext _ctx;

    /// <summary>Collects the end labels of <paramref name="axes"/> before the axes is drawn, when only its X limits are
    /// known: a limit that is set bounds the visible range, a limit that is not set does not (an automatic range spans
    /// every point).</summary>
    /// <param name="axes">The axes whose labels are described.</param>
    /// <param name="theme">The active theme (for the font).</param>
    /// <param name="ctx">The context the text is measured with.</param>
    internal DirectLabelMetrics(Axes axes, Theme theme, IRenderContext ctx)
        : this(axes, theme, ctx, new MinMaxRange(axes.XAxis.Min ?? double.NegativeInfinity,
                                                 axes.XAxis.Max ?? double.PositiveInfinity)) { }

    /// <summary>Collects the end labels of <paramref name="axes"/> for the X range the axes is drawn with.</summary>
    /// <param name="axes">The axes whose labels are described.</param>
    /// <param name="theme">The active theme (for the font).</param>
    /// <param name="ctx">The context the text is measured with.</param>
    /// <param name="visibleX">The visible X range; its two ends may come in either order.</param>
    internal DirectLabelMetrics(Axes axes, Theme theme, IRenderContext ctx, MinMaxRange visibleX)
    {
        _ctx = ctx;
        Font = LegendMeasurer.LegendFont(axes, theme);
        Labels = Collect(axes, theme, ctx, visibleX);
    }

    /// <summary>The font every end label of the axes is drawn in (the drawing applies the series colour).</summary>
    internal Font Font { get; }

    /// <summary>The labels in drawing order: the primary series, then the secondary-Y series. Empty when the axes'
    /// renderer draws no end labels (polar, 3D: there the name stays in the legend).</summary>
    internal IReadOnlyList<DirectLabel> Labels { get; }

    /// <summary>The size of <paramref name="text"/> in <see cref="Font"/>; a label with math markup is measured as the
    /// rich text it is drawn as.</summary>
    /// <param name="text">The label text.</param>
    /// <returns>The measured size in pixels.</returns>
    internal Size Measure(string text) => MathTextParser.ContainsMath(text)
        ? _ctx.MeasureRichText(MathTextParser.Parse(text), Font)
        : _ctx.MeasureText(text, Font);

    /// <summary>The width of the widest label; 0 when there is none.</summary>
    /// <returns>The width in pixels.</returns>
    internal double WidestLabel()
    {
        double widest = 0;
        foreach (var label in Labels)
            widest = Math.Max(widest, Measure(label.Text).Width);
        return widest;
    }

    private static List<DirectLabel> Collect(Axes axes, Theme theme, IRenderContext ctx, MinMaxRange visibleX)
    {
        var labels = new List<DirectLabel>();
        var range = new MinMaxRange(Math.Min(visibleX.Min, visibleX.Max), Math.Max(visibleX.Min, visibleX.Max));
        Collect(axes.Series, SeriesAxisPair.Primary, 0, axes.Legend.LegendValues, range, labels);
        // Every right-side slot's series, in slot order, numbered as ONE running index across every slot — the same
        // number SlotIndex(SecondaryY, i) expects (Axes.AllSeries' order), never reset per slot.
        int indexInPair = 0;
        for (int slot = 0; slot < axes.RightYAxes.Count; slot++)
        {
            Collect(axes.RightYAxes[slot].Series, SeriesAxisPair.SecondaryY, slot, axes.Legend.LegendValues, range, labels, indexInPair);
            indexInPair += axes.RightYAxes[slot].Series.Count;
        }
        // Asked only when some series wants a label: whether one is drawn is the answer of the renderer the factory
        // builds for these axes, the rule the legend applies when it leaves such a series out.
        if (labels.Count > 0 && !AxesRenderer.Create(axes, default, ctx, theme).DrawsDirectLabels) labels.Clear();
        return labels;
    }

    private static void Collect(IReadOnlyList<ISeries> series, SeriesAxisPair pair, int axisSlot, bool legendValues,
                                MinMaxRange visibleX, List<DirectLabel> labels, int startingIndexInPair = 0)
    {
        for (int i = 0; i < series.Count; i++)
        {
            if (series[i] is not IHasLabelPlacement { LabelPlacement: SeriesLabelPlacement.LineEnd } placed) continue;
            var one = series[i];
            if (!one.Visible || string.IsNullOrEmpty(one.Label)) continue;
            var points = placed.LinePoints();
            int end = points.XData.LastDrawableIndex(points.YData, visibleX.Min, visibleX.Max);
            if (end < 0) continue;
            double x = points.XData[end], y = points.YData[end];
            labels.Add(new DirectLabel(one, pair, startingIndexInPair + i, axisSlot,
                                       legendValues ? AxesRenderer.WithLegendValue(one.Label, y) : one.Label, x, y));
        }
    }
}
