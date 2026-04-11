// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a bar chart in polar coordinates with wedge-shaped bars.</summary>
public sealed class PolarBarSeries : PolarSeries
{
    public double BarWidth { get; set; } = 0.3;

    public Color? Color { get; set; }

    public double Alpha { get; set; } = 0.8;

    /// <summary>Initializes a new polar bar series.</summary>
    public PolarBarSeries(double[] r, double[] theta) : base(r, theta) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "polarbar" };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
