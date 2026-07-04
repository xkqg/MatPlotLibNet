// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a kernel density estimation (KDE) series that draws a smooth density curve for a data sample.</summary>
public sealed class KdeSeries : ChartSeries, IHasColor, IHasAlpha
{
    public double[] Data { get; }

    public double? Bandwidth { get; set; }

    public bool Fill { get; set; } = true;

    public double Alpha { get; set; } = 0.3;

    public double LineWidth { get; set; } = 1.5;

    public Color? Color { get; set; }

    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Initializes a new instance of <see cref="KdeSeries"/> with the specified data.</summary>
    /// <param name="data">The data values used to estimate the density.</param>
    public KdeSeries(double[] data)
    {
        Data = data;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Data.Length == 0) return new(0, 1, 0, 1);
        double min = Data.Min(), max = Data.Max();
        double range = max - min;
        if (range == 0) range = 1.0;
        double padding = range * 0.3;
        // Evaluate the actual KDE to find the density peak so the Y axis covers the full curve.
        // Without this, reporting yMax=null leaves the aggregator's yMax at double.MinValue and
        // the 5 % margin pass produces axis labels around ±1e307.
        var sorted = Data.OrderBy(v => v).ToArray();
        double bw = Bandwidth ?? GaussianKde.SilvermanBandwidth(sorted);
        var (_, density) = GaussianKde.Evaluate(sorted, bw);
        double yMax = density.Length > 0 ? density.Max() * 1.05 : 1.0;
        return new(min - padding, max + padding, 0, yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "kde",
        Data = Data,
        Bandwidth = Bandwidth,
        Alpha = Alpha,
        LineWidth = LineWidth,
        Color = Color,
        LineStyle = LineStyle == LineStyle.Solid ? null : LineStyle.ToString().ToLowerInvariant()
    };

    /// <summary>Reconstructs a <see cref="KdeSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static KdeSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Kde(dto.Data ?? []);
        if (dto.Bandwidth.HasValue)
        {
            s.Bandwidth = dto.Bandwidth.Value;
        }
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        if (dto.LineWidth.HasValue)
        {
            s.LineWidth = dto.LineWidth.Value;
        }
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        if (dto.LineStyle is not null && Enum.TryParse<Styling.LineStyle>(dto.LineStyle, true, out var ls))
        {
            s.LineStyle = ls;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
