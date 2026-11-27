// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Fluent builder for ONE Y axis of a subplot and the series plotted against it, on either side. The callback of
/// <see cref="AxesBuilderLinkedAxesVerbs.WithAddRightYAxis"/> and <see cref="AxesBuilderLinkedAxesVerbs.WithAddLeftYAxis"/> receives one for the axis
/// it just stacked, and the builder's own Y verbs (<see cref="AxesBuilderLabelVerbs.extension(AxesBuilder).SetYLabel(string)"/>,
/// <see cref="AxesBuilderLabelVerbs.SetYAxisColor"/>, <see cref="AxesBuilderLimitVerbs.SetYLim"/>) are this builder over the primary Y axis,
/// which is slot 0 of the left side. One set of verbs configures every Y axis, so a setting made on the primary is made
/// the same way on any other.</summary>
/// <remarks>Every verb acts on this builder's own axis and no other. The series a verb adds are plotted against this
/// axis' own range, not the primary's.</remarks>
/// <example>
/// <code>
/// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
///     .Plot(days, price).SetYLabel("Price")
///     .WithAddRightYAxis(rate => rate
///         .SetYLabel("Rate (%)").SetYAxisColor(Colors.Red)
///         .Plot(days, rates)));
/// </code>
/// </example>
public sealed class YAxisSlotBuilder
{
    private readonly AxisVerbs _verbs;

    /// <summary>Initializes a builder over <paramref name="slot"/> of <paramref name="axes"/>.</summary>
    /// <param name="axes">The axes the slot belongs to; its spines are the ones slot 0 of a side colours.</param>
    /// <param name="slot">The slot this builder configures.</param>
    internal YAxisSlotBuilder(Axes axes, YAxisSlot slot)
    {
        _verbs = AxisVerbs.Of(axes, slot);
    }

    /// <summary>Sets the label displayed alongside this axis, with an optional text style override.</summary>
    /// <param name="label">The axis label text.</param>
    /// <param name="configure">Function that receives the current <see cref="TextStyle"/> of the label and returns a
    /// modified copy; <see langword="null"/> leaves the style unchanged.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <example><code>rate.SetYLabel("Rate (%)", style => style with { FontSize = 14 });</code></example>
    public YAxisSlotBuilder SetYLabel(string label, Func<TextStyle, TextStyle>? configure = null)
    {
        _verbs.Label(label, configure);
        return this;
    }

    /// <summary>Sets explicit min/max limits for this axis' data range.</summary>
    /// <param name="min">The lower limit.</param>
    /// <param name="max">The upper limit.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <example><code>rate.SetYLim(0, 100);</code></example>
    public YAxisSlotBuilder SetYLim(double min, double max)
    {
        _verbs.Limits(min, max);
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
        _verbs.Color(color);
        return this;
    }

    /// <summary>Links this axis to the primary Y axis, as matplotlib's <c>ax.secondary_yaxis('right', functions=(forward,
    /// inverse))</c> does: the axis shows the range the primary axis shows, each value written through
    /// <paramref name="transform"/>, so a tick for a value stands at the height where the primary axis has the value it maps back
    /// to. A linked axis has no range and no series of its own; its label, colour and ticks are set as on any axis.</summary>
    /// <param name="transform">From the primary axis' values to this axis' values: <see cref="AxisTransform.Linear"/>,
    /// <see cref="AxisTransform.Reciprocal"/>, <see cref="AxisTransform.Logarithm"/>, <see cref="AxisTransform.Power"/>, or
    /// <see cref="AxisTransform.Of"/> for a pair of functions of your own. Called again, the new transform replaces the
    /// first.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transform"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">This axis holds a series: a linked axis holds none of its own (the series verbs of a
    /// linked axis refuse the same way).</exception>
    /// <example>
    /// Fahrenheit on the right of a plot in Celsius:
    /// <code>
    /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
    ///     .Plot(hours, celsius).SetYLabel("°C")
    ///     .WithAddRightYAxis(fahrenheit => fahrenheit.SetYLabel("°F").Link(AxisTransform.Linear(1.8, 32))));
    /// </code>
    /// </example>
    public YAxisSlotBuilder Link(AxisTransform transform)
    {
        _verbs.Link(transform);
        return this;
    }

    /// <summary>Adds a line series plotted against this axis.</summary>
    /// <param name="x">The X values.</param>
    /// <param name="y">The Y values, in the units of this axis.</param>
    /// <param name="configure">Optional callback that configures the new series.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> differ in length, or this axis is
    /// linked (<see cref="Link"/>), which holds no series of its own.</exception>
    /// <example><code>rate.Plot(days, rates, line => line.Label = "Rate");</code></example>
    public YAxisSlotBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null)
    {
        _verbs.Seat(nameof(y), () => _verbs.Slot!.Plot(x, y), configure);
        return this;
    }

    /// <summary>Adds a scatter series plotted against this axis.</summary>
    /// <param name="x">The X values.</param>
    /// <param name="y">The Y values, in the units of this axis.</param>
    /// <param name="configure">Optional callback that configures the new series.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> differ in length, or this axis is
    /// linked (<see cref="Link"/>), which holds no series of its own.</exception>
    /// <example><code>rate.Scatter(days, rates, points => points.Label = "Samples");</code></example>
    public YAxisSlotBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null)
    {
        _verbs.Seat(nameof(y), () => _verbs.Slot!.Scatter(x, y), configure);
        return this;
    }

    /// <summary>Adds a STREAMING line series against this axis, so a live chart can carry another unit at once.</summary>
    /// <param name="capacity">Maximum data points retained in the ring buffer. Default <see cref="StreamingSeries.RestingCapacity"/>.</param>
    /// <param name="configure">Optional callback that configures the new series.</param>
    /// <returns>The created series, so the caller can <c>AppendPoint</c> to it from any thread. Unlike the other verbs
    /// it does NOT return the builder, because the series IS the handle live data needs (the same shape
    /// <see cref="AxesBuilderStreamingVerbs.StreamingPlot"/> has on the primary axis).</returns>
    /// <exception cref="ArgumentException">This axis is linked (<see cref="Link"/>), which holds no series of its own; the
    /// refusal names <paramref name="capacity"/>, the size of the series it would have made.</exception>
    /// <example><code>var latency = rate.StreamingPlot(capacity: 600, line => line.Label = "Latency");</code></example>
    public StreamingLineSeries StreamingPlot(int capacity = StreamingSeries.RestingCapacity, Action<StreamingLineSeries>? configure = null)
    {
        return _verbs.Seat(nameof(capacity), () => _verbs.Slot!.AddSeries(new StreamingLineSeries(capacity)), configure);
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
        _verbs.Slot!.LabelLastAtLineEnd();
        return this;
    }
}
