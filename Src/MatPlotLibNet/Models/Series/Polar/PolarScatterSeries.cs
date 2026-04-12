// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a scatter plot in polar coordinates (r, theta).</summary>
public sealed class PolarScatterSeries : PolarSeries, IHasColor
{
    public Color? Color { get; set; }

    public double MarkerSize { get; set; } = 6;

    /// <summary>Initializes a new polar scatter series.</summary>
    public PolarScatterSeries(double[] r, double[] theta) : base(r, theta) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "polarscatter" };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
