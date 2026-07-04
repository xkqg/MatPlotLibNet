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
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        for (int i = 0; i < XData.Length; i++)
        {
            double x0 = XData[i], x1 = x0 + UData[i] * Scale;
            double y0 = YData[i], y1 = y0 + VData[i] * Scale;
            if (Math.Min(x0, x1) < xMin) xMin = Math.Min(x0, x1);
            if (Math.Max(x0, x1) > xMax) xMax = Math.Max(x0, x1);
            if (Math.Min(y0, y1) < yMin) yMin = Math.Min(y0, y1);
            if (Math.Max(y0, y1) > yMax) yMax = Math.Max(y0, y1);
        }
        return new(xMin, xMax, yMin, yMax);
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
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
