// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a quiver key (reference arrow) series showing the scale of a quiver field.</summary>
public sealed class QuiverKeySeries : ChartSeries, IHasColor
{
    /// <summary>Horizontal position of the key arrow, as a fraction of the width of the axes: 0 is the left edge and 1
    /// the right.</summary>
    public double X { get; set; }

    /// <summary>Vertical position of the key arrow, as a fraction of the height of the axes: 0 is the bottom edge and 1
    /// the top.</summary>
    public double Y { get; set; }

    /// <summary>Length of the key arrow in data units along X, so 1 is as long as one unit of the X axis.</summary>
    public double U { get; set; }

    /// <summary>The text printed above the arrow, naming what its length stands for, such as <c>"1 m/s"</c>.</summary>
    public new string Label { get; set; }

    /// <summary>Font size of the label, in points. Default 12.</summary>
    public double FontSize { get; set; } = 12;

    /// <inheritdoc />
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
        // The label's point size has always travelled in the marker size field: every document written so far carries it
        // there, and the golden corpus pins the bytes.
        MarkerSize = FontSize,
        Color = Color,
    };

    /// <summary>Reconstructs a <see cref="QuiverKeySeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static QuiverKeySeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.QuiverKey(dto.QuiverKeyX ?? 0.5, dto.QuiverKeyY ?? 0.9, dto.QuiverKeyU ?? 1.0, dto.QuiverKeyLabel ?? "");
        if (dto.MarkerSize.HasValue)
        {
            s.FontSize = dto.MarkerSize.Value;
        }
        s.Color = dto.Color;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
