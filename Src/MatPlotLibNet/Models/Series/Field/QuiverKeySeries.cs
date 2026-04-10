// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a quiver key (reference arrow) series showing the scale of a quiver field.</summary>
public sealed class QuiverKeySeries : ChartSeries
{
    /// <summary>Gets or sets the X position in axes fraction (0–1).</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the Y position in axes fraction (0–1).</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the reference arrow length in data units.</summary>
    public double U { get; set; }

    /// <summary>Gets or sets the label displayed next to the reference arrow.</summary>
    public new string Label { get; set; }

    /// <summary>Gets or sets the font size of the label in points.</summary>
    public double FontSize { get; set; } = 12;

    /// <summary>Gets or sets the color. If <see langword="null"/>, the current cycle color is used.</summary>
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
