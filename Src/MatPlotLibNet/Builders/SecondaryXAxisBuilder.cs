// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Fluent builder for configuring a secondary X-axis (top edge) and adding series that scale against it.</summary>
/// <remarks>Obtained via <see cref="AxesBuilderLinkedAxesVerbs.WithSecondaryXAxis"/>. The secondary axis renders ticks and
/// labels on the top of the plot and uses an independent X-axis data range, or, linked to the bottom axis
/// (<see cref="Link"/>), shows the bottom axis' range in other units.</remarks>
public sealed class SecondaryXAxisBuilder
{
    private readonly Axes _axes;
    private readonly AxisVerbs _top;

    /// <summary>Initializes a new <see cref="SecondaryXAxisBuilder"/> attached to the given axes and to its top X axis,
    /// which it creates when the axes have none yet. The top axis of an axes never changes once it exists, so the builder
    /// holds its verbs from the start.</summary>
    internal SecondaryXAxisBuilder(Axes axes)
    {
        _axes = axes;
        _top = AxisVerbs.TopOf(axes);
    }

    /// <summary>Sets the label displayed above the top-edge X-axis.</summary>
    public SecondaryXAxisBuilder SetXLabel(string label) { _top.Label(label); return this; }

    /// <summary>Colours the top X axis in one call: its spine, its tick marks, its tick labels and its title.</summary>
    /// <param name="color">The colour all four take.</param>
    /// <returns>This builder, for chaining.</returns>
    public SecondaryXAxisBuilder SetXAxisColor(Color color)
    {
        _top.Color(color);
        return this;
    }

    /// <summary>Sets explicit min/max limits for the secondary X-axis data range.</summary>
    public SecondaryXAxisBuilder SetXLim(double min, double max) { _top.Limits(min, max); return this; }

    /// <summary>Links the top X axis to the primary X axis, as matplotlib's <c>ax.secondary_xaxis('top', functions=(forward,
    /// inverse))</c> does: the axis shows the range the bottom axis shows, each value written through
    /// <paramref name="transform"/>, so a tick for a value stands above the place where the bottom axis has the value it maps
    /// back to. A linked axis has no range and no series of its own; its label, colour and ticks are set as on any axis.</summary>
    /// <param name="transform">From the bottom axis' values to the top axis' values: <see cref="AxisTransform.Linear"/>,
    /// <see cref="AxisTransform.Reciprocal"/>, <see cref="AxisTransform.Logarithm"/>, <see cref="AxisTransform.Power"/>, or
    /// <see cref="AxisTransform.Of"/> for a pair of functions of your own. Called again, the new transform replaces the
    /// first.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transform"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The top axis holds a series: a linked axis holds none of its own (the series verbs of a
    /// linked top axis refuse the same way).</exception>
    /// <example>
    /// The period along the top of a plot over frequency:
    /// <code>
    /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
    ///     .Plot(frequency, power).SetXLabel("frequency (Hz)")
    ///     .WithSecondaryXAxis(top => top.SetXLabel("period (s)").Link(AxisTransform.Reciprocal())));
    /// </code>
    /// </example>
    public SecondaryXAxisBuilder Link(AxisTransform transform)
    {
        _top.Link(transform);
        return this;
    }

    /// <summary>Adds a line series plotted against the secondary X-axis.</summary>
    /// <param name="x">The X values, in the units of the top axis.</param>
    /// <param name="y">The Y values.</param>
    /// <param name="configure">Optional callback that configures the new series.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">The top axis is linked (<see cref="Link"/>), which holds no series of its own, or
    /// <paramref name="x"/> and <paramref name="y"/> differ in length.</exception>
    public SecondaryXAxisBuilder PlotXSecondary(double[] x, double[] y, Action<LineSeries>? configure = null)
    {
        _top.Seat(nameof(x), () => _axes.PlotXSecondary(x, y), configure);
        return this;
    }

    /// <summary>Adds a scatter series plotted against the secondary X-axis.</summary>
    /// <param name="x">The X values, in the units of the top axis.</param>
    /// <param name="y">The Y values.</param>
    /// <param name="configure">Optional callback that configures the new series.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">The top axis is linked (<see cref="Link"/>), which holds no series of its own, or
    /// <paramref name="x"/> and <paramref name="y"/> differ in length.</exception>
    public SecondaryXAxisBuilder ScatterXSecondary(double[] x, double[] y, Action<ScatterSeries>? configure = null)
    {
        _top.Seat(nameof(x), () => _axes.ScatterXSecondary(x, y), configure);
        return this;
    }
}
