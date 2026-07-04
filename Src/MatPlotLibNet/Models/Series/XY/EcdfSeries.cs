// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an empirical cumulative distribution function (ECDF) step-function series.</summary>
public sealed class EcdfSeries : ChartSeries, IHasColor
{
    public double[] Data { get; }

    public double[] SortedX { get; }

    public double[] CdfY { get; }

    public Color? Color { get; set; }

    public double LineWidth { get; set; } = 1.5;

    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Creates a new ECDF series from the given raw data.</summary>
    /// <param name="data">The raw data values to compute the ECDF from.</param>
    public EcdfSeries(double[] data)
    {
        Data = data;
        SortedX = [.. data.OrderBy(v => v)];
        int n = SortedX.Length;
        CdfY = new double[n];
        for (int i = 0; i < n; i++)
            CdfY[i] = (i + 1.0) / n;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        SortedX.Length > 0
            ? new(SortedX.Min(), SortedX.Max(), 0, 1)
            : new(0, 1, 0, 1);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "ecdf",
        Data = Data,
        Color = Color,
        LineWidth = LineWidth,
        LineStyle = LineStyle.ToString().ToLowerInvariant()
    };

    /// <summary>Reconstructs an <see cref="EcdfSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static EcdfSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Ecdf(dto.Data ?? []);
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        if (dto.LineWidth.HasValue)
        {
            s.LineWidth = dto.LineWidth.Value;
        }
        if (dto.LineStyle is not null && Enum.TryParse<LineStyle>(dto.LineStyle, true, out var ls))
        {
            s.LineStyle = ls;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
