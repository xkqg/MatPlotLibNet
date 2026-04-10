// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a rug plot series that draws short tick marks along an axis to show the distribution of individual values.</summary>
public sealed class RugplotSeries : ChartSeries
{
    /// <summary>Gets the data values to display as rug ticks.</summary>
    public Vec Data { get; }

    /// <summary>Gets or sets the relative height of each rug tick as a fraction of the Y axis range.</summary>
    public double Height { get; set; } = 0.05;

    /// <summary>Gets or sets the opacity of the rug ticks (0.0 to 1.0).</summary>
    public double Alpha { get; set; } = 0.5;

    /// <summary>Gets or sets the width of each rug tick line in pixels.</summary>
    public double LineWidth { get; set; } = 1.0;

    /// <summary>Gets or sets the color of the rug ticks. If <see langword="null"/>, the current cycle color is used.</summary>
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
        return new(Data.Min(), Data.Max(), 0, null);
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
