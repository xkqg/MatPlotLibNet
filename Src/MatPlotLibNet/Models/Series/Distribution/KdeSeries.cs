// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a kernel density estimation (KDE) series that draws a smooth density curve for a data sample.</summary>
public sealed class KdeSeries : ChartSeries, IHasColor, IHasAlpha, IKeepsDataCopy, ISeriesKind<KdeSeries>, IGivenStroked, INamesValueAxis,
    IThemedRange
{
    /// <inheritdoc />
    /// <remarks>seaborn's <c>kdeplot</c> names its density axis 'Density'.</remarks>
    string INamesValueAxis.ValueAxisName => "Density";

    /// <inheritdoc />
    bool INamesValueAxis.ValuesAlongX => false;

    /// <inheritdoc />
    /// <remarks>What seaborn's <c>kdeplot</c> draws (<see cref="SeabornVerbLook.Seaborn"/>): the curve alone, a <c>Line2D</c> at 2, asked as
    /// its <c>plot</c>; a filled density where a fill was given, a <c>PolyCollection</c> at 1, asked as its <c>fill_between</c>.</remarks>
    internal override MatplotlibArtist Artist =>
        SeabornVerbLook.Seaborn.DensityFillsOver(GivenFill) ? MatplotlibArtist.FillBetween : MatplotlibArtist.Line;

    /// <inheritdoc />
    /// <remarks>The handle of the curve as its theme draws it (<see cref="FillsUnder"/>): the filled curve's rectangle when it is filled;
    /// a line from the stroke it was given when it is not, as matplotlib's <c>HandlerLine2D</c> draws the handle of seaborn's
    /// <c>kdeplot(fill=False)</c>.</remarks>
    internal override IReadOnlyList<LegendPart> LegendPartsUnder(Theme theme) =>
        base.LegendPartsUnder(theme) is [var part] ? [part with { Swatch = FillsUnder(theme) ? LegendSwatch.Patch : LegendSwatch.Line(this) }] : [];

    /// <summary>The observations the density is estimated from; a missing value (NaN) or an infinity is not used.</summary>
    public double[] Data { get; }

    /// <summary>The width of the Gaussian kernel that smooths the curve, in data units. <see langword="null"/> (the
    /// default), or a value that is not a positive finite number, takes the theme's rule: Silverman's rule of thumb under the library's
    /// own themes, Scott's (scipy's <c>gaussian_kde</c>, which seaborn's <c>kdeplot</c> smooths with) under the matplotlib themes.</summary>
    public double? Bandwidth { get; set => Change(ref field, value); }

    /// <summary>When <see langword="true"/> the area under the curve is filled at <see cref="Alpha"/>; when <see langword="false"/> only the
    /// curve is drawn. Given no choice, the curve is filled under the library's own themes (<see langword="true"/>, the value this reads)
    /// and drawn alone under the matplotlib themes, as seaborn's <c>kdeplot(fill=False)</c>; a choice that is given is drawn as given.</summary>
    public bool Fill { get => SeabornVerbLook.Library.DensityFillsOver(GivenFill); set => GivenFill = value; }

    /// <summary>The value of <see cref="Fill"/> its author gave, <see langword="null"/> while none was.</summary>
    internal bool? GivenFill { get; private set => Change(ref field, value); }

    /// <inheritdoc />
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = 0.3;

    /// <inheritdoc cref="LineSeries.LineWidth"/>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The width and the dash pattern the line was given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; set => Change(ref field, value); }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The dash pattern of the density curve. Default <see cref="Styling.GivenStroke.RestingStyle"/>.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <summary>Initializes a new instance of <see cref="KdeSeries"/> with the specified data.</summary>
    /// <param name="data">The data values used to estimate the density.</param>
    public KdeSeries(double[] data)
    {
        Data = data;
    }

    /// <summary>Whether the curve is filled under <paramref name="theme"/>: as its author chose, else as the theme's verb look fills a
    /// density curve (<see cref="SeabornVerbLook.DensityFills"/>). The renderer and the legend read it.</summary>
    /// <param name="theme">The theme the curve is drawn under.</param>
    /// <returns><see langword="true"/> when the area under the curve is filled.</returns>
    internal bool FillsUnder(Theme theme) => theme.Looks.Verbs.DensityFillsOver(GivenFill);

    /// <inheritdoc />
    /// <remarks>The range under the theme's looks (<see cref="RangeIn"/>).</remarks>
    DataRangeContribution IThemedRange.RangeUnder(IAxesContext context, Theme theme) => RangeIn(theme.Looks);

    /// <inheritdoc />
    internal override bool HoldsNoData => Data.Length == 0;

    /// <inheritdoc />
    /// <remarks>seaborn's <c>kdeplot</c> returns before it touches the axes when it is handed no data: they keep (0, 1).</remarks>
    internal override bool AsksWhenEmpty => false;

    /// <inheritdoc />
    /// <remarks>The range under the library's themes.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => RangeIn(ThemeLooks.Library);

    /// <summary>The curve's own extent under <paramref name="looks"/>: the grid it is drawn on (<see cref="CurveUnder"/>) and the density
    /// from 0 (<see cref="SeabornVerbLook.DensityRangesFromZero"/>, or the fill's base where it is filled; else the line's lowest density,
    /// as matplotlib's data limits range a <c>Line2D</c>) to the peak times the verb look's headroom
    /// (<see cref="SeabornVerbLook.DensityHeadroom"/>), sticky at its base (<see cref="SeabornVerbLook.DensityBase"/>: seaborn's curve stands
    /// on 0, so no margin opens under it). The axes cover the whole curve and nothing past it. (Reporting no yMax would leave the
    /// aggregator's yMax at double.MinValue and the margin pass would produce axis labels around 1e307.)</summary>
    private DataRangeContribution RangeIn(ThemeLooks looks)
    {
        if (HoldsNoData) return NoDataUnderTheLibrary;
        if (CurveUnder(looks) is not { } curve) return default;
        var verbs = looks.Verbs;
        double low = verbs.DensityRangesFromZero || verbs.DensityFillsOver(GivenFill) ? 0 : curve.Y.Min();
        return new(curve.X[0], curve.X[^1], low, curve.Y.Max() * verbs.DensityHeadroom, StickyYMin: verbs.DensityBase);
    }

    /// <summary>The curve the last estimate made, with what it was made from.</summary>
    [RenderCache]
    private KeptEstimate<XYCurve?>? _curve;

    [RenderCache]
    private DataSnapshot? _dataCopy;

    ref DataSnapshot? IKeepsDataCopy.DataCopy => ref _dataCopy;

    /// <summary>The density curve under <paramref name="looks"/>: the grid it is drawn on, which reaches three bandwidths past the
    /// smallest and the largest observation, and the density at each point of it, smoothed with the given bandwidth, else by the
    /// theme's rule (<see cref="ThemeLooks.DensityCurveRule"/>: Silverman's on 100 points under the library's themes, seaborn's on 200
    /// under the matplotlib themes). The range of the axes and the renderer both read it, so the curve and the range are one extent. It
    /// is estimated once and kept until a setting or a value of <see cref="Data"/> changes or it is asked under another rule
    /// (<see cref="KeptEstimate{T}"/>), so neither the second reader nor a figure drawn again estimates it twice.</summary>
    /// <param name="looks">The looks of the theme the curve is drawn under.</param>
    /// <returns>The curve; <see langword="null"/> when no observation can be drawn.</returns>
    internal XYCurve? CurveUnder(ThemeLooks looks) =>
        KeptEstimate<XYCurve?>.Of(ref _curve, this, looks.DensityCurveRule, static (series, rule) => series.Data.DensityCurve(series.Bandwidth, rule));

    static string ISeriesKind<KdeSeries>.Kind => "kde";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Data = Data,
        Bandwidth = Bandwidth,
        Alpha = Alpha,
        Stroking = Stroke,
        Color = Color,
        Fill = GivenFill,
    };

    /// <summary>The reader of a <see cref="KdeSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static KdeSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Kde(dto.Data ?? []);
        if (dto.Bandwidth is { } bandwidth) s.Bandwidth = bandwidth;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        s.Stroke = dto.StrokeOver(s.Stroke);
        if (dto.Color is { } color) s.Color = color;
        if (dto.Fill is { } fill) s.Fill = fill;
        return s;
    }

    /// <inheritdoc />
    /// <remarks>The samples the curve is estimated from.</remarks>
    public override ChartDataTable? ToDataTable() => ChartDataTable.FromIndexedValues(ChartDataColumn.Position(DataAxis.X, Label ?? "value"), Data);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
