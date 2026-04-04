// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a scatter chart series displaying individual data points as markers.</summary>
public sealed class ScatterSeries : ChartSeries
{
    /// <summary>Gets the X-axis data values.</summary>
    public double[] XData { get; }

    /// <summary>Gets the Y-axis data values.</summary>
    public double[] YData { get; }

    /// <summary>Gets or sets per-point marker sizes.</summary>
    public double[]? Sizes { get; set; }

    /// <summary>Gets or sets per-point marker colors.</summary>
    public Color[]? Colors { get; set; }

    /// <summary>Gets or sets the uniform marker color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the marker style for the data points.</summary>
    public MarkerStyle Marker { get; set; } = MarkerStyle.Circle;

    /// <summary>Gets or sets the default marker size in points squared.</summary>
    public double MarkerSize { get; set; } = 36;

    /// <summary>Gets or sets the opacity of the markers (0.0 to 1.0).</summary>
    public double Alpha { get; set; } = 1.0;

    /// <summary>Gets or sets the color map used for mapping scalar data to colors.</summary>
    public IColorMap? ColorMap { get; set; }


    /// <summary>Initializes a new instance of <see cref="ScatterSeries"/> with the specified data.</summary>
    /// <param name="xData">The X-axis data values.</param>
    /// <param name="yData">The Y-axis data values.</param>
    public ScatterSeries(double[] xData, double[] yData)
    {
        XData = xData;
        YData = yData;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
