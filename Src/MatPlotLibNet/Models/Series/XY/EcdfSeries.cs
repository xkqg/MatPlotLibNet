// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an empirical cumulative distribution function (ECDF) step-function series.</summary>
public sealed class EcdfSeries : ChartSeries
{
    /// <summary>Gets the raw input data.</summary>
    public double[] Data { get; }

    /// <summary>Gets the sorted data values (computed from <see cref="Data"/>).</summary>
    public double[] SortedX { get; }

    /// <summary>Gets the CDF values (1/n, 2/n, ..., 1.0), computed from <see cref="Data"/>.</summary>
    public double[] CdfY { get; }

    /// <summary>Gets or sets the line color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the line width.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Gets or sets the line style.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Creates a new ECDF series from the given raw data.</summary>
    /// <param name="data">The raw data values to compute the ECDF from.</param>
    public EcdfSeries(double[] data)
    {
        Data = data;
        SortedX = [.. data.OrderBy(v => v)];
        int n = SortedX.Length;
        CdfY = new double[n];
        for (int i = 0; i < n; i++)
            CdfY[i] = (i + 1.0) / n;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        SortedX.Length > 0
            ? new(SortedX.Min(), SortedX.Max(), 0, 1)
            : new(0, 1, 0, 1);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "ecdf",
        Data = Data,
        Color = Color,
        LineWidth = LineWidth,
        LineStyle = LineStyle.ToString().ToLowerInvariant()
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
