// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering;

/// <summary>Adapts an <see cref="Axes"/> instance to the <see cref="IAxesContext"/> interface.</summary>
internal sealed class AxesContextAdapter : IAxesContext
{
    private readonly Axes _axes;

    /// <summary>Initializes a new adapter wrapping the given <see cref="Axes"/> instance.</summary>
    /// <param name="axes">The subplot axes to expose through the <see cref="IAxesContext"/> interface.</param>
    public AxesContextAdapter(Axes axes) => _axes = axes;

    /// <inheritdoc />
    public double? XAxisMin => _axes.XAxis.Min;
    /// <inheritdoc />
    public double? XAxisMax => _axes.XAxis.Max;
    /// <inheritdoc />
    public double? YAxisMin => _axes.YAxis.Min;
    /// <inheritdoc />
    public double? YAxisMax => _axes.YAxis.Max;
    /// <inheritdoc />
    public BarMode BarMode => _axes.BarMode;
    /// <inheritdoc />
    public IReadOnlyList<ISeries> AllSeries => _axes.Series;
}
