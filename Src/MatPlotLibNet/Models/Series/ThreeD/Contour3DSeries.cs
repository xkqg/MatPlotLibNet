// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents 3D contour lines projected into three-dimensional space from grid data.</summary>
public sealed class Contour3DSeries : GridSeries3D, IColormappable, IHasColor
{
    public int Levels { get; set; } = 10;

    public IColorMap? ColorMap { get; set; }

    public double LineWidth { get; set; } = 1.0;

    public Color? Color { get; set; }

    /// <summary>Initializes a new 3D contour series with the specified grid data.</summary>
    public Contour3DSeries(double[] x, double[] y, double[,] z) : base(x, y, z) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "contour3d",
        XData = X,
        YData = Y,
        ZGridData = ZToListList(),
        Color = Color,
        Levels = Levels != 10 ? Levels : null,
        LineWidth = LineWidth != 1.0 ? LineWidth : null,
        Label = Label
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
