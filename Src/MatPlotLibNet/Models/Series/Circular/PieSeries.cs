// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a pie chart series displaying proportional data as circular slices.</summary>
public sealed class PieSeries : ChartSeries
{
    /// <inheritdoc />
    internal override int ColorSlotSpan => Math.Max(1, Sizes.Length);

    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>The size of each slice, in any unit: a slice covers its share of the total. A slice that cannot be
    /// drawn (NaN or infinite) counts as zero and draws no wedge.</summary>
    public double[] Sizes { get; }

    /// <summary>The text printed outside each wedge, by position; <see langword="null"/>, or a null entry, draws no
    /// label for that wedge.</summary>
    public string[]? Labels { get; set; }

    /// <summary>The colour of each wedge, by position; <see langword="null"/> (the default) names none. A wedge past the
    /// end of the array, or every wedge when it is <see langword="null"/>, wears what the figure's theme hands out at the
    /// wedge's own position, as a donut's and a funnel's do. Drawing never writes into this property: it holds what was set.</summary>
    public Color[]? Colors { get; set; }

    /// <summary>The angle in degrees at which the first wedge begins, measured counter-clockwise from three o'clock;
    /// the default 90 starts at the top.</summary>
    public double StartAngle { get; set; } = 90;

    /// <summary>When <see langword="true"/> the wedges follow each other counter-clockwise; the default
    /// <see langword="false"/> goes clockwise.</summary>
    public bool CounterClockwise { get; set; }

    /// <summary>How far each wedge is pulled out from the centre, as a fraction of the radius, by position; a wedge
    /// past the end of the array stays in place. <see langword="null"/> pulls none out.</summary>
    public double[]? Explode { get; set; }

    /// <summary>A .NET format string for the percentage printed inside each wedge, such as <c>"{0:F1}%"</c>, where
    /// <c>{0}</c> is the wedge's share of the total as a number from 0 to 100. <see langword="null"/> (the default)
    /// prints no percentages.</summary>
    public string? AutoPct { get; set; }

    /// <summary>When <see langword="true"/> a soft dark copy of each wedge is drawn slightly offset behind it. Default
    /// <see langword="false"/>.</summary>
    public bool Shadow { get; set; }

    /// <summary>The radius of the pie as a fraction of half the shorter side of the plot; <see langword="null"/> (the
    /// default) uses 0.8 of it, which leaves room for the labels.</summary>
    public double? Radius { get; set; }

    /// <summary>The hatch pattern of each wedge, by position; a wedge past the end of the array, or every wedge when
    /// this is <see langword="null"/>, is left plain.</summary>
    public HatchPattern[]? Hatches { get; set; }

    /// <summary>Initializes a new instance of <see cref="PieSeries"/> with the specified slice sizes.</summary>
    /// <param name="sizes">The numeric sizes of each pie slice.</param>
    public PieSeries(double[] sizes)
    {
        Sizes = sizes;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "pie",
        Sizes = Sizes, PieLabels = Labels,
        Colors = Colors?.ToList(),
        StartAngle = StartAngle.OrNullAtRest(90.0),
        CounterClockwise = CounterClockwise ? true : null,
        Explode = Explode,
        AutoPct = AutoPct,
        Shadow = Shadow ? true : null,
        Radius = Radius,
        Hatches = Hatches?.ToList(),
    };

    /// <summary>Reconstructs a <see cref="PieSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static PieSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Pie(dto.Sizes ?? [], dto.PieLabels);
        s.Colors = dto.Colors?.ToArray();
        if (dto.StartAngle.HasValue)
        {
            s.StartAngle = dto.StartAngle.Value;
        }
        s.CounterClockwise = dto.CounterClockwise ?? false;
        s.Explode = dto.Explode;
        s.AutoPct = dto.AutoPct;
        s.Shadow = dto.Shadow ?? false;
        s.Radius = dto.Radius;
        s.Hatches = dto.Hatches?.ToArray();
        return s;
    }


    /// <inheritdoc />
    /// <remarks>One row per slice: its label - or "slice N" when the series carries none - and its size.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new IReadOnlyList<DataCell>[Sizes.Length];
        for (int i = 0; i < Sizes.Length; i++)
        {
            string label = Labels is { } labels && i < labels.Length
                ? labels[i]
                : "slice " + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            rows[i] = [DataCell.FromText(label), DataCell.FromNumber(Sizes[i])];
        }

        return new ChartDataTable(null, [new("label", DataColumnKind.Text), new(Label ?? "size")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
