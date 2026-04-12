// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a streamline plot through a 2D vector field (U, V components on a grid).</summary>
public sealed class StreamplotSeries : ChartSeries, IHasColor
{
    public double[] X { get; }

    public double[] Y { get; }

    public double[,] U { get; }

    public double[,] V { get; }

    public double Density { get; set; } = 1.0;

    public Color? Color { get; set; }

    public double LineWidth { get; set; } = 1.0;

    public double ArrowSize { get; set; } = 1.0;

    /// <summary>Creates a new streamplot series from grid coordinates and velocity field components.</summary>
    /// <param name="x">1D array of X grid coordinates.</param>
    /// <param name="y">1D array of Y grid coordinates.</param>
    /// <param name="u">2D array of X-velocity components (rows=Y, cols=X).</param>
    /// <param name="v">2D array of Y-velocity components (rows=Y, cols=X).</param>
    public StreamplotSeries(double[] x, double[] y, double[,] u, double[,] v)
    {
        X = x;
        Y = y;
        U = u;
        V = v;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(X.Min(), X.Max(), Y.Min(), Y.Max());

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "streamplot",
        XData = X,
        YData = Y,
        HeatmapData = ChartSerializer.To2DList(U),
        VFieldData = ChartSerializer.To2DList(V),
        LineWidth = LineWidth,
        Color = Color
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
