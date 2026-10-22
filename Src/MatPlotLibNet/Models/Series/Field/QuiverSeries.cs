// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a vector field (quiver) series with arrows at each grid point.</summary>
public sealed class QuiverSeries : ChartSeries, IHasColor
{
    public double[] XData { get; }

    public double[] YData { get; }

    public double[] UData { get; }

    public double[] VData { get; }

    public Color? Color { get; set; }

    public double Scale { get; set; } = 1.0;

    public double ArrowHeadSize { get; set; } = 0.3;

    /// <summary>Creates a new quiver series from the given position and vector data.</summary>
    public QuiverSeries(double[] xData, double[] yData, double[] uData, double[] vData)
    {
        XData = xData;
        YData = yData;
        UData = uData;
        VData = vData;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Over the arrows that can be drawn, tail and head: an arrow with a coordinate that is missing (NaN) or infinite
        // is not drawn, so it is left out of the extent on both axes.
        var x = Range1D.Empty;
        var y = Range1D.Empty;
        for (int i = 0; i < XData.Length; i++)
        {
            var tail = new Point(XData[i], YData[i]);
            var head = new Point(tail.X + UData[i] * Scale, tail.Y + VData[i] * Scale);
            if (!tail.CanDrawLineTo(head)) continue;
            x = x.Including(tail.X).Including(head.X);
            y = y.Including(tail.Y).Including(head.Y);
        }
        return new(x.OrNone()?.Lo, x.OrNone()?.Hi, y.OrNone()?.Lo, y.OrNone()?.Hi);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "quiver",
        XData = XData, YData = YData,
        UData = UData, VData = VData,
        Color = Color, Scale = Scale, ArrowHeadSize = ArrowHeadSize
    };

    /// <summary>Reconstructs a <see cref="QuiverSeries"/> from its serialization DTO, including scale and arrowhead size, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static QuiverSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Quiver(dto.XData ?? [], dto.YData ?? [], dto.UData ?? [], dto.VData ?? []);
        s.Color = dto.Color;
        if (dto.Scale.HasValue)
        {
            s.Scale = dto.Scale.Value;
        }
        if (dto.ArrowHeadSize.HasValue)
        {
            s.ArrowHeadSize = dto.ArrowHeadSize.Value;
        }
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Where each arrow sits and what it points at.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.FromNumberColumns(
            [new("x", Axis: DataAxis.X), new("y", Axis: DataAxis.Y), new("u"), new("v")], [XData, YData, UData, VData]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
