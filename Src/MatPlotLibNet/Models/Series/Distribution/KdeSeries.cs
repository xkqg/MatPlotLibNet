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
    /// <summary>The observations the density is estimated from; a missing value (NaN) or an infinity is not used.</summary>
    public double[] Data { get; }

    /// <summary>The width of the Gaussian kernel that smooths the curve, in data units. <see langword="null"/> (the
    /// default), or a value that is not a positive finite number, uses Silverman's rule of thumb.</summary>
    public double? Bandwidth { get; set; }

    /// <summary>When <see langword="true"/> (the default) the area under the curve is filled at <see cref="Alpha"/>;
    /// when <see langword="false"/> only the curve is drawn.</summary>
    public bool Fill { get; set; } = true;

    /// <inheritdoc />
    public double Alpha { get; set; } = 0.3;

    /// <summary>Width of the density curve, in pixels. Default 1.5.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <inheritdoc />
    public Color? Color { get; set; }

    /// <summary>The dash pattern of the density curve. Default <c>Solid</c>.</summary>
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

        // The range is the curve's own extent: the grid it is drawn on and the density's peak, so the axes cover the whole
        // curve and nothing past it. (Reporting no yMax would leave the aggregator's yMax at double.MinValue and the 5 %
        // margin pass would produce axis labels around ±1e307.)
        if (Curve() is not { } curve) return new(null, null, null, null);
        return new(curve.X[0], curve.X[^1], 0, curve.Y.Max() * 1.05);
    }

    /// <summary>The density curve: the grid it is drawn on, which reaches three bandwidths past the smallest and the largest
    /// observation, and the density at each point of it. The range of the axes and the renderer both read it, so the curve
    /// and the range are one extent. A bandwidth that is not a positive finite number is not used: Silverman's is.</summary>
    /// <returns>The curve; <see langword="null"/> when no observation can be drawn.</returns>
    internal XYCurve? Curve()
    {
        var sorted = SortedSample();
        if (sorted.Length == 0) return null;
        double bandwidth = Bandwidth is > 0 and < double.PositiveInfinity and { } given ? given : GaussianKde.SilvermanBandwidth(sorted);
        return GaussianKde.Evaluate(sorted, bandwidth);
    }

    /// <summary>The observations the density is estimated from, in ascending order: a missing value (NaN) and an infinity
    /// are not observations, so the curve is estimated from the others, as a box or a violin is drawn from them. The
    /// range and the renderer both estimate from this one sample.</summary>
    /// <returns>The drawable observations, sorted.</returns>
    internal double[] SortedSample() => [.. Data.DrawableValues().OrderBy(v => v)];

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "kde",
        Data = Data,
        Bandwidth = Bandwidth,
        Alpha = Alpha,
        LineWidth = LineWidth,
        Color = Color,
        LineStyle = LineStyle == LineStyle.Solid ? null : LineStyle.ToString().ToLowerInvariant(),
        Fill = Fill.OrNullAtRest(true),
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
        s.Fill = dto.Fill != false;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The samples the curve is estimated from.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new IReadOnlyList<DataCell>[Data.Length];
        for (int i = 0; i < Data.Length; i++)
        {
            rows[i] = [DataCell.FromNumber(i), DataCell.FromNumber(Data[i])];
        }

        return new ChartDataTable(null, [new("index"), new(Label ?? "value", Axis: DataAxis.X)], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
