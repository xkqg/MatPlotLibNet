// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a filled area series, rendering the region between a line and a baseline (or between two Y datasets).</summary>
public sealed class AreaSeries : XYSeries, IHasColor, IHasAlpha, IHasEdgeColor
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
    public Func<double, double, bool>? Where { get; set; }

    /// <summary>When <see langword="true"/>, applies Fritsch-Carlson monotone cubic interpolation to the top edge before filling.</summary>
    public bool Smooth { get; set; }

    /// <summary>Number of interpolated sub-points per input interval when <see cref="Smooth"/> is <see langword="true"/>. Default 10.</summary>
    public int SmoothResolution { get; set; } = 10;

    /// <summary>Creates a new area series from the given X and Y data.</summary>
    /// <remarks>ZOrder defaults to -1 so fills render behind all other series (ZOrder 0).</remarks>
    public AreaSeries(double[] xData, double[] yData) : base(xData, yData) { ZOrder = -1; }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Empty data: no contribution to axis range. Without this guard, .Min()/.Max()
        // throw "Sequence contains no elements" — caught by RendererEdgeCaseTests.
        if (YData.Length == 0) return new DataRangeContribution(null, null, null, null);

        double yMin = YData.Min(), yMax = YData.Max();
        double? stickyYMin = null;
        if (YData2 is not null && YData2.Length > 0)
        {
            yMin = Math.Min(yMin, YData2.Min());
            yMax = Math.Max(yMax, YData2.Max());
        }
        else if (0 <= yMin)
        {
            yMin = 0;
            stickyYMin = 0;  // fill_between with y2=0 and non-negative y1: sticky floor
        }
        double xMin = XData.Min(), xMax = XData.Max();
        return new(xMin, xMax, yMin, yMax,
            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: stickyYMin);
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
        HatchColor = HatchColor
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
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
