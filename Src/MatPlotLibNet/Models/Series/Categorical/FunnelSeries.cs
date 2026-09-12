// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a funnel chart showing progressive reduction through stages (e.g., sales pipeline).</summary>
public sealed class FunnelSeries : ChartSeries
{
    public string[] Labels { get; }
    public double[] Values { get; }
    public Color[]? Colors { get; set; }

    /// <summary>Initializes a new <see cref="FunnelSeries"/> with the given stage labels and values.</summary>
    /// <param name="labels">Stage label for each slice.</param>
    /// <param name="values">Numeric value for each slice, in order from top to bottom.</param>
    public FunnelSeries(string[] labels, double[] values)
    {
        Labels = labels; Values = values;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "funnel",
        PieLabels = Labels, Values = Values
    };

    /// <summary>Reconstructs a <see cref="FunnelSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static FunnelSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Funnel(dto.PieLabels ?? [], dto.Values ?? []);
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The categories as text, then the values under the series' own label.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int count = Math.Min(Labels.Length, Values.Length);
        var rows = new IReadOnlyList<DataCell>[count];
        for (int i = 0; i < count; i++)
        {
            rows[i] = [DataCell.FromText(Labels[i]), DataCell.FromNumber(Values[i])];
        }

        return new ChartDataTable(null, [new("category", DataColumnKind.Text), new(Label ?? "value")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
