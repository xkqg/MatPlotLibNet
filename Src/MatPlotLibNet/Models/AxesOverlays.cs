// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>The overlays of one <see cref="Axes"/>: what is drawn over its data besides the series (annotations, reference lines,
/// trendlines, price levels, Fibonacci retracements, shaded spans, buy and sell signals, and patches). It holds the eight lists,
/// makes the lines, spans, signals and annotations the axes' verbs and a threshold add, takes a drawing tool into the list the tool
/// names (<see cref="Add{TTool}"/>), keeps the place of each patch among the series, and it is the one place that knows the eight: for
/// the axes' change mark (<see cref="LatestChange"/>) and for the walk a kept draw compares (<see cref="Items"/>). The axes'
/// public members are forwards to it, so its callers see the lists they always saw.</summary>
internal sealed class AxesOverlays : IChangeTracked
{
    /// <summary>The text annotations.</summary>
    internal StampedList<Annotation> Annotations { get; } = new();

    /// <summary>The horizontal and vertical reference lines.</summary>
    internal StampedList<ReferenceLine> ReferenceLines { get; } = new();

    /// <summary>The trendlines, drawn between two points.</summary>
    internal StampedList<Trendline> Trendlines { get; } = new();

    /// <summary>The horizontal price levels.</summary>
    internal StampedList<HorizontalLevel> HorizontalLevels { get; } = new();

    /// <summary>The Fibonacci retracements.</summary>
    internal StampedList<FibonacciRetracement> FibonacciRetracements { get; } = new();

    /// <summary>The shaded spans.</summary>
    internal StampedList<SpanRegion> Spans { get; } = new();

    /// <summary>The buy and sell signal markers.</summary>
    internal StampedList<SignalMarker> Signals { get; } = new();

    /// <summary>The patches, in the order they were added.</summary>
    internal StampedList<Patch> Patches { get; } = new();

    /// <summary>Adds a text annotation at <paramref name="x"/>, <paramref name="y"/>.</summary>
    /// <returns>The annotation added.</returns>
    internal Annotation Annotate(string text, double x, double y) => Keep(Annotations, new Annotation(text, x, y));

    /// <summary>Adds a reference line at <paramref name="value"/>: constant Y when horizontal, constant X when vertical.</summary>
    /// <returns>The line added.</returns>
    internal ReferenceLine AddLine(double value, Orientation orientation) => Keep(ReferenceLines, new ReferenceLine(value, orientation));

    /// <summary>Adds a shaded span from <paramref name="min"/> to <paramref name="max"/>: across Y when horizontal, across X when
    /// vertical.</summary>
    /// <returns>The span added.</returns>
    internal SpanRegion AddSpan(double min, double max, Orientation orientation) => Keep(Spans, new SpanRegion(min, max, orientation));

    /// <summary>Adds a buy or sell signal marker at <paramref name="x"/>, <paramref name="y"/>.</summary>
    /// <returns>The marker added.</returns>
    internal SignalMarker AddSignal(double x, double y, SignalDirection direction) => Keep(Signals, new SignalMarker(x, y, direction));

    /// <summary>Adds a drawing tool to the list it names (<see cref="IDrawingTool.AddTo"/>): the one way a tool enters an axes, from
    /// the axes' verbs and from an event a viewer sent.</summary>
    /// <returns>The same tool.</returns>
    internal TTool Add<TTool>(TTool tool) where TTool : IDrawingTool
    {
        tool.AddTo(this);
        return tool;
    }

    /// <summary>Adds <paramref name="patch"/> after the first <paramref name="seriesBefore"/> series of the axes: the patch key
    /// (<see cref="SeriesBeforePatch"/>) is made with the first patch, as most axes have none.</summary>
    /// <returns>The same patch.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="patch"/> is null.</exception>
    internal TPatch AddPatch<TPatch>(TPatch patch, int seriesBefore) where TPatch : Patch
    {
        ArgumentNullException.ThrowIfNull(patch);
        Patches.Add(patch);
        (_seriesBeforePatch ??= []).Add(seriesBefore);
        return patch;
    }

    /// <summary>For the patch at each place of <see cref="Patches"/>, how many of the axes' own series had been added when it was: the
    /// one key that puts the rows of the legend in the order the series and the patches were added, as matplotlib lists them. A series
    /// is never taken out of an axes once a patch stands after it, so the key stays true.</summary>
    private List<int>? _seriesBeforePatch;

    /// <summary>How many of the axes' own series were added before the patch at <paramref name="patchIndex"/> of <see cref="Patches"/>,
    /// asked only of an axes that has that patch.</summary>
    internal int SeriesBeforePatch(int patchIndex) => _seriesBeforePatch![patchIndex];

    /// <summary>Places the patch added last after the first <paramref name="seriesBefore"/> of the <paramref name="seriesCount"/> series
    /// of the axes, as a figure read from JSON says, where every series is added before any patch. A number past the series places it
    /// after them all.</summary>
    internal void PlaceLastPatchAfter(int seriesBefore, int seriesCount) =>
        _seriesBeforePatch![^1] = Math.Clamp(seriesBefore, 0, seriesCount);

    /// <summary>Adds a threshold: a dashed reference line at <paramref name="value"/>, a span from it to the far edge on the
    /// <paramref name="breach"/> side (±<see cref="double.MaxValue"/>, clipped to the plot when drawn), both in
    /// <paramref name="color"/> when one is given, and an annotation with <paramref name="label"/> at the value when one is
    /// given (on the Y axis' value at X = 0 for a horizontal threshold, on the X axis' value at Y = 0 for a vertical one).</summary>
    internal void AddThreshold(double value, Orientation orientation, ThresholdBreach breach, Color? color, string? label)
    {
        var line = AddLine(value, orientation);
        line.LineStyle = LineStyle.Dashed;
        if (color is not null) line.Color = color;

        bool above = breach == ThresholdBreach.Above;
        var span = AddSpan(above ? value : double.MinValue, above ? double.MaxValue : value, orientation);
        if (color is not null) span.Color = color;

        if (label is null) return;
        // At 0 along the line and the value across it.
        var at = Point.Along(orientation.RunsAlongX, 0, value);
        Annotate(label, at.X, at.Y);
    }

    /// <summary>The newest change to any of the eight lists or to anything in them.</summary>
    public long LatestChange
    {
        get
        {
            long latest = Annotations.LatestChange;
            latest = ChangeClock.Latest(latest, ReferenceLines.LatestChange);
            latest = ChangeClock.Latest(latest, Trendlines.LatestChange);
            latest = ChangeClock.Latest(latest, HorizontalLevels.LatestChange);
            latest = ChangeClock.Latest(latest, FibonacciRetracements.LatestChange);
            latest = ChangeClock.Latest(latest, Spans.LatestChange);
            latest = ChangeClock.Latest(latest, Signals.LatestChange);
            return ChangeClock.Latest(latest, Patches.LatestChange);
        }
    }

    /// <summary>Every overlay, list by list in the order above, each list in the order its items were added: the order a kept
    /// draw takes its copy of the axes' data in and compares it by.</summary>
    internal IEnumerable<object> Items()
    {
        foreach (var item in Annotations) yield return item;
        foreach (var item in ReferenceLines) yield return item;
        foreach (var item in Trendlines) yield return item;
        foreach (var item in HorizontalLevels) yield return item;
        foreach (var item in FibonacciRetracements) yield return item;
        foreach (var item in Spans) yield return item;
        foreach (var item in Signals) yield return item;
        foreach (var item in Patches) yield return item;
    }

    private static T Keep<T>(StampedList<T> list, T item)
    {
        list.Add(item);
        return item;
    }
}
