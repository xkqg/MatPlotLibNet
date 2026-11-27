// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a residual plot series that displays regression residuals as a scatter plot with an optional zero line.</summary>
public sealed class ResidualSeries : ChartSeries, IHasColor, IDotSeries, IThemedRange, ISeriesKind<ResidualSeries>
{
    /// <summary>The opacity of the dots the theme's dot look names for a regression's scatter (<see cref="DotLook.RegressionAlpha"/>:
    /// seaborn's 0.8 under the matplotlib themes).</summary>
    double? IGivenAlpha.ThemeAlphaOf(ThemeLooks looks) => looks.Dots.RegressionAlpha;

    /// <summary>The x coordinate of each observation, in data units; the residuals are plotted against these.</summary>
    public Vec XData { get; }

    /// <summary>The y coordinate of each observation, in data units; one value per <see cref="XData"/> entry. A residual
    /// is the distance of an observation from the fitted polynomial.</summary>
    public Vec YData { get; }

    private const int RestingDegree = RegressionSeries.RestingDegree;
    /// <summary>The degree of the polynomial whose misfit is plotted: the default, <see cref="RestingDegree"/>, is a straight line, 2 a
    /// parabola, and so on.</summary>
    public int Degree { get; set => Change(ref field, value); } = RestingDegree;

    /// <summary>The diameter of each residual marker, in pixels. Default <see cref="MarkerLook.RestingSize"/>. Given none, the marker is the size <c>Axes.scatter</c> draws
    /// where it is given none (<c>residplot</c> gives <c>scatterplot</c> no size) under the matplotlib themes, 6 points across under the
    /// default style and 4.47 under classic, and <see cref="MarkerLook.RestingSize"/> pixels under every other theme; a size that is given, the default included, is drawn as pixels.</summary>
    public double MarkerSize { get => Dot.Diameter ?? Rests.Diameter; set => Dot = Dot with { Diameter = value }; }

    /// <summary>What was named of the dots (the diameter); <see langword="null"/> while none was.</summary>
    internal GivenDot Dot { get; set => Change(ref field, value); }

    /// <summary>What a residual plot rests at: <see cref="MarkerLook.RestingSize"/> pixels across.</summary>
    private static readonly RestingDot Rests = new(MarkerLook.RestingSize);

    GivenDot IDotSeries.Given => Dot;

    RestingDot IDotSeries.Resting => Rests;

    /// <inheritdoc />
    /// <remarks><c>residplot</c> gives <c>scatterplot</c> no size, so <c>Axes.scatter</c>'s own is drawn.</remarks>
    double IDotSeries.SeabornDiameter(SeriesSlot slot, DotLook look) => slot.ScatterDiameter;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    private const bool RestingShowZeroLine = true;
    /// <summary>Whether a horizontal line is drawn at a residual of zero, as the reference for a perfect fit: dashed in the dots'
    /// colour under the library's own themes, dotted in dark grey under the matplotlib themes, as seaborn's <c>residplot</c> draws it.
    /// True by default.</summary>
    public bool ShowZeroLine { get; set => Change(ref field, value); } = RestingShowZeroLine;

    /// <summary>Initializes a new instance of <see cref="ResidualSeries"/> with the specified X and Y data.</summary>
    /// <param name="xData">The X data values.</param>
    /// <param name="yData">The Y data values.</param>
    public ResidualSeries(Vec xData, Vec yData)
    {
        XData = xData;
        YData = yData;
    }

    /// <inheritdoc />
    internal override bool HoldsNoData => XData.Length == 0;

    /// <inheritdoc />
    /// <remarks>The range under the library's themes.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => RangeIn(SeabornVerbLook.Library);

    /// <inheritdoc />
    /// <remarks>The range under the theme's verb look (<see cref="RangeIn"/>).</remarks>
    DataRangeContribution IThemedRange.RangeUnder(IAxesContext context, Theme theme) => RangeIn(theme.Looks.Verbs);

    /// <summary>The range of the residuals under <paramref name="look"/>: X over the observations, Y over the residuals padded by the
    /// look's share of their spread (<see cref="SeabornVerbLook.ResidualPadding"/>: a tenth under the library's themes, at least a tenth
    /// of a unit; none under seaborn's, whose scatter and zero line are ranged by the axes' margins alone). The residuals of a fit with
    /// a constant term straddle 0, so the zero line stands inside it.</summary>
    private DataRangeContribution RangeIn(SeabornVerbLook look)
    {
        if (HoldsNoData) return new(0, 1, -1, 1);
        // The fit is made over the pairs that can be drawn: a pair with a missing value (NaN) or an infinity is left out.
        var pairs = XData.Data.DrawablePairs(YData.Data);
        if (pairs.X.DrawableRange() is not { } x) return default;
        try
        {
            double[] coeffs = LeastSquares.PolyFit(pairs.X, pairs.Y, Degree);
            double[] predicted = LeastSquares.PolyEval(coeffs, pairs.X);
            Vec residuals = new Vec(pairs.Y) - new Vec(predicted);
            double rMin = residuals.Min();
            double rMax = residuals.Max();
            double padding = Math.Max(Math.Abs(rMax - rMin), 1) * look.ResidualPadding;
            return new(x.Lo, x.Hi, rMin - padding, rMax + padding);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            // Same reachable failure as ResidualSeriesRenderer.Render (mismatched XData/YData
            // lengths) — a series whose range can't be computed contributes nothing, mirroring the
            // empty-series default above, rather than crashing axes-range aggregation for the whole
            // figure.
            ChartDiagnostics.Emit(new ChartDiagnostic(
                nameof(ResidualSeries),
                $"Residual range computation failed and was skipped: {ex.Message}",
                ex));
            return new(0, 1, -1, 1);
        }
    }

    static string ISeriesKind<ResidualSeries>.Kind => "residual";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = XData,
        YData = YData,
        Degree = Degree,
        MarkerSize = Dot.Diameter,
        Color = Color,
        ShowZeroLine = ShowZeroLine.OrNullAtRest(RestingShowZeroLine),
    };

    /// <summary>The reader of a <see cref="ResidualSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static ResidualSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Residplot(dto.XData ?? [], dto.YData ?? []);
        if (dto.Degree is { } degree) s.Degree = degree;
        if (dto.MarkerSize is { } markerSize) s.MarkerSize = markerSize;
        if (dto.Color is { } color) s.Color = color;
        if (dto.ShowZeroLine is { } showZeroLine) s.ShowZeroLine = showZeroLine;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The observations; the residual the chart draws is derived from them by the renderer.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.XY(Label, XData.Data, YData.Data);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
