// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>A map colours each region by a value, read through a colour map, with a colour bar: a value for each feature
/// (<c>Values</c>, parallel to the features), the colour map and the normaliser the value goes through, a missing value in the
/// map's bad colour, and the colour bar's range from the values that can be drawn. <c>Choropleth</c> joins the values to the
/// built-in countries by their three-letter code.</summary>
[Collection("ChartSerializerGlobalState")]
public class GeoChoroplethTests
{
    private static GeoFeature Square(double x, string code) =>
        new(new GeoGeometry { Type = "Polygon", PositionRings = [[new(x, 0), new(x + 5, 0), new(x + 5, 5), new(x, 5), new(x, 0)]] },
            new Dictionary<string, string> { ["ADM0_A3"] = code });

    private static List<GeoFeature> ThreeSquares() => [Square(0, "AAA"), Square(10, "BBB"), Square(20, "CCC")];

    private static string[] Fills(GeoPolygonSeries series, Theme? theme = null)
    {
        string svg = Plt.Create().WithSize(600, 330).WithTheme(theme ?? Theme.Default).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree).AddSeries(series)).ToSvg();
        return MapSvg.PathFills(svg);
    }

    private static string Hex(Color color) => color.ToHex().ToUpperInvariant();

    // ---- the colour of a region --------------------------------------------------------------------------------------------

    [Fact]
    public void ARegionIsDrawnInTheColourMapsColour_ForItsValue()
    {
        var series = new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree) { Values = [0, 5, 10], ColorMap = ColorMaps.Viridis };

        Assert.Equal([Hex(ColorMaps.Viridis.GetColor(0)), Hex(ColorMaps.Viridis.GetColor(0.5)), Hex(ColorMaps.Viridis.GetColor(1))], Fills(series));
    }

    [Fact]
    public void AValueBeatsTheFillColour()
    {
        var series = new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree)
            { Values = [0, 5, 10], ColorMap = ColorMaps.Viridis, Color = Colors.Red };

        Assert.DoesNotContain(Hex(Colors.Red), Fills(series));
    }

    [Fact]
    public void WithNoValues_TheRegionsAreTheFillColour_AsBefore()
    {
        var series = new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree) { Color = Colors.Red };

        Assert.Equal([Hex(Colors.Red), Hex(Colors.Red), Hex(Colors.Red)], Fills(series));
    }

    [Fact]
    public void AMissingValue_IsDrawnInTheColourMapsBadColour()
    {
        var map = new ListedColorMap("two", [Colors.Blue, Colors.Green]) { BadColor = Colors.Gray };
        var series = new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree) { Values = [0, double.NaN, 10], ColorMap = map };

        Assert.Equal([Hex(Colors.Blue), Hex(Colors.Gray), Hex(Colors.Green)], Fills(series));
    }

    [Fact]
    public void ANormaliserShapesTheScale()
    {
        var plain = new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree) { Values = [1, 10, 100], ColorMap = ColorMaps.Viridis };
        var logarithmic = new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree)
            { Values = [1, 10, 100], ColorMap = ColorMaps.Viridis, Normalizer = new LogNormalizer() };

        Assert.NotEqual(Fills(plain)[1], Fills(logarithmic)[1]);
        Assert.Equal(Hex(ColorMaps.Viridis.GetColor(0.5)), Fills(logarithmic)[1]);
    }

    /// <summary>Under the matplotlib themes a <see cref="LogNormalizer"/> is matplotlib's <c>LogNorm</c>: 8 in 2 to 200 is
    /// <c>log(4) / log(100)</c> of the map, where the documented formula of the normaliser, which the library's own theme keeps, gives
    /// <c>log(7) / log(199)</c>.</summary>
    [Fact]
    public void ALogarithmicScale_IsMatplotlibsLogNorm_UnderTheMatplotlibThemes()
    {
        var series = new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree)
            { Values = [2, 8, 200], ColorMap = ColorMaps.Viridis, Normalizer = new LogNormalizer() };

        Assert.Equal(Hex(ColorMaps.Viridis.GetColor(Math.Log(4) / Math.Log(100))), Fills(series, Theme.MatplotlibV2)[1]);
        Assert.Equal(Hex(ColorMaps.Viridis.GetColor(Math.Log(7) / Math.Log(199))), Fills(series)[1]);
    }

    /// <summary>matplotlib's <c>LogNorm</c> takes the smallest value above 0 as <c>vmin</c> and masks 0: of 0, 2, 20 and 200, 20 stands at
    /// half the map.</summary>
    [Fact]
    public void ALogarithmicScale_RunsFromTheSmallestValueAboveZero_UnderTheMatplotlibThemes()
    {
        var series = new GeoPolygonSeries([.. ThreeSquares(), Square(30, "DDD")], GeoProjection.PlateCarree)
            { Values = [0, 2, 20, 200], ColorMap = ColorMaps.Viridis, Normalizer = new LogNormalizer() };

        Assert.Contains(Hex(ColorMaps.Viridis.GetColor(0.5)), Fills(series, Theme.MatplotlibV2));
        Assert.DoesNotContain(Hex(ColorMaps.Viridis.GetColor(0.5)), Fills(series));
    }

    [Fact]
    public void ValuesThatDoNotPairWithTheFeatures_AreRefused()
    {
        var series = new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree);

        var ex = Assert.Throws<ArgumentException>(() => series.Values = [1, 2]);

        Assert.Equal("Values", ex.ParamName);
        Assert.StartsWith(2.RefusalAgainst(3), ex.Message, StringComparison.Ordinal);
        Assert.Null(series.Values);
    }

    // ---- the colour bar ----------------------------------------------------------------------------------------------------

    [Fact]
    public void TheColourBarRange_IsTheValuesThatCanBeDrawn_AnInfinityAndAMissingValueLeftOut()
    {
        var series = new GeoPolygonSeries(ThreeSquares().Append(Square(30, "DDD")).ToList(), GeoProjection.PlateCarree)
            { Values = [1, double.PositiveInfinity, 3, double.NaN] };

        Assert.Equal(new MatPlotLibNet.Numerics.MinMaxRange(1, 3), series.GetColorBarRange());
    }

    [Fact]
    public void WithNoValuesThatCanBeDrawn_TheColourBarRangeIsZeroToOne()
    {
        var series = new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree) { Values = [double.NaN, double.NaN, double.NaN] };

        Assert.Equal(new MatPlotLibNet.Numerics.MinMaxRange(0, 1), series.GetColorBarRange());
    }

    [Fact]
    public void OneValueAlone_IsARangeOfOne_AsTheColoursAreMeasured()
    {
        var series = new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree) { Values = [4, 4, 4] };

        Assert.Equal(new MatPlotLibNet.Numerics.MinMaxRange(4, 5), series.GetColorBarRange());
    }

    [Fact]
    public void TheColourBar_IsDrawnWithItsTicks_OnAMapWhoseAxesAreHidden()
    {
        string svg = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree)
            .AddSeries(new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree) { Values = [0, 5, 10], ColorMap = ColorMaps.Viridis })
            .WithColorBar()).ToSvg();

        int bar = svg.IndexOf("class=\"colorbar\"", StringComparison.Ordinal);
        Assert.True(bar >= 0, "the colour bar is drawn");
        string tail = svg[bar..];
        foreach (string tick in new[] { ">0<", ">2<", ">4<", ">6<", ">8<", ">10<" })
            Assert.Contains(tick, tail);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  With nothing to back it the bar takes the range of its own limits (0 to 1) and the default map, as any colour bar does.
        new DrawnToken("AMapWithNoValues_BacksNoColourBar", () =>
        {
            var series = new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree);
            return Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.PlateCarree).AddSeries(series).WithColorBar()).ToSvg();
        }, "class=\"colorbar\"") { Absent = [">100<"] },
        new DrawnToken("TheColourBar_ReadsTheRegionsThatHaveValues_NotTheOceanBeneathThem", ColourBar, ">100<", ">200<"),
        new DrawnToken("TheColourBar_ReadsTheRegionsThatHaveValues_NotTheOceanBeneathThem_InTheirColour", ColourBar, Colors.Orange.ToHex())
            { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    /// <summary>The colour bar of a map whose regions have values, drawn over an ocean: the part of the markup from the bar on.</summary>
    private static string ColourBar()
    {
        var map = new ListedColorMap("two", [Colors.Blue, Colors.Orange]);
        string svg = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree)
            .Ocean(Colors.Gray)
            .AddSeries(new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree) { Values = [100, 150, 200], ColorMap = map })
            .WithColorBar()).ToSvg();

        return svg[svg.IndexOf("class=\"colorbar\"", StringComparison.Ordinal)..];
    }

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ---- the join ----------------------------------------------------------------------------------------------------------

    [Fact]
    public void Choropleth_JoinsTheValuesToTheCountries_ByTheirThreeLetterCode()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.Robinson)
            .Choropleth(new Dictionary<string, double> { ["FRA"] = 1.5, ["NOR"] = 2.5 })).Build();

        var series = Assert.IsType<GeoPolygonSeries>(Assert.Single(figure.SubPlots[0].Series));

        Assert.Equal(series.Features.Count, series.Values!.Length);
        Assert.Equal(1.5, series.Values[series.Features.FindIndex(f => f.Properties["ADM0_A3"] == "FRA")]);
        Assert.Equal(2.5, series.Values[series.Features.FindIndex(f => f.Properties["ADM0_A3"] == "NOR")]);
        Assert.Equal(2, series.Values.Count(v => !double.IsNaN(v)));
    }

    [Fact]
    public void ACodeThatNoCountryHas_IsIgnored()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.Robinson)
            .Choropleth(new Dictionary<string, double> { ["FRA"] = 1, ["SGP"] = 9, ["XXX"] = 3 })).Build();

        var series = (GeoPolygonSeries)figure.SubPlots[0].Series[0];

        Assert.Equal(1, series.Values!.Count(v => !double.IsNaN(v)));
    }

    [Fact]
    public void Choropleth_ByAPropertyOfTheFeatures_ReadsItsNumbers()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.Robinson).Choropleth("POP_EST")).Build();

        var series = (GeoPolygonSeries)figure.SubPlots[0].Series[0];
        int france = series.Features.FindIndex(f => f.Properties["ADM0_A3"] == "FRA");

        Assert.True(series.Values![france] > 60_000_000);
    }

    [Fact]
    public void Choropleth_NeedsTheAxesProjection_AsEveryLayerDoes()
    {
        Assert.Throws<InvalidOperationException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Choropleth(new Dictionary<string, double> { ["FRA"] = 1 })).Build());
    }

    [Fact]
    public void ANumberInAProperty_IsReadInvariantly_WhateverTheCulture()
    {
        var feature = new GeoFeature(Square(0, "AAA").Geometry, new Dictionary<string, string> { ["v"] = "1.5", ["w"] = "x", ["e"] = "1e3" });
        var before = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("nl-NL");
        try
        {
            Assert.Equal(1.5, feature.NumberOf("v"));
            Assert.Equal(1000, feature.NumberOf("e"));
            Assert.True(double.IsNaN(feature.NumberOf("w")));
            Assert.True(double.IsNaN(feature.NumberOf("absent")));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    // ---- JSON --------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheValuesTheColourMapAndTheNormaliser_ComeBackFromJson()
    {
        var map = new ListedColorMap("custom", [Colors.Blue, Colors.Orange]) { BadColor = Colors.Gray };
        var figure = Plt.Create().WithSize(600, 330).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree)
            .AddSeries(new GeoPolygonSeries(ThreeSquares(), GeoProjection.PlateCarree)
                { Values = [1, double.NaN, 100], ColorMap = map, Normalizer = new LogNormalizer() })).Build();

        var back = Assert.IsType<GeoPolygonSeries>(figure.RoundTripped().SubPlots[0].Series[0]);

        Assert.Equal<double>([1, double.NaN, 100], back.Values!);
        Assert.IsType<LogNormalizer>(back.Normalizer);
        Assert.Equal(map.GetColor(0.3), back.ColorMap!.GetColor(0.3));
        Assert.Equal(map.BadColor, back.ColorMap.GetBadColor());
        Assert.Equal(figure.ToSvg(), figure.RoundTripped().ToSvg());
    }

    [Fact]
    public void AChoropleth_ComesBackAsTheSamePicture()
    {
        var figure = Plt.Create().WithSize(600, 330).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.Robinson)
            .Choropleth(new Dictionary<string, double> { ["FRA"] = 1, ["NOR"] = 4, ["BRA"] = 9 }, ColorMaps.Plasma)
            .WithColorBar()).Build();

        Assert.Equal(figure.ToSvg(), figure.RoundTripped().ToSvg());
    }
}
