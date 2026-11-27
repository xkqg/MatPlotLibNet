// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a polynomial regression series that fits and draws a trend line with optional confidence bands.</summary>
public sealed class RegressionSeries : ChartSeries, IHasColor, ISeriesKind<RegressionSeries>, IGivenStroked
{
    /// <inheritdoc />
    /// <remarks>2: the fit seaborn's <c>regplot</c> draws is a <c>Line2D</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => LegendSwatch.Line(this);

    /// <summary>The x coordinate of each observation the line is fitted to, in data units.</summary>
    public double[] XData { get; }

    /// <summary>The y coordinate of each observation the line is fitted to, in data units; one value per
    /// <see cref="XData"/> entry. An observation with a missing value (NaN) or an infinity is left out of the fit.</summary>
    public double[] YData { get; }

    internal const int RestingDegree = 1;
    /// <summary>The degree of the polynomial that is fitted: the default, <see cref="RestingDegree"/>, is a straight line, 2 a parabola, and so on.
    /// Drawing limits it to 0 through 10.</summary>
    public int Degree { get; set => Change(ref field, value); } = RestingDegree;

    /// <summary>Whether a shaded confidence band is drawn around the fitted line. False by default.</summary>
    public bool ShowConfidence { get; set => Change(ref field, value); } = false;

    private const double RestingConfidenceLevel = Numerics.ConfidenceBand.DefaultLevel;
    /// <summary>The confidence level of the band, as a fraction between 0 and 1. Default <see cref="Numerics.ConfidenceBand.DefaultLevel"/>; it
    /// has an effect only when <see cref="ShowConfidence"/> is true.</summary>
    public double ConfidenceLevel { get; set => Change(ref field, value); } = RestingConfidenceLevel;

    /// <summary>How wide the fitted line is drawn: as a line, but at its own width whatever width the theme names, unless the
    /// theme's cycler names a width for its slot.</summary>
    LineWidthRule IWidthGiven.WidthRule => LineWidthRule.TwoPixelLine;

    /// <summary>The thickness of the fitted line, in pixels. Default: the rest of the library's two-pixel line.</summary>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The width and the dash pattern the line was given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; set => Change(ref field, value); }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The colour of the confidence band; null (the default) uses the colour of the fitted line.</summary>
    public Color? BandColor { get; set => Change(ref field, value); }

    private const double RestingBandAlpha = 0.2;
    /// <summary>The opacity of the confidence band, from 0 (invisible) to 1 (opaque). Default <see cref="RestingBandAlpha"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double BandAlpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(BandAlpha))); } = RestingBandAlpha;

    /// <summary>The dash pattern of the fitted line. Default solid.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <summary>Initializes a new instance of <see cref="RegressionSeries"/> with the specified X and Y data.</summary>
    /// <remarks>Deliberately does NOT validate matching lengths at construction (unlike
    /// <see cref="HexbinSeries"/>'s constructor): a length mismatch is the reachable way a least-squares fit fails,
    /// and the renderer catches that failure and reports it as a diagnostic instead of crashing, so the figure
    /// still renders with this one series missing its line. Throwing here instead would turn that graceful
    /// degradation into a hard construction-time crash.</remarks>
    public RegressionSeries(double[] xData, double[] yData)
    {
        XData = xData;
        YData = yData;
    }

    /// <inheritdoc />
    internal override bool HoldsNoData => XData.Length == 0;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (HoldsNoData) return NoDataUnderTheLibrary;
        // Over the pairs the line is fitted to: a pair with a missing value (NaN) or an infinity is left out.
        var pairs = XData.DrawablePairs(YData);
        return DataRangeContribution.Over(pairs.X, pairs.Y);
    }

    static string ISeriesKind<RegressionSeries>.Kind => "regression";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = XData, YData = YData,
        Degree = Degree.OrNullAtRest(RestingDegree),
        ShowConfidence = ShowConfidence.OrNullAtRest(false),
        ConfidenceLevel = ConfidenceLevel.OrNullAtRest(RestingConfidenceLevel),
        Stroking = Stroke,
        Color = Color,
        FillColor = BandColor,
        Alpha = BandAlpha.OrNullAtRest(RestingBandAlpha)
    };

    /// <summary>The reader of a <see cref="RegressionSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static RegressionSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Regression(dto.XData ?? [], dto.YData ?? []);
        if (dto.Degree is { } degree) s.Degree = degree;
        if (dto.ShowConfidence is { } showConfidence) s.ShowConfidence = showConfidence;
        if (dto.ConfidenceLevel is { } confidenceLevel) s.ConfidenceLevel = confidenceLevel;
        s.Stroke = dto.StrokeOver(s.Stroke);
        if (dto.Color is { } color) s.Color = color;
        if (dto.FillColor is { } fillColor) s.BandColor = fillColor;
        if (dto.Alpha is { } alpha) s.BandAlpha = alpha;
        return s;
    }

    /// <inheritdoc />
    /// <remarks>The observations the fit was computed from — the line itself is derived, the points are data.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.XY(Label, XData, YData);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
