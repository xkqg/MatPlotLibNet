// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a contour series on an unstructured triangular mesh.</summary>
public sealed class TricontourSeries : ChartSeries, IColormappable, INormalizable
{
    /// <summary>Gets the X coordinates of the unstructured data points.</summary>
    public Vec X { get; }

    /// <summary>Gets the Y coordinates of the unstructured data points.</summary>
    public Vec Y { get; }

    /// <summary>Gets the Z values at each data point.</summary>
    public Vec Z { get; }

    /// <summary>Gets or sets the number of contour levels.</summary>
    public int Levels { get; set; } = 10;

    /// <summary>Gets or sets the color map used to color contour levels.</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Gets or sets the normalizer for contour value mapping.</summary>
    public INormalizer? Normalizer { get; set; }

    /// <summary>Initializes a new instance of <see cref="TricontourSeries"/>.</summary>
    public TricontourSeries(Vec x, Vec y, Vec z)
    {
        X = x; Y = y; Z = z;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0) return new(0, 1, 0, 1);
        return new(X.Min(), X.Max(), Y.Min(), Y.Max());
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "tricontour",
        XData = X,
        YData = Y,
        ZData = Z,
        Levels = Levels,
        ColorMapName = ColorMap?.Name
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
