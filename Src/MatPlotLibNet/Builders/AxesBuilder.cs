// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>Fluent builder for configuring an <see cref="Models.Axes"/> instance within a subplot. The class is the builder itself:
/// the axes it builds, adding a series made by hand, and building. Every verb (<c>ax.Plot</c>, <c>ax.SetXLabel</c>,
/// <c>ax.WithLegend</c>, ...) is an extension member in the class of its concern: the series verbs of a family beside the axes verbs
/// they forward to (<see cref="AxesBuilderXYVerbs"/> beside <see cref="AxesXYVerbs"/>), each setting of the axes in its own class
/// (<see cref="AxesBuilderLabelVerbs"/>, <see cref="AxesBuilderTickVerbs"/>, <see cref="AxesBuilderLegendVerbs"/>, ...), the indicator
/// verbs by what they draw, and the composed charts in their families (<see cref="XYAxesExtensions"/>, ...). They sit in the
/// namespace of the builder, so a call reads the same as a method of the class.</summary>
/// <example>
/// Dual-axis chart: candlestick + Bollinger bands on the left, the traded volume against its own Y axis on the right:
/// <code>
/// string svg = Plt.Create()
///     .AddSubPlot(1, 1, 1, ax =>
///     {
///         ax.Candlestick(open, high, low, close)
///           .BollingerBands(20, 2.0)
///           .AxHLine(support, r => r.Color = Colors.Red)
///           .SetYLabel("Price")
///           .WithAddRightYAxis(volumeAxis =>
///               volumeAxis.SetYLabel("Volume").Plot(days, volume));
///     })
///     .ToSvg();
/// </code>
/// </example>
public sealed class AxesBuilder
{
    private readonly Axes _axes;

    /// <summary>Creates a builder over a new, empty <see cref="Models.Axes"/>.</summary>
    public AxesBuilder() : this(new Axes()) { }

    /// <summary>Creates a builder that configures an axes that already exists, in place: an inset is configured where it
    /// stands, so nothing configured through the builder has to be carried across to it afterwards.</summary>
    internal AxesBuilder(Axes axes) => _axes = axes;

    /// <summary>The axes this builder configures: the one thing a builder holds, and what every verb acts on.</summary>
    internal Axes Axes => _axes;

    /// <summary>The verbs of the primary X axis.</summary>
    internal AxisVerbs X => AxisVerbs.BottomOf(_axes);

    /// <summary>The verbs of the primary Y axis, slot 0 of the left side: the same verbs every Y axis of the subplot has.</summary>
    internal AxisVerbs Y => AxisVerbs.Of(_axes, _axes.YAxes.Left[0]);

    /// <summary>The verbs of the Z axis.</summary>
    internal AxisVerbs Z => AxisVerbs.DepthOf(_axes);

    /// <summary>The one tail of a verb whose axes made what it adds (a series, an overlay, a series added by hand): the caller's
    /// callback, when there is one, gets the new thing, and the builder is returned for chaining.</summary>
    /// <typeparam name="T">What the axes made.</typeparam>
    /// <param name="made">The new series or overlay, already on the axes.</param>
    /// <param name="configure">The caller's callback, or <see langword="null"/>.</param>
    /// <returns>This builder.</returns>
    internal AxesBuilder Configured<T>(T made, Action<T>? configure)
    {
        configure?.Invoke(made);
        return this;
    }

    /// <summary>Adds a pre-constructed series to this axes. Used by extension packages
    /// (e.g. <c>MatPlotLibNet.Geo</c>) that ship their own series types.</summary>
    /// <typeparam name="T">A type implementing <see cref="ISeries"/>.</typeparam>
    /// <param name="series">The series instance to add.</param>
    /// <param name="configure">Optional configuration callback applied after the series is added.</param>
    /// <returns>This <see cref="AxesBuilder"/> for fluent chaining.</returns>
    public AxesBuilder AddSeries<T>(T series, Action<T>? configure = null) where T : ISeries
        => Configured(_axes.AddSeries(series), configure);

    /// <summary>Builds and returns a new <see cref="Axes"/> at the specified grid position.</summary>
    /// <param name="rows">Total number of rows in the grid.</param>
    /// <param name="cols">Total number of columns in the grid.</param>
    /// <param name="index">Zero-based linear index of this subplot within the grid.</param>
    internal Axes Build(int rows, int cols, int index)
    {
        _axes.GridRows = rows;
        _axes.GridCols = cols;
        _axes.GridIndex = index;
        return _axes;
    }

    /// <summary>Builds and returns a new <see cref="Axes"/> at the specified grid position.</summary>
    /// <param name="position">The row/column span that this subplot occupies.</param>
    internal Axes Build(GridPosition position)
    {
        _axes.GridPosition = position;
        return _axes;
    }
}
