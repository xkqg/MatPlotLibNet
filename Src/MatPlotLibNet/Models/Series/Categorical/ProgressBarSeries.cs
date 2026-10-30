// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a progress bar showing a value as a fraction of a track (0.0 to 1.0).</summary>
public sealed class ProgressBarSeries : ChartSeries
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>The fraction of the track that the bar fills, from 0 to 1 (the constructor clamps what it is given to
    /// that range); it is also printed on the bar as a percentage.</summary>
    public double Value { get; }

    /// <summary>The colour of the filled part of the bar. Default Tab10 blue.</summary>
    public Color FillColor { get; set; } = Colors.Tab10Blue;

    /// <summary>The colour of the unfilled track behind the bar. Default light gray.</summary>
    public Color TrackColor { get; set; } = Colors.LightGray;

    /// <summary>Height of the bar as a fraction of the height of the plot, centred vertically. Default 0.3.</summary>
    public double BarHeight { get; set; } = 0.3;

    /// <summary>Creates a new progress bar series with the given value (0.0 to 1.0).</summary>
    public ProgressBarSeries(double value) => Value = Math.Clamp(value, 0, 1);

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "progressbar",
        GaugeValue = Value, FillColor = FillColor,
        TrackColor = TrackColor, BarHeight = BarHeight
    };

    /// <summary>Reconstructs a <see cref="ProgressBarSeries"/> from its serialization DTO, including fill and track colours, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static ProgressBarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.ProgressBar(dto.GaugeValue ?? 0);
        if (dto.FillColor.HasValue)
        {
            s.FillColor = dto.FillColor.Value;
        }
        if (dto.TrackColor.HasValue)
        {
            s.TrackColor = dto.TrackColor.Value;
        }
        if (dto.BarHeight.HasValue)
        {
            s.BarHeight = dto.BarHeight.Value;
        }
        return s;
    }


    /// <inheritdoc />
    /// <remarks>One row: the fraction the bar fills.</remarks>
    public override ChartDataTable? ToDataTable() =>
        new(null,
            [new("label", DataColumnKind.Text), new(Label ?? "value")],
            [[DataCell.FromText(Label ?? ""), DataCell.FromNumber(Value)]]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
