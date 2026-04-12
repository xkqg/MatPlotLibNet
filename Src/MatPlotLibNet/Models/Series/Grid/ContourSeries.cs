// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a contour plot series displaying iso-lines or filled regions for 3D surface data.</summary>
public sealed class ContourSeries : ChartSeries, IColormappable, ILabelable
{
    public double[] XData { get; }

    public double[] YData { get; }

    public double[,] ZData { get; }

    public int Levels { get; set; } = 10;

    public double[]? LevelValues { get; set; }

    public bool Filled { get; set; }

    public bool ShowLabels { get; set; }

    public string? LabelFormat { get; set; }

    public double LabelFontSize { get; set; } = 10;

    public IColorMap? ColorMap { get; set; }


    /// <summary>Initializes a new instance of <see cref="ContourSeries"/> with the specified grid data.</summary>
    /// <param name="xData">The X-axis grid coordinates.</param>
    /// <param name="yData">The Y-axis grid coordinates.</param>
    /// <param name="zData">The 2D matrix of Z values at each grid point.</param>
    public ContourSeries(double[] xData, double[] yData, double[,] zData)
    {
        XData = xData;
        YData = yData;
        ZData = zData;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "contour",
        XData = XData, YData = YData, HeatmapData = ChartSerializer.To2DList(ZData)
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
