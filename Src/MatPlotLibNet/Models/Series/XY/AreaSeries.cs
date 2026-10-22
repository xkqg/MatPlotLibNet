// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a filled area series, rendering the region between a line and a baseline (or between two Y datasets).</summary>
public sealed class AreaSeries : XYSeries, IHasColor, IHasAlpha, IHasEdgeColor, ITableNamedByAxes
{
    public double[]? YData2 { get; set; }

    public Color? Color { get; set; }

    public double Alpha { get; set; } = 0.3;

    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    public double LineWidth { get; set; } = 1.5;

    public Color? FillColor { get; set; }

    public HatchPattern Hatch { get; set; } = HatchPattern.None;

    public Color? HatchColor { get; set; }

    public Color? EdgeColor { get; set; }

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

        // Both edges of the area: its top and its bottom, the second curve or the zero it is filled down to. Over what the Y
        // axis can place: a gap in the area (NaN) and an infinity are not part of its extent, and on a log axis, which has
        // no place for zero (the bottom is drawn at the plot's edge there), the range starts at the lowest value reached.
        var scale = context.YScale;
        bool toZero = YData2 is not { Length: > 0 };
        var y = toZero
            ? YData.DrawableRange(scale) is { } top ? top.Including(0, scale) : (Range1D?)null
            : new[] { YData, YData2! }.DrawableRange(scale);
        // fill_between with y2=0 and non-negative y1: sticky floor
        double? stickyYMin = toZero && y is { Lo: >= 0 } ? 0 : null;
        var x = XData.DrawableRange();
        return new(x?.Lo, x?.Hi, y?.Lo, y?.Hi,
            StickyXMin: x?.Lo, StickyXMax: x?.Hi, StickyYMin: stickyYMin);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "area",
        XData = XData, YData = YData, YData2 = YData2,
        Color = Color, Alpha = Alpha,
        LineStyle = LineStyle.ToString().ToLowerInvariant(),
        LineWidth = LineWidth,
        Smooth = Smooth ? true : null,
        SmoothResolution = Smooth && SmoothResolution != 10 ? SmoothResolution : null,
        // Null when unhatched, so an unhatched area emits no hatch bytes and its golden stays byte-identical.
        Hatch = Hatch != HatchPattern.None ? Hatch : null,
        HatchColor = HatchColor,
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
        var s = axes.FillBetween(dto.XData ?? [], dto.YData ?? [], dto.YData2);
        s.Color = dto.Color;
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        s.LineWidth = dto.LineWidth ?? 1.5;
        ChartSerializer.ApplyEnum<LineStyle>(dto.LineStyle, v => s.LineStyle = v);
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
        s.FillColor = dto.FillColor;
        s.EdgeColor = dto.EdgeColor;
        s.StepMode = dto.DrawStyle ?? DrawStyle.Default;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>A fill between two lines carries both of them; a fill to the baseline carries one.</remarks>
    public override ChartDataTable? ToDataTable() =>
        YData2 is { Length: > 0 } second
            ? ChartDataTable.FromNumberColumns(
                [new("x", Axis: DataAxis.X), new(Label ?? "y", Axis: DataAxis.Y), new("y2", Axis: DataAxis.Y)], [XData, YData, second])
            : base.ToDataTable();

    /// <inheritdoc />
    /// <remarks>A fill down to one level the Y axis names stands on that name as a whole: a ridge on its group's baseline.
    /// Its table is the x and the height of the fill above the level, row by row, under the series' label or, without
    /// one, the name; the level itself says nothing a row needs, and a height is no position. Any other area tables as
    /// <see cref="ToDataTable()"/> does.</remarks>
    ChartDataTable? ITableNamedByAxes.ToDataTable(AxisNames names)
    {
        if (Level() is not { } level || names.Along(DataAxis.Y, [level]) is not [{ } name])
        {
            return ToDataTable();
        }

        double[] heights = [.. YData.Zip(YData2!, (top, bottom) => top - bottom)];
        return ChartDataTable.FromNumberColumns([new("x", Axis: DataAxis.X), new(Label ?? name)], [XData, heights]);
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
