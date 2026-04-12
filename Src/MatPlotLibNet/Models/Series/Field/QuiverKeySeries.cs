// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a quiver key (reference arrow) series showing the scale of a quiver field.</summary>
public sealed class QuiverKeySeries : ChartSeries, IHasColor
{
    public double X { get; set; }

    public double Y { get; set; }

    public double U { get; set; }

    public new string Label { get; set; }

    public double FontSize { get; set; } = 12;

    public Color? Color { get; set; }

    /// <summary>Initializes a new instance of <see cref="QuiverKeySeries"/>.</summary>
    public QuiverKeySeries(double x, double y, double u, string label)
    {
        X = x; Y = y; U = u; Label = label;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
        => new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "quiverkey",
        QuiverKeyX = X,
        QuiverKeyY = Y,
        QuiverKeyU = U,
        QuiverKeyLabel = Label,
        MarkerSize = FontSize
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
