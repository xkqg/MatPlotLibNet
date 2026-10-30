// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A Piper diagram: the major ions of a set of waters on three panels at once, so a reader can see what kind of water
/// each one is and which are alike. The cations stand on a triangle (calcium at the bottom left, magnesium at the top, sodium
/// and potassium at the bottom right), the anions on a second triangle beside it (bicarbonate and carbonate at the bottom
/// left, sulphate at the top, chloride at the bottom right), and both on a diamond between and above them. A laboratory
/// delivers mg/L; the diagram is of percent of the milliequivalents per litre, each triangle closed on its own sum.
/// Each sample is a marker on each of the three panels, in one colour, so a reader follows a water from one to the next.
/// <para>The diagram fills the plot as a whole, at one scale, so the triangles are equilateral at any size of the figure, and
/// the axes around it draw no grid, ticks or frame. The table gives each sample's six percentages and its charge balance
/// error, and flags a water that does not balance.</para></summary>
public sealed class PiperSeries : ChartSeries
{
    private const double RestingMarkerSize = 6;

    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <inheritdoc />
    internal override int ColorSlotSpan => Math.Max(1, Samples.Count);

    /// <summary>The waters, in mg/L.</summary>
    public IReadOnlyList<WaterSample> Samples { get; }

    /// <summary>The colour of each sample, by position; <see langword="null"/> (the default) names none. A sample past the end
    /// of the array, or every sample when it is <see langword="null"/>, wears what the figure's theme hands out at its own
    /// position.</summary>
    public Color[]? Colors { get; set; }

    /// <summary>The diameter of a marker, in points; 6 unless said otherwise.</summary>
    public double MarkerSize { get; set; } = RestingMarkerSize;

    /// <summary>Initializes a Piper diagram of the given waters. Built through <c>Axes.Piper</c>, which refuses a water that has
    /// no place on the diagram.</summary>
    /// <param name="samples">The waters, in mg/L.</param>
    public PiperSeries(IReadOnlyList<WaterSample> samples) => Samples = samples;

    /// <inheritdoc />
    /// <remarks>An entry for every sample that has a name, as a marker in the colour the sample wears on all three panels.</remarks>
    internal override IReadOnlyList<LegendPart> LegendParts =>
        [.. Samples.Select((sample, position) => new LegendPart(sample.Label ?? "", new LegendSwatch(LegendSwatchKind.Marker, MarkerSize: MarkerSize), position))
            .Where(part => !string.IsNullOrEmpty(part.Label))];

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "piper",
        WaterSamples = [.. Samples.Select(WaterSampleDto.From)],
        Colors = Colors?.ToList(),
        MarkerSize = MarkerSize.OrNullAtRest(RestingMarkerSize),
    };

    /// <summary>Reconstructs a <see cref="PiperSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static PiperSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Piper([.. (dto.WaterSamples ?? []).Select(sample => sample.ToWaterSample())]);
        s.Colors = dto.Colors?.ToArray();
        if (dto.MarkerSize.HasValue)
        {
            s.MarkerSize = dto.MarkerSize.Value;
        }

        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <inheritdoc />
    /// <remarks>One row for each sample: its name, the six percentages the diagram is read by, the charge balance error, and
    /// whether the analysis balances (within <see cref="PiperFrame.ChargeBalanceTolerance"/> percent). A sample that does not is
    /// flagged here and still drawn.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new IReadOnlyList<DataCell>[Samples.Count];
        for (int i = 0; i < Samples.Count; i++)
        {
            var shares = Samples[i].ToShares();
            rows[i] =
            [
                DataCell.FromText(Samples[i].Label ?? $"sample {(i + 1).ToString(CultureInfo.InvariantCulture)}"),
                DataCell.FromNumber(shares.Ca), DataCell.FromNumber(shares.Mg), DataCell.FromNumber(shares.NaK),
                DataCell.FromNumber(shares.HCO3CO3), DataCell.FromNumber(shares.SO4), DataCell.FromNumber(shares.Cl),
                DataCell.FromNumber(shares.ChargeBalance),
                DataCell.FromText(shares.IsUnbalanced() ? "unbalanced" : "balanced"),
            ];
        }

        return new ChartDataTable(null,
        [
            new("sample", DataColumnKind.Text), new("Ca %"), new("Mg %"), new("Na+K %"), new("HCO3+CO3 %"), new("SO4 %"), new("Cl %"),
            new("CBE %"), new("balance", DataColumnKind.Text),
        ], rows);
    }

    /// <summary>Refuses a water that has no place on the diagram, named by its row: an amount that is negative, infinite or not
    /// a number, and a water with no cations or no anions, whose triangle has nothing to close on. A document read from JSON
    /// builds its series through the verb, so one with such a water is refused when it is read.</summary>
    /// <param name="samples">The waters.</param>
    /// <exception cref="ArgumentOutOfRangeException">An amount is negative, infinite or not a number; the message names the row and the ion.</exception>
    /// <exception cref="ArgumentException">A water has no cations or no anions; the message names the row.</exception>
    internal static void RefuseUnplaceable(IReadOnlyList<WaterSample> samples)
    {
        for (int row = 0; row < samples.Count; row++)
        {
            foreach (var amount in samples[row].Ions())
            {
                if (!double.IsFinite(amount.MgPerLitre) || amount.MgPerLitre < 0)
                    throw new ArgumentOutOfRangeException(amount.Ion, amount.MgPerLitre, $"Row {row}: the {amount.Ion} amount of a water is a concentration in mg/L, a finite number that is not negative.");
            }

            var shares = samples[row].ToShares();
            if (double.IsNaN(shares.Ca))
                throw new ArgumentException($"Row {row} has no cations (Ca, Mg, Na, K), so it has no place on the cation triangle.");
            if (double.IsNaN(shares.Cl))
                throw new ArgumentException($"Row {row} has no anions (HCO3, CO3, SO4, Cl), so it has no place on the anion triangle.");
        }
    }
}
