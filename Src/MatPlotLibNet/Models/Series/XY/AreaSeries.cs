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
internal readonly record struct AreaCurves(double[] Along, double[] Across, double[]? Second);

/// <summary>Represents a filled area series, rendering the region between a line and a baseline (or between two Y datasets).</summary>
public sealed class AreaSeries : XYSeries, IHasColor, IHasAlpha, IHasEdgeColor, ITableNamedByAxes
{
    /// <summary>The second curve of an upright fill (<see cref="Orientation"/>): where on the Y axis the fill ends, at
    /// each X position. <see langword="null"/> fills down to Y = 0. A horizontal fill uses <see cref="XData2"/> and
    /// leaves this alone.</summary>
    public double[]? YData2 { get; set; }

    /// <summary>Which way the fill lies: <see cref="BarOrientation.Vertical"/> (the default) fills between curves of Y over
    /// X, <see cref="BarOrientation.Horizontal"/> between curves of X over Y, matplotlib's <c>fill_betweenx</c>. The data is
    /// held in axis terms either way: <see cref="XYSeries.XData"/> is on the X axis and <see cref="XYSeries.YData"/> on the Y
    /// axis, so a horizontal fill's positions are <see cref="XYSeries.YData"/>, its first curve <see cref="XYSeries.XData"/>
    /// and its second <see cref="XData2"/>.</summary>
    public BarOrientation Orientation { get; set; } = BarOrientation.Vertical;

    /// <summary>The second curve of a horizontal fill (<see cref="Orientation"/>): where on the X axis the fill ends, at each
    /// position of <see cref="XYSeries.YData"/>. <see langword="null"/> fills down to X = 0. A vertical fill uses
    /// <see cref="YData2"/> and leaves this alone.</summary>
    public double[]? XData2 { get; set; }

    /// <inheritdoc />
    public Color? Color { get; set; }

    /// <inheritdoc />
    public double Alpha { get; set; } = 0.3;

    /// <summary>The dash pattern of the line along the top edge of the area. Default solid.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>The thickness of the line along the top edge of the area, in pixels. Default 1.5.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>The exact fill colour of the area. When set it is used as given, ignoring <see cref="Alpha"/>; when
    /// null the fill is the series colour at <see cref="Alpha"/> opacity.</summary>
    public Color? FillColor { get; set; }

    /// <summary>The pattern of lines or dots laid over the fill. Default none.</summary>
    public HatchPattern Hatch { get; set; } = HatchPattern.None;

    /// <summary>The colour of the <see cref="Hatch"/> pattern; null picks a colour that contrasts with the fill.</summary>
    public Color? HatchColor { get; set; }

    /// <summary>The colour the fill blends to at the tip of the area; <see langword="null"/> (the default) is a flat fill. The
    /// fill runs in a straight line across the area from the series colour at its baseline (the zero line, or the mean height
    /// of the lower curve) to this colour at the point of the top edge furthest from it, whichever way the axis runs. With a
    /// <see cref="FillColor"/> that colour is the baseline's; the <see cref="Alpha"/> of the series applies to both colours
    /// otherwise, and a hatch is laid over the gradient. Every backend (SVG, Skia, Maui) draws the gradient.</summary>
    public Color? GradientColor { get; set; }

    /// <inheritdoc />
    public Color? EdgeColor { get; set; }

    /// <summary>How the edges of the area run between data points: the default joins them with straight segments,
    /// while a step mode holds each value flat until the next point, giving a staircase.</summary>
    public DrawStyle StepMode { get; set; } = DrawStyle.Default;

    /// <summary>Optional predicate <c>(x, y) => condition</c> that masks which regions get filled.
    /// Segments where the predicate returns <see langword="false"/> are skipped.</summary>
    /// <remarks>The one setting of an area that JSON does not carry: a predicate is code, and a JSON document carries
    /// data. A figure read back from JSON fills every segment.</remarks>
    public Func<double, double, bool>? Where { get; set; }

    /// <summary>When <see langword="true"/>, applies Fritsch-Carlson monotone cubic interpolation to the top edge before filling.</summary>
    public bool Smooth { get; set; }

    /// <summary>Number of interpolated sub-points per input interval when <see cref="Smooth"/> is <see langword="true"/>. Default 10.</summary>
    public int SmoothResolution { get; set; } = 10;

    /// <summary>Creates a new area series from the given X and Y data.</summary>
    /// <remarks>ZOrder defaults to -1 so fills render behind all other series (ZOrder 0).</remarks>
    public AreaSeries(double[] xData, double[] yData) : base(xData, yData) { ZOrder = RestingZOrder; }

    /// <inheritdoc />
    /// <remarks>-1: a fill is drawn behind the lines and markers it shades.</remarks>
    internal override int RestingZOrder => -1;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Empty data: no contribution to axis range. Without this guard, .Min()/.Max()
        // throw "Sequence contains no elements" — caught by RendererEdgeCaseTests.
        if (YData.Length == 0) return new DataRangeContribution(null, null, null, null);

        // A horizontal fill is the vertical one on its side: measured as an upright fill (its positions along, its curves
        // across) and put on its axes, so the two orientations cannot range differently.
        bool horizontal = Orientation == BarOrientation.Horizontal;
        var scale = horizontal ? context.XScale : context.YScale;
        var curves = Curves;
        double[] along = curves.Along, across = curves.Across;
        double[]? second = curves.Second;
        var upright = Upright(along, across, second, scale);
        return horizontal ? upright.OnAxes(Orientation) : upright;
    }

    /// <summary>The data of the fill in the terms of its drawing: <see cref="AreaCurves.Along"/> is the axis the fill runs
    /// along (X for an upright fill, Y for one that lies down), <see cref="AreaCurves.Across"/> the axis its first curve is
    /// read on, <see cref="AreaCurves.Second"/> its second curve.</summary>
    internal AreaCurves Curves => Orientation == BarOrientation.Horizontal
        ? new AreaCurves(YData, XData, XData2)
        : new AreaCurves(XData, YData, YData2);

    /// <summary>The range of the fill as if it stood upright: <paramref name="along"/> on the axis it runs along,
    /// <paramref name="across"/> and <paramref name="second"/> on the axis its curves are read on, which places them by
    /// <paramref name="scale"/>.</summary>
    private static DataRangeContribution Upright(double[] along, double[] across, double[]? second, AxisScale scale)
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
        return new(x?.Lo, x?.Hi, y?.Lo, y?.Hi,
            StickyXMin: x?.Lo, StickyXMax: x?.Hi, StickyYMin: stickyYMin);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "area",
        XData = XData, YData = YData, YData2 = YData2,
        XData2 = XData2,
        // Left out for the upright fill, so its golden stays byte-identical.
        Orientation = Orientation == BarOrientation.Horizontal ? "horizontal" : null,
        Color = Color, Alpha = Alpha,
        LineStyle = LineStyle.ToString().ToLowerInvariant(),
        LineWidth = LineWidth,
        Smooth = Smooth ? true : null,
        SmoothResolution = Smooth && SmoothResolution != 10 ? SmoothResolution : null,
        // Null when unhatched, so an unhatched area emits no hatch bytes and its golden stays byte-identical.
        Hatch = Hatch != HatchPattern.None ? Hatch : null,
        HatchColor = HatchColor,
        GradientColor = GradientColor,
        FillColor = FillColor,
        EdgeColor = EdgeColor,
        DrawStyle = StepMode.OrNullAtRest(DrawStyle.Default),
    };

    /// <summary>Reconstructs an <see cref="AreaSeries"/> from its serialization DTO, including the optional second Y dataset for fill-between, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static AreaSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        bool horizontal = string.Equals(dto.Orientation, "horizontal", StringComparison.OrdinalIgnoreCase);
        var s = horizontal
            ? axes.FillBetweenX(dto.YData ?? [], dto.XData ?? [], dto.XData2)
            : axes.FillBetween(dto.XData ?? [], dto.YData ?? [], dto.YData2);
        // Both second curves travel, whichever way the fill lies; the one that is not drawn is simply not used.
        s.YData2 = dto.YData2;
        s.XData2 = dto.XData2;
        s.Color = dto.Color;
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        s.LineWidth = dto.LineWidth ?? 1.5;
        dto.LineStyle.ApplyEnum<LineStyle>(v => s.LineStyle = v);
        if (dto.Smooth == true)
        {
            s.Smooth = true;
        }
        if (dto.SmoothResolution.HasValue)
        {
            s.SmoothResolution = dto.SmoothResolution.Value;
        }

        if (dto.Hatch.HasValue)
        {
            s.Hatch = dto.Hatch.Value;
        }

        s.HatchColor = dto.HatchColor;
        s.GradientColor = dto.GradientColor;
        s.FillColor = dto.FillColor;
        s.EdgeColor = dto.EdgeColor;
        s.StepMode = dto.DrawStyle ?? DrawStyle.Default;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>A fill between two lines carries both of them; a fill to the baseline carries one.</remarks>
    public override ChartDataTable? ToDataTable() =>
        Orientation == BarOrientation.Horizontal
            ? this.WithRowKeys(HorizontalTable())
            : YData2 is { Length: > 0 } second
            ? this.WithRowKeys(ChartDataTable.FromNumberColumns(
                [new("x", Axis: DataAxis.X), new(Label ?? "y", Axis: DataAxis.Y), new("y2", Axis: DataAxis.Y)], [XData, YData, second]))
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
        if (Orientation == BarOrientation.Horizontal) return ToDataTable();

        if (ColumnRuns(names) is { } columns)
        {
            var rows = columns.Select(column => (IReadOnlyList<DataCell>)
                [DataCell.FromNumber(column.Centre), DataCell.FromNumber(column.Left), DataCell.FromNumber(column.Right), DataCell.FromNumber(column.Height)]);
            return new ChartDataTable(null,
                [new("x", Axis: DataAxis.X), new("left", Axis: DataAxis.X), new("right", Axis: DataAxis.X), new(Label ?? "y")], [.. rows]);
        }

        if (Level() is not { } level || names.Along(DataAxis.Y, [level]) is not [{ } name])
        {
            return ToDataTable();
        }

        double[] heights = [.. YData.Zip(YData2!, (top, bottom) => top - bottom)];
        return ChartDataTable.FromNumberColumns([new("x", Axis: DataAxis.X), new(Label ?? name)], [XData, heights]);
    }

    /// <summary>The table of a horizontal fill: the positions along Y, then the curve (or curves) on X.</summary>
    private ChartDataTable HorizontalTable() => XData2 is { Length: > 0 } second
        ? ChartDataTable.FromNumberColumns(
            [new("y", Axis: DataAxis.Y), new(Label ?? "x1", Axis: DataAxis.X), new("x2", Axis: DataAxis.X)], [YData, XData, second])
        : ChartDataTable.FromNumberColumns([new("y", Axis: DataAxis.Y), new(Label ?? "x", Axis: DataAxis.X)], [YData, XData]);

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
