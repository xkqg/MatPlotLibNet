// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>The data of an area in the terms of its drawing, whichever way it lies.</summary>
/// <param name="Along">The positions along the axis the fill runs along: X for an upright fill, Y for one that lies down.</param>
/// <param name="Across">The first curve, on the axis the fill is read on.</param>
/// <param name="Second">The second curve on that axis, or <see langword="null"/> for a fill from zero.</param>
internal readonly record struct AreaCurves(double[] Along, double[] Across, double[]? Second)
{
    /// <summary>Throws when the first curve, or the second, does not hold one value for each position, as the verbs refuse it
    /// (<c>FillBetween</c>, <c>FillBetweenX</c>): the message counts the positions first.</summary>
    /// <exception cref="ArgumentException">A curve holds another number of values than there are positions.</exception>
    internal void ThrowIfMisaligned()
    {
        Along.Length.ThrowIfMisaligned(Across.Length);
        if (Second is not null) Along.Length.ThrowIfMisaligned(Second.Length);
    }
}

/// <summary>Represents a filled area series, rendering the region between a line and a baseline (or between two Y datasets).</summary>
public sealed class AreaSeries : XYSeries, IHasColor, IHasAlpha, IHasEdgeColor, ITableNamedByAxes, IThemedRange, IEdgedPatch, IGivenAlpha, IHatched, ISeriesKind<AreaSeries>, IGivenStroked
{
    /// <inheritdoc />
    /// <remarks>The opacity is the one given (<see cref="IGivenAlpha.GivenAlpha"/>): a fill given none keeps the opacity of its edge colour.</remarks>
    GivenPatchEdge IEdgedPatch.GivenEdge => new(EdgeColor, null, GivenAlpha);

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>fill_between</c> and <c>fill_betweenx</c>, which return a collection: its outline is joined as the theme joins one.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.FillBetween;

    /// <summary>The second curve of an upright fill (<see cref="Orientation"/>): where on the Y axis the fill ends, at
    /// each X position. <see langword="null"/> fills down to Y = 0. A horizontal fill uses <see cref="XData2"/> and
    /// leaves this alone, carried and not checked.</summary>
    /// <exception cref="ArgumentException">The fill stands upright and the curve does not hold one value for each X position; the
    /// curve the fill had is kept.</exception>
    public double[]? YData2
    {
        get;
        set
        {
            CurvesOf(Orientation, XData2, value).ThrowIfMisaligned();
            Change(ref field, value);
        }
    }

    private const BarOrientation RestingOrientation = BarOrientation.Vertical;
    /// <summary>Which way the fill lies: <see cref="BarOrientation.Vertical"/> (the default, <see cref="RestingOrientation"/>) fills between curves of Y over
    /// X, <see cref="BarOrientation.Horizontal"/> between curves of X over Y, matplotlib's <c>fill_betweenx</c>. The data is
    /// held in axis terms either way: <see cref="XYSeries.XData"/> is on the X axis and <see cref="XYSeries.YData"/> on the Y
    /// axis, so a horizontal fill's positions are <see cref="XYSeries.YData"/>, its first curve <see cref="XYSeries.XData"/>
    /// and its second <see cref="XData2"/>.</summary>
    /// <exception cref="ArgumentException">The second curve the fill would draw lying this way does not hold one value for each
    /// position; the fill keeps the way it lay.</exception>
    public BarOrientation Orientation
    {
        get;
        set
        {
            CurvesOf(value, XData2, YData2).ThrowIfMisaligned();
            Change(ref field, value);
        }
    } = RestingOrientation;

    /// <summary>The second curve of a horizontal fill (<see cref="Orientation"/>): where on the X axis the fill ends, at each
    /// position of <see cref="XYSeries.YData"/>. <see langword="null"/> fills down to X = 0. A vertical fill uses
    /// <see cref="YData2"/> and leaves this alone, carried and not checked.</summary>
    /// <exception cref="ArgumentException">The fill lies down and the curve does not hold one value for each Y position; the curve
    /// the fill had is kept.</exception>
    public double[]? XData2
    {
        get;
        set
        {
            CurvesOf(Orientation, value, YData2).ThrowIfMisaligned();
            Change(ref field, value);
        }
    }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>An area given no alpha reads <see cref="RestingAlpha"/>. It is drawn at that value under the library's own themes and opaque under the
    /// matplotlib themes, as matplotlib's <c>fill_between</c> draws its polygon with no alpha of its own; an alpha that was given,
    /// the default included, is drawn as it is under every theme.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get => GivenAlpha ?? RestingAlpha; set => GivenAlpha = value.ThrowIfNotOpacity(nameof(Alpha)); }

    /// <summary>The value of <see cref="Alpha"/> its author gave, <see langword="null"/> while none was.</summary>
    internal double? GivenAlpha { get; private set => Change(ref field, value); }

    /// <summary>The alpha an area reads when none was given.</summary>
    internal const double RestingAlpha = 0.3;

    /// <summary>The alpha that was given, <see langword="null"/> when none was and the fill follows its theme.</summary>
    double? IGivenAlpha.GivenAlpha => GivenAlpha;

    double IGivenAlpha.RestingAlpha => RestingAlpha;

    /// <summary>The dash pattern of the line along the top edge of the area. Default solid.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <summary>The thickness of the line along the top edge of the area, in pixels, read as a line's (<see cref="LineSeries.LineWidth"/>). An area
    /// given no width draws that line under the library's own themes only: matplotlib's <c>fill_between</c> draws no line (its polygon
    /// is edged only where the style edges every patch, as the classic one does); a width that was given is drawn under every theme.</summary>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The width and the dash pattern the line was given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; set => Change(ref field, value); }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <summary>The exact fill colour of the area. When set it is used as given, ignoring <see cref="Alpha"/>; when
    /// null the fill is the series colour at <see cref="Alpha"/> opacity.</summary>
    public Color? FillColor { get; set => Change(ref field, value); }

    /// <summary>The pattern of lines or dots laid over the fill. Default none.</summary>
    public HatchPattern Hatch { get; set => Change(ref field, value); } = HatchPattern.None;

    /// <summary>The colour of the <see cref="Hatch"/> pattern. Null draws it as the theme does: in the colour of the fill's edge, black
    /// when it has none, under v2, black under classic (matplotlib's <c>hatch.color</c>), and under the library's own themes in the
    /// <see cref="EdgeColor"/> given, else in a colour that contrasts with the fill. A colour given takes the fill's opacity.</summary>
    public Color? HatchColor { get; set => Change(ref field, value); }

    /// <summary>The colour the fill blends to at the tip of the area; <see langword="null"/> (the default) is a flat fill. The
    /// fill runs in a straight line across the area from the series colour at its baseline (the zero line, or the mean height
    /// of the lower curve) to this colour at the point of the top edge furthest from it, whichever way the axis runs. With a
    /// <see cref="FillColor"/> that colour is the baseline's; the <see cref="Alpha"/> of the series applies to both colours
    /// otherwise, and a hatch is laid over the gradient. Every backend (SVG, Skia, Maui) draws the gradient.</summary>
    public Color? GradientColor { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public Color? EdgeColor { get; set => Change(ref field, value); }

    private const DrawStyle RestingStepMode = DrawStyle.Default;
    /// <summary>How the edges of the area run between data points: the default joins them with straight segments,
    /// while a step mode holds each value flat until the next point, giving a staircase.</summary>
    public DrawStyle StepMode { get; set => Change(ref field, value); } = RestingStepMode;

    /// <summary>Optional predicate <c>(x, y) => condition</c> that masks which regions get filled, as matplotlib's
    /// <c>fill_between(..., where=...)</c>: it is asked of each data point, and each unbroken run of points it holds for is filled
    /// on its own, stepped (<see cref="StepMode"/>) and smoothed (<see cref="Smooth"/>) as the top line is, between the run's first
    /// and last point. The line along the top is drawn whole.</summary>
    /// <remarks>The one setting of an area that JSON does not carry: a predicate is code, and a JSON document carries
    /// data. A figure read back from JSON fills every segment.</remarks>
    public Func<double, double, bool>? Where { get; set => Change(ref field, value); }

    /// <summary>When <see langword="true"/>, applies Fritsch-Carlson monotone cubic interpolation to the top edge before filling.</summary>
    public bool Smooth { get; set => Change(ref field, value); }

    private const int RestingSmoothResolution = 10;
    /// <summary>Number of interpolated sub-points per input interval when <see cref="Smooth"/> is <see langword="true"/>. Default <see cref="RestingSmoothResolution"/>.</summary>
    public int SmoothResolution { get; set => Change(ref field, value); } = RestingSmoothResolution;

    /// <summary>Creates a new area series from the given X and Y data.</summary>
    /// <param name="xData">The X values.</param>
    /// <param name="yData">The Y values.</param>
    /// <remarks>ZOrder reads -1 until one is given, so a fill is drawn behind the series at 0 under the library's own themes.</remarks>
    public AreaSeries(double[] xData, double[] yData) : base(xData, yData) { }

    /// <summary>A band a verb composes as part of its own chart (a ridge, a stream, a confidence band, a fold of a horizon chart,
    /// a filled staircase): filled between <paramref name="top"/> and <paramref name="bottom"/> at <paramref name="alpha"/> and
    /// outlined along its top at the line's resting width. The width is given, so the band keeps its own look and range under every
    /// theme and is never drawn as matplotlib's <c>fill_between</c> polygon (<see cref="IsFillBetweenUnder"/>).</summary>
    /// <param name="x">The positions along X.</param>
    /// <param name="top">The upper curve, one value per position.</param>
    /// <param name="bottom">The lower curve, one value per position; <see langword="null"/> fills down to Y = 0.</param>
    /// <param name="alpha">The opacity of the fill, given.</param>
    /// <returns>The band, not yet added to an axes.</returns>
    /// <exception cref="ArgumentException">A curve holds another number of values than there are positions.</exception>
    internal static AreaSeries Band(double[] x, double[] top, double[]? bottom, double alpha) =>
        Composed(x, top, bottom, alpha, LineWidthRule.Line.Resting);

    /// <summary>The channel of an indicator (Bollinger bands, Keltner channels): filled between <paramref name="upper"/> and
    /// <paramref name="lower"/> at <paramref name="alpha"/>, with no outline, as the reference draws it with
    /// <c>fill_between(..., linewidth=0)</c>; its middle line is a series of its own. The width (none) is given, so the channel keeps
    /// its own look under every theme.</summary>
    /// <param name="x">The positions along X.</param>
    /// <param name="upper">The upper line, one value per position.</param>
    /// <param name="lower">The lower line, one value per position.</param>
    /// <param name="alpha">The opacity of the fill, given.</param>
    /// <returns>The channel, not yet added to an axes.</returns>
    /// <exception cref="ArgumentException">A line holds another number of values than there are positions.</exception>
    internal static AreaSeries Channel(double[] x, double[] upper, double[] lower, double alpha) => Composed(x, upper, lower, alpha, 0);

    /// <summary>The area a verb composes: the curves (refused by <see cref="YData2"/> when they do not pair), the opacity and the
    /// width of the outline all given.</summary>
    private static AreaSeries Composed(double[] x, double[] top, double[]? bottom, double alpha, double outline) =>
        new(x, top) { YData2 = bottom, Alpha = alpha, LineWidth = outline };

    /// <inheritdoc />
    /// <remarks>-1: a fill is drawn behind the lines and markers it shades.</remarks>
    internal override int RestingZOrder => -1;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Empty data: no contribution to axis range. Without this guard, .Min()/.Max()
        // throw "Sequence contains no elements" — caught by RendererEdgeCaseTests.
        if (YData.Length == 0) return default;

        // A horizontal fill is the vertical one on its side: measured as an upright fill (its positions along, its curves
        // across, on the scale of the axis its values run along) and put on its axes, so the two orientations cannot range
        // differently.
        var curves = Curves;
        var upright = Upright(curves.Along, curves.Across, curves.Second, Orientation.ValueScale(context));
        return upright.OnAxes(Orientation);
    }

    /// <summary>Whether <paramref name="theme"/> draws this area as matplotlib's <c>fill_between</c> polygon: under the matplotlib
    /// themes (<see cref="PatchLook.LikeCollections"/>) when it was given no line width, so it draws no line of its own. A verb that lays
    /// a fill as part of its own chart (a ridgeline, a stream graph, a Marimekko chart, filled stairs) names its width and keeps its
    /// own look and range.</summary>
    /// <param name="theme">The theme the area is drawn under.</param>
    /// <returns><see langword="true"/> when the area is drawn as a <c>fill_between</c> polygon.</returns>
    internal bool IsFillBetweenUnder(Theme theme) => theme.Looks.Patch.LikeCollections && Stroke.Width is null;

    /// <inheritdoc />
    /// <remarks>Edged as its polygon is where it is drawn as <c>fill_between</c> (<see cref="IsFillBetweenUnder"/>), so the handle of a
    /// classic fill is edged in black, as matplotlib's handle copies the collection; a plain patch where it draws a line of its own.</remarks>
    internal override LegendSwatch LegendSwatch => Stroke.Width is null ? LegendSwatch.EdgedPatch : LegendSwatch.Patch;

    /// <inheritdoc />
    /// <remarks>Where it is drawn as <c>fill_between</c> (<see cref="IsFillBetweenUnder"/>) the fill holds no edge sticky, so the axes'
    /// margin pads past it on both sides, as matplotlib's <c>fill_between</c> and <c>fill_betweenx</c> register no sticky edge
    /// (printed: <c>sticky_edges</c> empty, xlim -0.5 .. 10.5 over x 0 .. 10 under the default style). Otherwise the ends of the fill
    /// and a floor of zero stay sticky (<see cref="ComputeDataRange"/>).</remarks>
    DataRangeContribution IThemedRange.RangeUnder(IAxesContext context, Theme theme) =>
        IsFillBetweenUnder(theme) ? ComputeDataRange(context) with { StickyXMin = null, StickyXMax = null, StickyYMin = null, StickyYMax = null }
            : ComputeDataRange(context);

    /// <summary>The data of the fill in the terms of its drawing: <see cref="AreaCurves.Along"/> is the axis the fill runs
    /// along (X for an upright fill, Y for one that lies down), <see cref="AreaCurves.Across"/> the axis its first curve is
    /// read on, <see cref="AreaCurves.Second"/> its second curve.</summary>
    internal AreaCurves Curves => CurvesOf(Orientation, XData2, YData2);

    /// <summary>The curves the fill draws lying <paramref name="orientation"/> with these second curves: the one rule <see cref="Curves"/>
    /// reads and every setter that changes what is drawn checks (<see cref="AreaCurves.ThrowIfMisaligned"/>) before it takes the value, so
    /// a curve set after the fill was made is refused as its verb refuses it, and the second curve the fill does not draw is not checked.</summary>
    private AreaCurves CurvesOf(BarOrientation orientation, double[]? xData2, double[]? yData2) => orientation.ValuesAlongX
        ? new AreaCurves(YData, XData, xData2)
        : new AreaCurves(XData, YData, yData2);

    /// <summary>The range of the fill as if it stood upright: <paramref name="along"/> on the axis it runs along,
    /// <paramref name="across"/> and <paramref name="second"/> on the axis its curves are read on, which places them by
    /// <paramref name="scale"/>.</summary>
    private static DataRangeContribution Upright(double[] along, double[] across, double[]? second, ScaleRule scale)
    {
        // Both edges of the area: its top and its bottom, the second curve or the zero it is filled down to. Over what the Y
        // axis can place: a gap in the area (NaN) and an infinity are not part of its extent, and on a log axis, which has
        // no place for zero (the bottom is drawn at the plot's edge there), the range starts at the lowest value reached.
        bool toZero = second is not { Length: > 0 };
        var y = toZero
            ? across.DrawableRange(scale) is { } top ? top.Including(0, scale) : (Range1D?)null
            : new[] { across, second! }.DrawableRange(scale);
        // fill_between with y2=0 and non-negative y1: sticky floor
        double? stickyYMin = toZero && y is { Lo: >= 0 } ? 0 : null;
        var x = along.DrawableRange();
        return DataRangeContribution.Spanning(x, y) with { StickyXMin = x?.Lo, StickyXMax = x?.Hi, StickyYMin = stickyYMin };
    }

    static string ISeriesKind<AreaSeries>.Kind => "area";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = XData, YData = YData, YData2 = YData2,
        XData2 = XData2,
        // Left out for the upright fill, so its golden stays byte-identical.
        Orientation = Orientation.OrNullAtRest(RestingOrientation)?.ToWireName(),
        Color = Color, Alpha = GivenAlpha,
        Stroking = Stroke,
        Smooth = Smooth.OrNullAtRest(false),
        SmoothResolution = Smooth ? SmoothResolution.OrNullAtRest(RestingSmoothResolution) : null,
        // Null when unhatched, so an unhatched area emits no hatch bytes and its golden stays byte-identical.
        Hatching = this.Hatching,
        GradientColor = GradientColor,
        FillColor = FillColor,
        EdgeColor = EdgeColor,
        DrawStyle = StepMode.OrNullAtRest(RestingStepMode),
    };

    /// <summary>The reader of a <see cref="AreaSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries the optional second Y dataset for fill-between.</summary>
    internal static AreaSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        // The record holds the data as the series does, in axis terms, whichever way the fill lies; the curves it draws are refused
        // by the setters (Orientation, YData2, XData2) as its verb (FillBetween, FillBetweenX) refuses them, before it is added. Both
        // second curves travel; the one that is not drawn is simply not used.
        var s = new AreaSeries(dto.XData ?? [], dto.YData ?? [])
        {
            Orientation = dto.Orientation.WireEnum<BarOrientation>() ?? RestingOrientation,
            YData2 = dto.YData2,
            XData2 = dto.XData2,
        };
        axes.AddSeries(s);
        s.Color = dto.Color;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        s.Stroke = dto.StrokeOver(s.Stroke);
        if (dto.Smooth == true)
        {
            s.Smooth = true;
        }
        if (dto.SmoothResolution is { } smoothResolution) s.SmoothResolution = smoothResolution;

        s.LayHatching(dto.Hatching);
        s.GradientColor = dto.GradientColor;
        s.FillColor = dto.FillColor;
        s.EdgeColor = dto.EdgeColor;
        if (dto.DrawStyle is { } drawStyle) s.StepMode = drawStyle;
        return s;
    }

    /// <inheritdoc />
    /// <remarks>A fill between two lines carries both of them; a fill to the baseline carries one.</remarks>
    public override ChartDataTable? ToDataTable() =>
        Orientation.ValuesAlongX
            ? this.WithRowKeys(HorizontalTable())
            : YData2 is { Length: > 0 } second
            ? this.WithRowKeys(ChartDataTable.XY(Label, XData, YData, new NumberColumn("y2", second, DataAxis.Y)))
            : base.ToDataTable();

    /// <inheritdoc />
    /// <remarks>Two shapes stand on the names of an axis as a whole.
    /// <para>A fill down to one level the Y axis names stands on that name: a ridge on its group's baseline. Its table is the
    /// x and the height of the fill above the level, row by row, under the series' label or, without one, the name; the level
    /// itself says nothing a row needs, and a height is no position.</para>
    /// <para>A fill drawn as columns, each a run of two edges between gaps (the cells of a Marimekko chart), whose centres the
    /// X axis names: a row for each column, keyed by its name, with the edge it begins at, the edge it ends at, and the height
    /// of the fill in it, which is the height of the cell. A column the axis does not name is no row, and an area with a run it
    /// cannot name is no columns. Any other area tables as <see cref="ToDataTable()"/> does.</para></remarks>
    ChartDataTable? ITableNamedByAxes.ToDataTable(AxisNames names)
    {
        // The names of the axes are read off the upright fill; a fill that lies down is tabled as it is.
        if (Orientation.ValuesAlongX) return ToDataTable();

        if (ColumnRuns(names) is { } columns)
        {
            var rows = columns.Select(column => (IReadOnlyList<DataCell>)
                [DataCell.FromNumber(column.Centre), DataCell.FromNumber(column.Left), DataCell.FromNumber(column.Right), DataCell.FromNumber(column.Height)]);
            return new ChartDataTable(null,
                [ChartDataColumn.Position(DataAxis.X), ChartDataColumn.Position(DataAxis.X, "left"), ChartDataColumn.Position(DataAxis.X, "right"), new(Label ?? "y")], [.. rows]);
        }

        if (Level() is not { } level || names.Along(DataAxis.Y, [level]) is not [{ } name])
        {
            return ToDataTable();
        }

        double[] heights = [.. YData.Zip(YData2!, (top, bottom) => top - bottom)];
        return ChartDataTable.FromNumberColumns([ChartDataColumn.Position(DataAxis.X), new(Label ?? name)], [XData, heights]);
    }

    /// <summary>The table of a horizontal fill: the positions along Y, then the curve (or curves) on X.</summary>
    private ChartDataTable HorizontalTable() => XData2 is { Length: > 0 } second
        ? ChartDataTable.FromNumberColumns(
            [ChartDataColumn.Position(DataAxis.Y), ChartDataColumn.Position(DataAxis.X, Label ?? "x1"), ChartDataColumn.Position(DataAxis.X, "x2")], [YData, XData, second])
        : ChartDataTable.FromNumberColumns([ChartDataColumn.Position(DataAxis.Y), ChartDataColumn.Position(DataAxis.X, Label)], [YData, XData]);

    /// <summary>One column of an area drawn as columns: where it stands and how high the fill is in it.</summary>
    private readonly record struct ColumnRun(double Centre, double Left, double Right, double Height);

    /// <summary>The columns this area is drawn as, when it is drawn as columns the X axis names: the points between two
    /// gaps (a missing x) are a run, a run of two points is a column from the one to the other, and the column stands on the
    /// name the axis shows at its centre, <c>(left + right) / 2</c>, the position a Marimekko chart puts its name at. A column
    /// with no width has no name and is left out. The fill's height in a column is the height of its first point above the
    /// second curve, or above the baseline when there is none.</summary>
    /// <param name="names">The names the axes shows along the axes this area stands on.</param>
    /// <returns>The columns in order; <see langword="null"/> when the area is no columns: a run is not two points, a run of
    /// some width stands on no name, or there is no column at all.</returns>
    private ColumnRun[]? ColumnRuns(AxisNames names)
    {
        var columns = new List<ColumnRun>();
        for (int start = 0; start < XData.Length;)
        {
            if (!double.IsFinite(XData[start])) { start++; continue; }

            int end = start;
            while (end + 1 < XData.Length && double.IsFinite(XData[end + 1])) end++;
            if (end - start != 1) return null;

            double left = Math.Min(XData[start], XData[end]), right = Math.Max(XData[start], XData[end]);
            if (left != right)
            {
                double centre = (left + right) / 2;
                if (names.Along(DataAxis.X, [centre]) is not [{ }]) return null;
                columns.Add(new(centre, left, right, YData[start] - (YData2 is { } second ? second[start] : 0)));
            }

            start = end + 1;
        }

        return columns.Count > 0 ? [.. columns] : null;
    }

    /// <summary>The one level the second curve holds, when it is one: every value of it that can be drawn is the same
    /// number (a value that cannot be drawn is a gap, not another level).</summary>
    /// <returns>The level, or <see langword="null"/> when there is no second curve, nothing of it can be drawn, or it
    /// holds more than one value.</returns>
    private double? Level()
    {
        double[] drawn = [.. (YData2 ?? []).Where(double.IsFinite).Distinct()];
        return drawn is [var level] ? level : null;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
