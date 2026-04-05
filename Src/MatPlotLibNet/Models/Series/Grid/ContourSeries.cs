// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a contour plot series displaying iso-lines or filled regions for 3D surface data.</summary>
public sealed class ContourSeries : ChartSeries
{
    /// <summary>Gets the X-axis grid coordinates.</summary>
    public double[] XData { get; }

    /// <summary>Gets the Y-axis grid coordinates.</summary>
    public double[] YData { get; }

    /// <summary>Gets the 2D matrix of Z values at each grid point.</summary>
    public double[,] ZData { get; }

    /// <summary>Gets or sets the number of contour levels to draw.</summary>
    public int Levels { get; set; } = 10;

    /// <summary>Gets or sets whether the contour regions are filled.</summary>
    public bool Filled { get; set; }

    /// <summary>Gets or sets the color map used to map contour levels to colors.</summary>
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
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
