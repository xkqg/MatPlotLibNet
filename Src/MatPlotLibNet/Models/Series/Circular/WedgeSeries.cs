// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A chart of the slices of a whole, as matplotlib's <c>pie</c> draws its wedges whether they reach the centre (a pie,
/// <see cref="PieSeries"/>) or leave a hole (a donut, <see cref="DonutSeries"/>, matplotlib's <c>pie</c> with a wedge width): the sizes,
/// the name and the colour of each slice, the angle the first one starts at, the colour slot, the legend's entries and the data table.
/// Only the library derives from it.</summary>
public abstract class WedgeSeries : ChartSeries, IEdgedPatch
{
    /// <summary>Holds the sizes of the slices.</summary>
    /// <param name="sizes">The size of each slice.</param>
    private protected WedgeSeries(double[] sizes) => Sizes = sizes;

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>pie</c> (a donut is a <c>pie</c> with a wedge width): a colour of the patch cycle for each wedge; given
    /// its colours (<c>colors=</c>), none.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.PieWedge.NamingItsColours(Colors is not null);

    /// <inheritdoc />
    internal override int ColorSlotSpan => Math.Max(1, Sizes.Length);

    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <inheritdoc />
    /// <remarks>Edged as its wedges are under the matplotlib themes: the theme's edge, black under classic.</remarks>
    internal override LegendSwatch LegendSwatch => LegendSwatch.EdgedPatch;

    /// <inheritdoc />
    /// <remarks>An entry for each slice that has a name (<see cref="Labels"/>, by position), in the colour its wedge is drawn in, as
    /// matplotlib's legend lists a pie's wedges; a slice with no name has none, and the ones after it keep their colours. A chart whose
    /// slices are not named has the series' own name as its entry, and not a second row beside the slices' names.</remarks>
    internal override IReadOnlyList<LegendPart> LegendPartsUnder(Theme theme) =>
        (Labels ?? []).Take(Sizes.Length).AsLegendParts(Colors, LegendSwatch.EdgedPatch) is { Count: > 0 } parts ? parts : base.LegendPartsUnder(theme);

    /// <summary>The size of each slice, in any unit: a slice covers its share of the total. A slice that cannot be drawn (NaN or
    /// infinite) counts as zero and draws no wedge.</summary>
    public double[] Sizes { get; }

    /// <summary>The name of each slice, by position: the text a pie prints outside its wedge (a donut prints none beside its wedges),
    /// the slice's entry in the legend and its row in the data table. <see langword="null"/>, or a null entry, leaves that slice
    /// unnamed.</summary>
    public string[]? Labels { get; set => Change(ref field, value); }

    /// <summary>The colour of each wedge, by position; <see langword="null"/> (the default) names none. A wedge past the end of the
    /// array, or every wedge when it is <see langword="null"/>, wears what the figure's theme hands out at the wedge's own position.
    /// Drawing never writes into this property: it holds what was set.</summary>
    public Color[]? Colors { get; set => Change(ref field, value); }

    /// <summary>The angle in degrees at which the first wedge begins, measured counter-clockwise from three o'clock. The wedges follow in
    /// the order of <see cref="Sizes"/>. Given none, a chart starts where its theme starts one: at the top under the library's own
    /// themes (the value this reads), at three o'clock under the matplotlib themes (matplotlib's
    /// <c>startangle=0</c>); an angle that is given is drawn as given under every theme.</summary>
    public double StartAngle { get => WedgeLook.Library.StartAngleOver(GivenStartAngle); set => GivenStartAngle = value; }

    /// <summary>The value of <see cref="StartAngle"/> its author gave, <see langword="null"/> while none was.</summary>
    internal double? GivenStartAngle { get; private set => Change(ref field, value); }

    /// <summary>When <see langword="true"/> the wedges follow each other counter-clockwise from start angle, when
    /// <see langword="false"/> clockwise, a pie's and a donut's alike: matplotlib draws a donut as a pie whose wedges have a width
    /// (<c>pie(..., wedgeprops={'width': w})</c>), so the two follow one direction rule (its <c>counterclock</c>). Given none, a chart
    /// goes the way its theme goes: clockwise under the library's own themes (the value this reads), counter-clockwise under the
    /// matplotlib themes (<c>counterclock=True</c>); a direction that is given is drawn as given under every theme.</summary>
    public bool CounterClockwise { get => WedgeLook.Library.CounterClockwiseOver(GivenCounterClockwise); set => GivenCounterClockwise = value; }

    /// <summary>The value of <see cref="CounterClockwise"/> its author gave, <see langword="null"/> while none was.</summary>
    internal bool? GivenCounterClockwise { get; private set => Change(ref field, value); }

    /// <summary>The angle the first wedge starts at under <paramref name="theme"/>, in degrees: the one given, else the theme's
    /// (<see cref="WedgeLook.StartAngle"/>).</summary>
    /// <param name="theme">The theme the chart is drawn under.</param>
    /// <returns>The angle, counter-clockwise from three o'clock.</returns>
    internal double StartAngleUnder(Theme theme) => theme.Looks.Wedges.StartAngleOver(GivenStartAngle);

    /// <summary>Whether the wedges follow each other counter-clockwise under <paramref name="theme"/>: as given, else as the theme lays
    /// them (<see cref="WedgeLook.CounterClockwise"/>).</summary>
    /// <param name="theme">The theme the chart is drawn under.</param>
    /// <returns><see langword="true"/> for counter-clockwise.</returns>
    internal bool CounterClockwiseUnder(Theme theme) => theme.Looks.Wedges.CounterClockwiseOver(GivenCounterClockwise);

    /// <summary>The angle the wedge of slice <paramref name="index"/> sweeps under <paramref name="theme"/>, in radians counter-clockwise,
    /// negative where the wedges go clockwise (<see cref="CounterClockwiseUnder"/>): its share of the whole turn. The one rule a pie and
    /// a donut lay their wedges by.</summary>
    /// <param name="index">The slice.</param>
    /// <param name="total">The total of the slices that can be drawn.</param>
    /// <param name="theme">The theme the chart is drawn under.</param>
    /// <returns>The sweep, in radians.</returns>
    internal double SweepOf(int index, double total, Theme theme) => (CounterClockwiseUnder(theme) ? 1 : -1) * Sizes[index] / total * 2 * Math.PI;

    /// <inheritdoc />
    /// <remarks>None: a chart of slices draws without axes and asks no range of them.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    /// <summary>The fields every chart of slices writes: the sizes, the names, the colours, and the start angle and the direction where
    /// they were given (one not given stays not given through the JSON, so it is drawn as the theme lays it). Each kind adds its own name
    /// and the fields of its own.</summary>
    /// <returns>The fields.</returns>
    private protected SeriesDto WedgesOnTheWire() => new()
    {
        Sizes = Sizes,
        PieLabels = Labels,
        Colors = Colors?.ToList(),
        StartAngle = GivenStartAngle,
        CounterClockwise = GivenCounterClockwise,
    };

    /// <summary>Reads back what <see cref="WedgesOnTheWire"/> wrote besides the sizes and the names, which the verb takes: the colours,
    /// the start angle when one was written, and the direction.</summary>
    /// <param name="dto">The serialization DTO.</param>
    private protected void RestoreWedges(SeriesDto dto)
    {
        Colors = dto.Colors?.ToArray();
        if (dto.StartAngle is { } startAngle) StartAngle = startAngle;
        if (dto.CounterClockwise is { } counterClockwise) CounterClockwise = counterClockwise;
    }

    /// <inheritdoc />
    /// <remarks>One row per slice: its label - or "slice N" when the series carries none - and its size.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new IReadOnlyList<DataCell>[Sizes.Length];
        for (int i = 0; i < Sizes.Length; i++)
        {
            string label = Labels.NameOr(i, "slice ");
            rows[i] = [DataCell.FromText(label), DataCell.FromNumber(Sizes[i])];
        }

        return new ChartDataTable(null, [new("label", DataColumnKind.Text), new(Label ?? "size")], rows);
    }
}
