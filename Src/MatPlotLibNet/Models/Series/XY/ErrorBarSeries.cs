// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an error bar series showing uncertainty at each data point.</summary>
public sealed class ErrorBarSeries : XYSeries, IHasColor, ISeriesKind<ErrorBarSeries>, IWidthGiven
{
    /// <inheritdoc />
    /// <remarks>2: the bars and caps of <c>errorbar</c> are a <c>LineCollection</c> and <c>Line2D</c>s at 2.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => LegendSwatch.Of(LegendSwatchKind.ErrorBar, this) with { CapSize = GivenCapSize };

    /// <summary>How far each vertical error bar reaches below its point, in data units, one non-negative distance per point.</summary>
    public double[] YErrorLow { get; }

    /// <summary>How far each vertical error bar reaches above its point, in data units, one non-negative distance per point.</summary>
    public double[] YErrorHigh { get; }

    /// <summary>How far each horizontal error bar reaches left of its point, in data units, one distance per point.
    /// Horizontal bars are drawn only when both this and <see cref="XErrorHigh"/> are set; null (the default) draws none.</summary>
    /// <exception cref="ArgumentException">The array does not hold one distance for each point; the distances the series had are kept.</exception>
    public double[]? XErrorLow { get; set => Change(ref field, value.ThrowIfNotOnePer(XData.Length, nameof(XErrorLow))); }

    /// <summary>How far each horizontal error bar reaches right of its point, in data units, one distance per point.
    /// Horizontal bars are drawn only when both this and <see cref="XErrorLow"/> are set; null (the default) draws none.</summary>
    /// <exception cref="ArgumentException">The array does not hold one distance for each point; the distances the series had are kept.</exception>
    public double[]? XErrorHigh { get; set => Change(ref field, value.ThrowIfNotOnePer(XData.Length, nameof(XErrorHigh))); }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The thickness of the error bars and their caps, in pixels, read and drawn as a line's (<see cref="LineSeries.LineWidth"/>);
    /// <see cref="ELineWidth"/> and <see cref="CapThick"/> override it for the bars and the caps separately.</summary>
    public double LineWidth { get => this.ReportedLineWidth; set => GivenLineWidth = value; }

    /// <summary>The line width that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenLineWidth { get; private set => Change(ref field, value); }

    double? IWidthGiven.GivenLineWidth => GivenLineWidth;

    /// <summary>The half-length of the cap at the end of each error bar, in pixels: the cap spans twice this. Default <see cref="RestingCapSize"/>. Given no size,
    /// the caps reach as far as their theme says: matplotlib's <c>errorbar.capsize</c> under the matplotlib themes (<c>SeriesLineLook.ErrorBarCapSize</c>), <see cref="RestingCapSize"/> under every other; a size that is given, the default included, is drawn as given, and a cap of 0
    /// is not drawn.</summary>
    public double CapSize { get => GivenCapSize ?? RestingCapSize; set => GivenCapSize = value; }

    /// <summary>The cap size a new series holds under a theme that names none.</summary>
    private const double RestingCapSize = 5.0;

    /// <summary>The cap size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenCapSize { get; private set => Change(ref field, value); }

    /// <summary>The half-length of the caps under <paramref name="theme"/>, in pixels.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>The half-length, in pixels.</returns>
    internal double CapSizeUnder(Theme theme) => theme.Looks.Lines.CapSizeOver(GivenCapSize, RestingCapSize);

    /// <summary>The thickness of the error bar lines alone, in pixels; null (the default) uses <see cref="LineWidth"/>.</summary>
    public double? ELineWidth { get; set => Change(ref field, value); }

    /// <summary>The thickness of the caps alone, in pixels; null (the default) uses <see cref="LineWidth"/>.</summary>
    public double? CapThick { get; set => Change(ref field, value); }

    /// <summary>Draws the error bars only at every N-th point, starting with the first, while every point keeps its dot.
    /// The default, <see cref="RestingErrorEvery"/>, draws a bar at every point; a thick data set reads better with a larger value.</summary>
    public int ErrorEvery { get; set => Change(ref field, value); } = RestingErrorEvery;

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

    static string ISeriesKind<ErrorBarSeries>.Kind => "errorbar";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = XData, YData = YData,
        YErrorLow = YErrorLow, YErrorHigh = YErrorHigh,
        XErrorLow = XErrorLow, XErrorHigh = XErrorHigh,
        Color = Color, LineWidth = GivenLineWidth, CapSize = GivenCapSize,
        ELineWidth = ELineWidth,
        CapThick = CapThick,
        ErrorEvery = ErrorEvery.OrNullAtRest(RestingErrorEvery),
    };

    /// <summary>The reader of a <see cref="ErrorBarSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries optional X error bars.</summary>
    internal static ErrorBarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.ErrorBar(dto.XData ?? [], dto.YData ?? [], dto.YErrorLow ?? [], dto.YErrorHigh ?? []);
        s.Color = dto.Color;
        if (dto.LineWidth is { } width) s.LineWidth = width;
        if (dto.CapSize is { } capSize) s.CapSize = capSize;
        s.XErrorLow = dto.XErrorLow;
        s.XErrorHigh = dto.XErrorHigh;
        s.ELineWidth = dto.ELineWidth;
        s.CapThick = dto.CapThick;
        if (dto.ErrorEvery is { } errorEvery) s.ErrorEvery = errorEvery;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The point, then every error arm the series carries. An error bar without its errors is a
    /// scatter plot, so the arms are columns, not a footnote.</remarks>
    public override ChartDataTable? ToDataTable() =>
        this.WithRowKeys(ChartDataTable.XY(Label, XData, YData,
            NumberColumn.IfAny("y error low", DataAxis.Y, YErrorLow), NumberColumn.IfAny("y error high", DataAxis.Y, YErrorHigh),
            NumberColumn.IfAny("x error low", DataAxis.X, XErrorLow), NumberColumn.IfAny("x error high", DataAxis.X, XErrorHigh)));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
