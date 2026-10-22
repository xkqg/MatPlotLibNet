// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a stem plot series displaying data as vertical lines with markers at the tips.</summary>
public sealed class StemSeries : ChartSeries
{
    public double[] XData { get; }

    public double[] YData { get; }

    public Color? MarkerColor { get; set; }

    public Color? StemColor { get; set; }

    public Color? BaselineColor { get; set; }

    public MarkerStyle Marker { get; set; } = RestingMarker;

    private const MarkerStyle RestingMarker = MarkerStyle.Circle;

    /// <summary>Initializes a new instance of <see cref="StemSeries"/> with the specified data.</summary>
    /// <param name="xData">The X-axis data values.</param>
    /// <param name="yData">The Y-axis data values.</param>
    public StemSeries(double[] xData, double[] yData)
    {
        XData = xData;
        YData = yData;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Over the drawable values only: a missing stem (NaN) and an infinity are not part of the extent.
        var x = XData.DrawableRange();
        double? xMin = x?.Lo, xMax = x?.Hi;
        if (YData.DrawableRange() is not { } raw)
        {
            return new(xMin, xMax, null, null);
        }

        // Stems stand on the y=0 baseline, so the baseline is a sticky edge of Y. Along X nothing is sticky: the first
        // and last stem are marks like any other and keep the axis margin, so their heads stay whole. (They used to be
        // sticky at both ends of the data, which put the end heads on the plot's frame, half of each outside it.)
        double? stickyYMin = raw.Lo >= 0 ? 0 : null;
        double? stickyYMax = raw.Hi <= 0 ? 0 : null;
        return new(xMin, xMax, Math.Min(raw.Lo, 0), Math.Max(raw.Hi, 0),
            StickyYMin: stickyYMin, StickyYMax: stickyYMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "stem", XData = XData, YData = YData,
        MarkerColor = MarkerColor,
        StemColor = StemColor,
        BaselineColor = BaselineColor,
        Marker = Marker.OrNullAtRest(RestingMarker),
    };

    /// <summary>Reconstructs a <see cref="StemSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static StemSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Stem(dto.XData ?? [], dto.YData ?? []);
        s.MarkerColor = dto.MarkerColor;
        s.StemColor = dto.StemColor;
        s.BaselineColor = dto.BaselineColor;
        s.Marker = dto.Marker ?? RestingMarker;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The stems' feet and heads.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.FromNumberColumns([new("x", Axis: DataAxis.X), new(Label ?? "y", Axis: DataAxis.Y)], [XData, YData]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
