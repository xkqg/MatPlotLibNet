// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a funnel chart showing progressive reduction through stages (e.g., sales pipeline).</summary>
public sealed class FunnelSeries : ChartSeries, ISeriesKind<FunnelSeries>
{
    /// <inheritdoc />
    internal override int ColorSlotSpan => Math.Max(1, Values.Length);

    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <inheritdoc />
    /// <remarks>An entry for each stage that has a name (<see cref="Labels"/>), in the colour its bar is drawn in; a stage with no name
    /// has none, and the ones after it keep their colours. A funnel whose stages are all unnamed has the series' own name as its entry.</remarks>
    internal override IReadOnlyList<LegendPart> LegendPartsUnder(Theme theme) =>
        Labels.Take(Values.Length).AsLegendParts(Colors) is { Count: > 0 } parts ? parts : base.LegendPartsUnder(theme);

    /// <summary>The name of each stage, drawn centred on its bar, from the top stage to the bottom one.</summary>
    public string[] Labels { get; }
    /// <summary>The size of each stage, from top to bottom: the width of a bar is its value relative to the largest
    /// value. A stage whose value cannot be drawn (NaN or infinite) is skipped with its label.</summary>
    public double[] Values { get; }
    /// <summary>The colour of each stage's bar, by position; a stage past the end of the array, or every stage when
    /// this is <see langword="null"/>, takes the colour the figure's theme hands out at its position.</summary>
    public Color[]? Colors { get; set => Change(ref field, value); }

    /// <summary>Initializes a new <see cref="FunnelSeries"/> with the given stage labels and values.</summary>
    /// <param name="labels">Stage label for each slice.</param>
    /// <param name="values">Numeric value for each slice, in order from top to bottom.</param>
    public FunnelSeries(string[] labels, double[] values)
    {
        Labels = labels; Values = values;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    static string ISeriesKind<FunnelSeries>.Kind => "funnel";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        PieLabels = Labels, Values = Values,
        Colors = Colors?.ToList(),
    };

    /// <summary>The reader of a <see cref="FunnelSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static FunnelSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Funnel(dto.PieLabels ?? [], dto.Values ?? []);
        s.Colors = dto.Colors?.ToArray();
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The categories as text, then the values under the series' own label.</remarks>
    public override ChartDataTable? ToDataTable() => ChartDataTable.ByCategory(Labels, new NumberColumn(Label ?? "value", Values));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
