// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an error bar series showing uncertainty at each data point.</summary>
public sealed class ErrorBarSeries : XYSeries, IHasColor
{
    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.ErrorBar, LineWidth);

    public double[] YErrorLow { get; }

    public double[] YErrorHigh { get; }

    public double[]? XErrorLow { get; set; }

    public double[]? XErrorHigh { get; set; }

    public Color? Color { get; set; }

    public double LineWidth { get; set; } = 1.5;

    public double CapSize { get; set; } = 5.0;

    public double? ELineWidth { get; set; }

    public double? CapThick { get; set; }

    public int ErrorEvery { get; set; } = RestingErrorEvery;

    private const int RestingErrorEvery = 1;

    /// <summary>Creates a new error bar series from the given data and error magnitudes.</summary>
    public ErrorBarSeries(double[] xData, double[] yData, double[] yErrorLow, double[] yErrorHigh)
        : base(xData, yData)
    {
        YErrorLow = yErrorLow;
        YErrorHigh = yErrorHigh;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Over what can be drawn: a missing value (NaN) and an infinity are left out, as the renderer leaves out the
        // point or the bar. The range runs from the lowest bar end to the highest; along X from the lowest point or
        // left end to the highest point or right end.
        var low = new double[YData.Length];
        var high = new double[YData.Length];
        for (int i = 0; i < YData.Length; i++)
        {
            low[i] = YData[i] - YErrorLow[i];
            high[i] = YData[i] + YErrorHigh[i];
        }

        double[][] xLows = [XData], xHighs = [XData];
        if (XErrorLow is not null && XErrorHigh is not null)
        {
            var left = new double[XData.Length];
            var right = new double[XData.Length];
            for (int i = 0; i < XData.Length; i++)
            {
                left[i] = XData[i] - XErrorLow[i];
                right[i] = XData[i] + XErrorHigh[i];
            }
            xLows = [XData, left];
            xHighs = [XData, right];
        }

        return new(xLows.DrawableRange()?.Lo, xHighs.DrawableRange()?.Hi, low.DrawableRange()?.Lo, high.DrawableRange()?.Hi);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "errorbar",
        XData = XData, YData = YData,
        YErrorLow = YErrorLow, YErrorHigh = YErrorHigh,
        XErrorLow = XErrorLow, XErrorHigh = XErrorHigh,
        Color = Color, LineWidth = LineWidth, CapSize = CapSize,
        ELineWidth = ELineWidth,
        CapThick = CapThick,
        ErrorEvery = ErrorEvery.OrNullAtRest(RestingErrorEvery),
    };

    /// <summary>Reconstructs an <see cref="ErrorBarSeries"/> from its serialization DTO, including optional X error bars, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static ErrorBarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.ErrorBar(dto.XData ?? [], dto.YData ?? [], dto.YErrorLow ?? [], dto.YErrorHigh ?? []);
        s.Color = dto.Color;
        s.LineWidth = dto.LineWidth ?? 1.5;
        if (dto.CapSize.HasValue)
        {
            s.CapSize = dto.CapSize.Value;
        }
        s.XErrorLow = dto.XErrorLow;
        s.XErrorHigh = dto.XErrorHigh;
        s.ELineWidth = dto.ELineWidth;
        s.CapThick = dto.CapThick;
        s.ErrorEvery = dto.ErrorEvery ?? RestingErrorEvery;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The point, then every error arm the series carries. An error bar without its errors is a
    /// scatter plot, so the arms are columns, not a footnote.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var columns = new List<ChartDataColumn> { new("x", Axis: DataAxis.X), new(Label ?? "y", Axis: DataAxis.Y) };
        var values = new List<IReadOnlyList<double>> { XData, YData };
        if (YErrorLow is { Length: > 0 } yLow)
        {
            columns.Add(new("y error low", Axis: DataAxis.Y));
            values.Add(yLow);
        }

        if (YErrorHigh is { Length: > 0 } yHigh)
        {
            columns.Add(new("y error high", Axis: DataAxis.Y));
            values.Add(yHigh);
        }

        if (XErrorLow is { Length: > 0 } xLow)
        {
            columns.Add(new("x error low", Axis: DataAxis.X));
            values.Add(xLow);
        }

        if (XErrorHigh is { Length: > 0 } xHigh)
        {
            columns.Add(new("x error high", Axis: DataAxis.X));
            values.Add(xHigh);
        }

        return ChartDataTable.FromNumberColumns(columns, values);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
