// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering;

/// <summary>Adapts an <see cref="Axes"/> instance to the <see cref="IAxesContext"/> interface.</summary>
internal sealed class AxesContextAdapter : IAxesContext
{
    private readonly Axes _axes;

    public AxesContextAdapter(Axes axes) => _axes = axes;

    public double? XAxisMin => _axes.XAxis.Min;
    public double? XAxisMax => _axes.XAxis.Max;
    public double? YAxisMin => _axes.YAxis.Min;
    public double? YAxisMax => _axes.YAxis.Max;
    public BarMode BarMode => _axes.BarMode;
    public IReadOnlyList<ISeries> AllSeries => _axes.Series;
}
