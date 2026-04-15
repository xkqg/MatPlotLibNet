// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a rug plot series that draws short tick marks along an axis to show the distribution of individual values.</summary>
public sealed class RugplotSeries : ChartSeries, IHasColor, IHasAlpha
{
    public Vec Data { get; }

    public double Height { get; set; } = 0.05;

    public double Alpha { get; set; } = 0.5;

    public double LineWidth { get; set; } = 1.0;

    public Color? Color { get; set; }

    /// <summary>Initializes a new instance of <see cref="RugplotSeries"/> with the specified data.</summary>
    /// <param name="data">The data values to display as rug ticks.</param>
    public RugplotSeries(Vec data)
    {
        Data = data;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Data.Length == 0) return new(0, 1, 0, 1);
        // Rugs are always drawn within [0, Height] on the Y axis — that's the extent a standalone
        // rugplot needs. When overlaid on a KDE/hist the axis scaler will widen from there.
        return new(Data.Min(), Data.Max(), 0, Math.Max(Height, 1.0));
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "rugplot",
        Data = Data,
        Alpha = Alpha,
        LineWidth = LineWidth,
        Color = Color,
        RugHeight = Height
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
