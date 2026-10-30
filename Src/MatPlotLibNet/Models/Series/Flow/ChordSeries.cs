// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A chord diagram: the flows between the groups of a square matrix, each group an arc of a circle as long as the
/// flows it sends, each pair of groups with a flow between them a ribbon that leaves one arc, bends through the centre and
/// arrives at the other. A flow of a group to itself is a loop on its own arc. The ribbon wears the colour of the group that
/// sends more of the two. A sibling of <see cref="SankeySeries"/> for flows that go round instead of along; the geometry is
/// <c>ChordLayout</c>'s, with the ribbons inside an arc ordered so that a complete graph crosses as few times as any layout can.
/// <para>It fills its plot without axes around it. The matrix is square, finite, not negative and not all zero; the table is the
/// matrix as source, target and value.</para></summary>
public sealed class ChordSeries : ChartSeries
{
    private const double RestingRibbonAlpha = 0.67;

    private double _padding = ChordLayout.DefaultPadding;
    private double _ribbonAlpha = RestingRibbonAlpha;

    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <inheritdoc />
    internal override int ColorSlotSpan => Math.Max(1, Names.Length);

    /// <summary>The name of each group, one for each row of <see cref="Flows"/>.</summary>
    public string[] Names { get; }

    /// <summary>The flow from the group of each row to the group of each column.</summary>
    public double[,] Flows { get; }

    /// <summary>The gap between neighbouring arcs, in radians; 0.05 unless said otherwise.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, not a number, or so large that the gaps alone fill
    /// the circle.</exception>
    public double Padding
    {
        get => _padding;
        set => _padding = value >= 0 && value * Names.Length < 2 * Math.PI
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Padding), value, "The gaps between the arcs are 0 or more, and together less than the circle.");
    }

    /// <summary>How opaque the ribbons are, from 0 (clear) to 1 (solid); 0.67 unless said otherwise, so a ribbon behind another shows.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double RibbonAlpha
    {
        get => _ribbonAlpha;
        set => _ribbonAlpha = value >= 0 && value <= 1
            ? value
            : throw new ArgumentOutOfRangeException(nameof(RibbonAlpha), value, "The opacity of a ribbon is between 0 and 1.");
    }

    /// <summary>The colour of each group, by position; <see langword="null"/> (the default) names none. A group past the end of
    /// the array, or every group when it is <see langword="null"/>, wears what the figure's theme hands out at its own
    /// position, as the slices of a pie do.</summary>
    public Color[]? Colors { get; set; }

    /// <summary>Initializes a chord diagram of the given groups and the flows between them. Built through <c>Axes.Chord</c>,
    /// which refuses a matrix that cannot be drawn.</summary>
    /// <param name="names">The name of each group.</param>
    /// <param name="flows">The flow from each group (row) to each group (column).</param>
    public ChordSeries(string[] names, double[,] flows)
    {
        Names = names;
        Flows = flows;
    }

    /// <summary>The arcs and ribbons of this diagram.</summary>
    internal ChordLayoutResult Layout() => ChordLayout.Compute(Flows, Padding);

    /// <inheritdoc />
    /// <remarks>An entry for every group that has a name, in the colour its arc and the ribbons it sends wear.</remarks>
    internal override IReadOnlyList<LegendPart> LegendParts =>
        [.. Names.Select((name, position) => new LegendPart(name, LegendSwatch.Patch, position)).Where(part => !string.IsNullOrEmpty(part.Label))];

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "chord",
        Categories = Names,
        ChordFlows = Flows.To2DList(),
        ChordPadding = Padding.OrNullAtRest(ChordLayout.DefaultPadding),
        Alpha = RibbonAlpha.OrNullAtRest(RestingRibbonAlpha),
        Colors = Colors?.ToList(),
    };

    /// <summary>Reconstructs a <see cref="ChordSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static ChordSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Chord(dto.Categories ?? [], dto.ChordFlows.From2DList());
        s.Padding = dto.ChordPadding ?? ChordLayout.DefaultPadding;
        s.RibbonAlpha = dto.Alpha ?? RestingRibbonAlpha;
        s.Colors = dto.Colors?.ToArray();
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <inheritdoc />
    /// <remarks>One row for each flow there is, row by row of the matrix: the group that sends it, the group that receives it
    /// and its size. A cell of zero is not a row.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new List<IReadOnlyList<DataCell>>();
        int count = Math.Min(Names.Length, Flows.GetLength(0));
        for (int source = 0; source < count; source++)
        {
            for (int target = 0; target < count; target++)
            {
                if (Flows[source, target] > 0)
                    rows.Add([DataCell.FromText(Names[source]), DataCell.FromText(Names[target]), DataCell.FromNumber(Flows[source, target])]);
            }
        }

        return new ChartDataTable(null, [new("source", DataColumnKind.Text), new("target", DataColumnKind.Text), new("value")], rows);
    }
}
