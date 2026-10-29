// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Fluent builder for ONE Y axis of a subplot and the series plotted against it, on either side. The callback of
/// <see cref="AxesBuilder.WithAddRightYAxis"/> and <see cref="AxesBuilder.WithAddLeftYAxis"/> receives one for the axis
/// it just stacked, and the builder's own Y verbs (<see cref="AxesBuilder.SetYLabel(string)"/>,
/// <see cref="AxesBuilder.SetYAxisColor"/>, <see cref="AxesBuilder.SetYLim"/>) are this builder over the primary Y axis,
/// which is slot 0 of the left side. One set of verbs configures every Y axis, so a setting made on the primary is made
/// the same way on any other.</summary>
/// <remarks>Every verb acts on this builder's own axis and no other. The series a verb adds are plotted against this
/// axis' own range, not the primary's.</remarks>
/// <example>
/// <code>
/// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
///     .Plot(days, price).SetYLabel("Price")
///     .WithAddRightYAxis(rate => rate
///         .SetYLabel("Rate (%)").SetYAxisColor(Color.Red)
///         .Plot(days, rates)));
/// </code>
/// </example>
public sealed class YAxisSlotBuilder
{
    private readonly Axes _axes;
    private readonly YAxisSlot _slot;

    /// <summary>Initializes a builder over <paramref name="slot"/> of <paramref name="axes"/>.</summary>
    /// <param name="axes">The axes the slot belongs to; its spines are the ones slot 0 of a side colours.</param>
    /// <param name="slot">The slot this builder configures.</param>
    internal YAxisSlotBuilder(Axes axes, YAxisSlot slot)
    {
        _axes = axes;
        _slot = slot;
    }

    /// <summary>The edge whose spine this axis stands on: the left or the right edge for slot 0 of its side, and
    /// <see langword="null"/> for a slot stacked past it, which draws ticks, tick labels and a title but no spine of its
    /// own.</summary>
    private SpineEdge? Spine =>
        !ReferenceEquals(_axes.YAxes.StackOn(_slot.Side)[0], _slot) ? null
        : _slot.Side == AxisSide.Left ? SpineEdge.Left : SpineEdge.Right;

    /// <summary>Sets the label displayed alongside this axis, with an optional text style override.</summary>
    /// <param name="label">The axis label text.</param>
    /// <param name="configure">Function that receives the current <see cref="TextStyle"/> of the label and returns a
    /// modified copy; <see langword="null"/> leaves the style unchanged.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <example><code>rate.SetYLabel("Rate (%)", style => style with { FontSize = 14 });</code></example>
    public YAxisSlotBuilder SetYLabel(string label, Func<TextStyle, TextStyle>? configure = null)
    {
        _slot.Axis.Label = label;
        if (configure is not null) _slot.Axis.LabelStyle = configure(_slot.Axis.LabelStyle ?? new TextStyle());
        return this;
    }

    /// <summary>Sets explicit min/max limits for this axis' data range.</summary>
    /// <param name="min">The lower limit.</param>
    /// <param name="max">The upper limit.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <example><code>rate.SetYLim(0, 100);</code></example>
    public YAxisSlotBuilder SetYLim(double min, double max)
    {
        _slot.Axis.Min = min;
        _slot.Axis.Max = max;
        return this;
    }

    /// <summary>Colours this axis in one call: its tick marks, its tick labels and its title, so the axis reads as
    /// belonging to the series drawn in that colour. Slot 0 of a side also colours that side's spine (the primary Y axis
    /// the left spine, the first right axis the right spine); an axis stacked past it draws no spine of its own, so it
    /// takes the other three. Keep <see cref="SetYLabel"/> naming the series and its unit, so the colour is never the
    /// only thing that says which series the axis belongs to.</summary>
    /// <param name="color">The colour all of them take.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <remarks>Everything else the axis carries (tick length, label rotation, font size, spine width) is kept.</remarks>
    /// <example><code>rate.SetYAxisColor(Color.FromHex("#D62728"));</code></example>
    public YAxisSlotBuilder SetYAxisColor(Color color)
    {
        _axes.ApplyAxisColor(_slot.Axis, Spine, color);
        return this;
    }

    /// <summary>Adds a line series plotted against this axis.</summary>
    /// <param name="x">The X values.</param>
    /// <param name="y">The Y values, in the units of this axis.</param>
    /// <param name="configure">Optional callback that configures the new series.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> differ in length.</exception>
    /// <example><code>rate.Plot(days, rates, line => line.Label = "Rate");</code></example>
    public YAxisSlotBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null)
    {
        x.Length.ThrowIfMisaligned(y.Length);
        var series = _slot.AddSeries(new LineSeries(x, y));
        configure?.Invoke(series);
        return this;
    }

    /// <summary>Adds a scatter series plotted against this axis.</summary>
    /// <param name="x">The X values.</param>
    /// <param name="y">The Y values, in the units of this axis.</param>
    /// <param name="configure">Optional callback that configures the new series.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> differ in length.</exception>
    /// <example><code>rate.Scatter(days, rates, points => points.Label = "Samples");</code></example>
    public YAxisSlotBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null)
    {
        x.Length.ThrowIfMisaligned(y.Length);
        var series = _slot.AddSeries(new ScatterSeries(x, y));
        configure?.Invoke(series);
        return this;
    }

    /// <summary>Adds a STREAMING line series against this axis, so a live chart can carry another unit at once.</summary>
    /// <param name="capacity">Maximum data points retained in the ring buffer. Default 10,000.</param>
    /// <param name="configure">Optional callback that configures the new series.</param>
    /// <returns>The created series, so the caller can <c>AppendPoint</c> to it from any thread. Unlike the other verbs
    /// it does NOT return the builder, because the series IS the handle live data needs (the same shape
    /// <see cref="AxesBuilder.StreamingPlot"/> has on the primary axis).</returns>
    /// <example><code>var latency = rate.StreamingPlot(capacity: 600, line => line.Label = "Latency");</code></example>
    public StreamingLineSeries StreamingPlot(int capacity = 10_000, Action<StreamingLineSeries>? configure = null)
    {
        var series = _slot.AddSeries(new StreamingLineSeries(capacity));
        configure?.Invoke(series);
        return series;
    }

    /// <summary>Draws the name of the series added last to this axis at the end of its line, in the line's colour,
    /// instead of in the legend. Applies to a line or a streaming line (<see cref="IHasLabelPlacement"/>) that carries a
    /// label; when the last series cannot carry a placement, or there is none, nothing changes.</summary>
    /// <returns>This builder, for chaining.</returns>
    /// <remarks>The end label is drawn for an axis on either side: a line ends at the right of the plot whatever axis
    /// measures it, so its label stands in the one column of end labels right of the plot (past the right axes), at the
    /// height this axis puts the end of the line at.</remarks>
    /// <example><code>rate.Plot(days, rates, line => line.Label = "Rate").WithDirectLabel();</code></example>
    public YAxisSlotBuilder WithDirectLabel()
    {
        if (_slot.Series.LastOrDefault() is IHasLabelPlacement placed) placed.LabelPlacement = SeriesLabelPlacement.LineEnd;
        return this;
    }
}
