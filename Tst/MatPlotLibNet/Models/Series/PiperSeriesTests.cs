// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A Piper diagram reads the major ions of a water on three panels at once: the cations on a triangle (calcium at the
/// bottom left, magnesium at the top, sodium and potassium at the bottom right), the anions on a second triangle beside it
/// (bicarbonate and carbonate at the bottom left, sulphate at the top, chloride at the bottom right, shifted right by 1.2 of
/// the triangle's side), and both on a diamond between and above them. A laboratory delivers mg/L; the diagram is of percent of
/// the milliequivalents per litre, each triangle closed on its own sum, and a water whose charges do not balance is flagged.
/// The vectors are worked out by hand from the constants of the council's hydrogeochemist and cross-checked against the
/// R package <c>hydrogeo</c>.</summary>
public class PiperSeriesTests
{
    private static readonly double Height = Math.Sqrt(3) / 2;

    /// <summary>P3: one milliequivalent of every ion but potassium and carbonate.</summary>
    private static WaterSample OneEach => new() { Ca = 20.039, Mg = 12.1525, Na = 22.98977, HCO3 = 61.0168, SO4 = 48.0313, Cl = 35.453 };

    /// <summary>P4: seawater.</summary>
    private static WaterSample Seawater => new() { Cl = 19350, Na = 10760, SO4 = 2710, Mg = 1290, Ca = 411, K = 399, HCO3 = 142 };

    /// <summary>P5: a calcium bicarbonate water.</summary>
    private static WaterSample CalciumBicarbonate => new() { Ca = 60, Mg = 10, Na = 8, K = 2, HCO3 = 200, SO4 = 20, Cl = 10 };

    /// <summary>P6: a sodium chloride brine.</summary>
    private static WaterSample Brine => new() { Ca = 400, Mg = 100, Na = 5000, K = 50, HCO3 = 150, SO4 = 300, Cl = 8000 };

    /// <summary>P7: a sodium bicarbonate water whose charges do not balance.</summary>
    private static WaterSample SodiumBicarbonate => new() { Ca = 10, Mg = 3, Na = 300, K = 5, HCO3 = 600, SO4 = 30, Cl = 40 };

    // ── the frame ───────────────────────────────────────────────────────────────

    [Fact]
    public void TheFrame_IsTwoPointTwoWide_ByTwoPointTwoHeightsHigh()
    {
        Assert.Equal(0.2, PiperFrame.Gap);
        Assert.Equal(2.2, PiperFrame.Width, 12);
        Assert.Equal(2.2 * Height, PiperFrame.Height, 12);
    }

    [Fact]
    public void P1_ThePureIons_StandAtTheCornersOfTheirTriangles()
    {
        // Cations (Ca, Mg, Na+K) then anions (HCO3+CO3, SO4, Cl) at 100 %.
        var ca = new PiperShares(100, 0, 0, 100, 0, 0, 0);
        var mg = new PiperShares(0, 100, 0, 0, 100, 0, 0);
        var naK = new PiperShares(0, 0, 100, 0, 0, 100, 0);

        Assert.Equal(new Point(0, 0), ca.CationPlace());
        Assert.Equal(0.5, mg.CationPlace().X, 12);
        Assert.Equal(Height, mg.CationPlace().Y, 12);
        Assert.Equal(new Point(1, 0), naK.CationPlace());

        Assert.Equal(new Point(1.2, 0), ca.AnionPlace());
        Assert.Equal(1.7, mg.AnionPlace().X, 12);
        Assert.Equal(Height, mg.AnionPlace().Y, 12);
        Assert.Equal(2.2, naK.AnionPlace().X, 12);
        Assert.Equal(0.0, naK.AnionPlace().Y, 12);
    }

    [Theory]
    [InlineData(0, 0, 1.1, 0.173205)]    // P2: the bottom vertex of the diamond
    [InlineData(1, 0, 0.6, 1.039230)]    // the left vertex
    [InlineData(0, 1, 1.6, 1.039230)]    // the right vertex
    [InlineData(1, 1, 1.1, 1.905256)]    // the top vertex
    [InlineData(0.5, 0.5, 1.1, 1.039230)]  // the centre
    public void P2_TheDiamond_StandsOnItsVertices(double calciumAndMagnesium, double sulphateAndChloride, double x, double y)
    {
        // A = (Ca + Mg) / cations, B = (SO4 + Cl) / anions, both as fractions of their triangle.
        var shares = new PiperShares(calciumAndMagnesium * 100, 0, (1 - calciumAndMagnesium) * 100, (1 - sulphateAndChloride) * 100, sulphateAndChloride * 100, 0, 0);

        var at = shares.DiamondPlace();

        Assert.Equal(x, at.X, 2e-6);
        Assert.Equal(y, at.Y, 2e-6);
    }

    // ── from mg/L to the diagram ────────────────────────────────────────────────

    [Fact]
    public void P3_OneMilliequivalentOfEach_IsAThirdOfEachTriangle_AndBalances()
    {
        var shares = OneEach.ToShares();

        Assert.Equal(100.0 / 3, shares.Ca, 3);
        Assert.Equal(100.0 / 3, shares.Mg, 3);
        Assert.Equal(100.0 / 3, shares.NaK, 3);
        Assert.Equal(100.0 / 3, shares.HCO3CO3, 3);
        Assert.Equal(100.0 / 3, shares.SO4, 3);
        Assert.Equal(100.0 / 3, shares.Cl, 3);
        Assert.Equal(0.0, shares.ChargeBalance, 3);
        Assert.Equal(0.5, shares.CationPlace().X, 2e-6);
        Assert.Equal(0.288675, shares.CationPlace().Y, 2e-6);
        Assert.Equal(1.7, shares.AnionPlace().X, 2e-6);
        Assert.Equal(0.288675, shares.AnionPlace().Y, 2e-6);
        Assert.Equal(1.1, shares.DiamondPlace().X, 2e-6);
        Assert.Equal(1.327906, shares.DiamondPlace().Y, 2e-6);
    }

    [Fact]
    public void P4_Seawater_IsMostlySodiumChloride()
    {
        var shares = Seawater.ToShares();

        Assert.Equal(3.391, shares.Ca, 3);
        Assert.Equal(17.549, shares.Mg, 3);
        Assert.Equal(79.061, shares.NaK, 3);
        Assert.Equal(0.385, shares.HCO3CO3, 3);
        Assert.Equal(9.333, shares.SO4, 3);
        Assert.Equal(90.282, shares.Cl, 3);
        Assert.Equal(0.03, shares.ChargeBalance, 2);
        Assert.Equal(0.878351, shares.CationPlace().X, 1e-5);
        Assert.Equal(0.151975, shares.CationPlace().Y, 1e-5);
        Assert.Equal(2.149486, shares.AnionPlace().X, 1e-5);
        Assert.Equal(0.080826, shares.AnionPlace().Y, 1e-5);
        Assert.Equal(1.493379, shares.DiamondPlace().X, 1e-5);
        Assert.Equal(1.217235, shares.DiamondPlace().Y, 1e-5);
    }

    [Fact]
    public void P5_ACalciumBicarbonateWater_StandsLowOnTheLeft()
    {
        var shares = CalciumBicarbonate.ToShares();

        Assert.Equal(71.016, shares.Ca, 3);
        Assert.Equal(19.517, shares.Mg, 3);
        Assert.Equal(9.467, shares.NaK, 3);
        Assert.Equal(82.434, shares.HCO3CO3, 3);
        Assert.Equal(10.472, shares.SO4, 3);
        Assert.Equal(7.094, shares.Cl, 3);
        Assert.Equal(2.929, shares.ChargeBalance, 3);
        Assert.Equal(0.192253, shares.CationPlace().X, 1e-5);
        Assert.Equal(0.169023, shares.CationPlace().Y, 1e-5);
        Assert.Equal(1.323298, shares.AnionPlace().X, 1e-5);
        Assert.Equal(0.090691, shares.AnionPlace().Y, 1e-5);
        Assert.Equal(0.735163, shares.DiamondPlace().X, 1e-5);
        Assert.Equal(1.109370, shares.DiamondPlace().Y, 1e-5);
    }

    [Fact]
    public void P6_ASodiumChlorideBrine_StandsOnTheRightOfTheDiamond()
    {
        var shares = Brine.ToShares();

        Assert.Equal(2.618, shares.ChargeBalance, 3);
        Assert.Equal(1.537681, shares.DiamondPlace().X, 1e-5);
        Assert.Equal(1.129002, shares.DiamondPlace().Y, 1e-5);
    }

    [Fact]
    public void P7_AWaterThatDoesNotBalance_IsFlagged_NotDropped()
    {
        var shares = SodiumBicarbonate.ToShares();

        Assert.Equal(9.161, shares.ChargeBalance, 3);
        Assert.Equal(1.148858, shares.DiamondPlace().X, 1e-5);
        Assert.Equal(0.350619, shares.DiamondPlace().Y, 1e-5);
        Assert.True(shares.IsUnbalanced());
        Assert.False(OneEach.ToShares().IsUnbalanced());
        Assert.False(CalciumBicarbonate.ToShares().IsUnbalanced());   // 2.9 % is within five
    }

    [Fact]
    public void PotassiumJoinsSodium_AndCarbonateJoinsBicarbonate()
    {
        // 39.0983 mg/L of potassium and 22.98977 mg/L of sodium are one milliequivalent each: the same share of the triangle.
        var withPotassium = new WaterSample { Ca = 20.039, K = 39.0983, HCO3 = 61.0168 }.ToShares();
        var withSodium = new WaterSample { Ca = 20.039, Na = 22.98977, HCO3 = 61.0168 }.ToShares();
        Assert.Equal(withSodium, withPotassium);
        Assert.Equal(50.0, withPotassium.NaK, 9);

        // 30.00445 mg/L of carbonate and 61.0168 mg/L of bicarbonate are one milliequivalent each.
        var withCarbonate = new WaterSample { Ca = 20.039, Cl = 35.453, CO3 = 30.00445 }.ToShares();
        var withBicarbonate = new WaterSample { Ca = 20.039, Cl = 35.453, HCO3 = 61.0168 }.ToShares();
        Assert.Equal(withBicarbonate, withCarbonate);
        Assert.Equal(50.0, withCarbonate.HCO3CO3, 9);
    }

    [Fact]
    public void TheAmountsAreMilliequivalents_NotMilligrams()
    {
        // Equal masses are not equal charges: 100 mg/L of calcium is 4.99 meq/L, of magnesium 8.23 meq/L.
        var shares = new WaterSample { Ca = 100, Mg = 100, Cl = 100 }.ToShares();

        Assert.Equal(100.0 / 20.039 / (100.0 / 20.039 + 100.0 / 12.1525) * 100, shares.Ca, 9);
        Assert.True(shares.Mg > shares.Ca);
    }

    // ── what cannot be drawn ────────────────────────────────────────────────────

    [Theory]
    [InlineData(-1.0, "Ca")]
    [InlineData(double.NaN, "Ca")]
    [InlineData(double.PositiveInfinity, "Ca")]
    public void AnIonThatIsNegativeOrNotANumber_IsRefusedByItsRowAndItsName(double mgPerLitre, string ion)
    {
        var samples = new[] { OneEach, OneEach with { Ca = mgPerLitre } };

        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Axes().Piper(samples));

        Assert.Contains("Row 1", refusal.Message, StringComparison.Ordinal);
        Assert.Contains(ion, refusal.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("cations")]
    [InlineData("anions")]
    public void AWaterWithNoCationsOrNoAnions_IsRefusedByItsRow(string missing)
    {
        var bad = missing == "cations"
            ? new WaterSample { HCO3 = 100 }
            : new WaterSample { Ca = 100 };

        var refusal = Assert.Throws<ArgumentException>(() => new Axes().Piper([OneEach, bad]));

        Assert.Contains("Row 1", refusal.Message, StringComparison.Ordinal);
        Assert.Contains(missing, refusal.Message, StringComparison.Ordinal);
    }

    // ── the series ──────────────────────────────────────────────────────────────

    [Fact]
    public void ThePiperSeries_DrawsWithoutAxes_AndHoldsASlotForEachSample()
    {
        var series = new Axes().Piper([OneEach, Seawater, CalciumBicarbonate]);

        Assert.True(series.DrawsWithoutAxes);
        Assert.Equal(3, series.ColorSlotSpan);
        Assert.Equal(CoordinateSystem.Cartesian, new Axes().CoordinateSystem);
    }

    [Fact]
    public void EachLabelledSample_HasALegendEntry_InItsOwnColour()
    {
        var series = new Axes().Piper([OneEach with { Label = "well 1" }, Seawater, CalciumBicarbonate with { Label = "spring" }]);

        var parts = series.LegendParts;

        Assert.Equal(["well 1", "spring"], parts.Select(p => p.Label));
        Assert.Equal([0, 2], parts.Select(p => p.ColorSlotOffset));
    }

    [Fact]
    public void TheTable_HasTheSixPercentagesAndTheBalance_ForEachSample()
    {
        var table = new Axes().Piper([OneEach with { Label = "well 1" }, SodiumBicarbonate with { Label = "borehole" }]).ToDataTable();

        Assert.NotNull(table);
        Assert.Equal(["sample", "Ca %", "Mg %", "Na+K %", "HCO3+CO3 %", "SO4 %", "Cl %", "CBE %", "balance"], table!.Columns.Select(c => c.Header));
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("well 1", table.Rows[0][0].Text);
        Assert.Equal(100.0 / 3, table.Rows[0][1].Number, 3);
        Assert.Equal("balanced", table.Rows[0][8].Text);
        Assert.Equal(9.161, table.Rows[1][7].Number, 3);
        Assert.Equal("unbalanced", table.Rows[1][8].Text);   // flagged, not dropped
    }

    [Fact]
    public void ARangeIsContributedToNoAxis_BecauseTheDiagramFillsItsPlot()
    {
        var contribution = new Axes().Piper([OneEach]).ComputeDataRange(new NoLimits());

        Assert.Equal(new DataRangeContribution(null, null, null, null), contribution);
    }

    private sealed class NoLimits : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => default;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    [Fact]
    public void TheSamples_RoundTripThroughTheJson_WithTheirColoursAndMarkerSize()
    {
        var figure = new Figure { Width = 600, Height = 400 };
        var series = figure.AddSubPlot(1, 1, 1).Piper([OneEach with { Label = "well 1" }, Seawater with { Label = "sea", CO3 = 5 }]);
        series.Colors = [Color.FromHex("#C03030"), Color.FromHex("#3030C0")];
        series.MarkerSize = 9;

        var read = new ChartSerializer().FromJson(figure.ToJson());

        var back = Assert.IsType<PiperSeries>(Assert.Single(read.SubPlots[0].Series));
        Assert.Equal(series.Samples, back.Samples);
        Assert.Equal(series.Colors, back.Colors);
        Assert.Equal(9.0, back.MarkerSize);
    }

    [Fact]
    public void WhatIsAtRest_IsLeftOutOfTheJson()
    {
        var dto = new Axes().Piper([new WaterSample { Ca = 20, HCO3 = 60, Label = "x" }]).ToSeriesDto();

        Assert.Equal("piper", dto.Type);
        Assert.Null(dto.MarkerSize);
        Assert.Null(dto.Colors);
        var sample = Assert.Single(dto.WaterSamples!);
        Assert.Equal(20.0, sample.Ca);
        Assert.Null(sample.Mg);   // an ion that is not there is not written
        Assert.Null(sample.CO3);
    }

    [Fact]
    public void TheIonsAreWrittenUnderTheirOwnLowerCaseSymbols()
    {
        var figure = new Figure { Width = 600, Height = 400 };
        figure.AddSubPlot(1, 1, 1).Piper([new WaterSample { Ca = 60, HCO3 = 200, CO3 = 5, SO4 = 20, Cl = 10, Na = 8, K = 2 }]);

        string json = figure.ToJson();

        foreach (string name in new[] { "\"ca\":60", "\"na\":8", "\"k\":2", "\"hco3\":200", "\"co3\":5", "\"cl\":10", "\"so4\":20" })
            Assert.Contains(name, json, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePiperKind_IsInTheRegistry() => Assert.Contains("piper", SeriesRegistry.Discriminators);
}
