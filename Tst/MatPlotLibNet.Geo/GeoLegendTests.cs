// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The handle a map layer has in its legend is drawn in the colour the layer is drawn in, by the series that draws it: a
/// layer of lines (the coastlines) and a layer that only outlines its regions (the borders) are a line in the colour of their
/// stroke, as matplotlib's <c>HandlerLineCollection</c> draws a collection of lines; a layer that fills its regions is a rectangle
/// in its fill, the first region's colour for a choropleth, as <c>HandlerPolyCollection</c> takes the first face colour.</summary>
public class GeoLegendTests
{
    private static string Legend(Theme theme, Action<AxesBuilder> layers)
    {
        string svg = Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => layers(ax.WithProjection(GeoProjection.Robinson))).Build().ToSvg();
        return svg.GroupOf("legend");
    }

    private static string[] LineStrokes(string legend) =>
        [.. legend.Elements("line").Select(line => line["stroke"]).OfType<string>().Where(stroke => stroke.StartsWith('#'))];

    /// <summary>Measured before the change: the swatches were the cycle's first two colours (#1F77B4, #FF7F0E under v2) while
    /// the lines were drawn white and #888888.</summary>
    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheCoastlinesAndTheBorders_HaveALineInTheColourTheyAreDrawnIn(string style)
    {
        var theme = style.AsTheme();

        string legend = Legend(theme, ax => ax.Coastlines(Colors.White).Borders(Color.FromHex("#888888")));

        Assert.Equal(["#FFFFFF", "#888888"], LineStrokes(legend));
        Assert.Empty(MapSvg.HandleFills(legend));
    }

    [Fact]
    public void TheLine_IsAsWideAsTheLayersStroke()
    {
        string legend = Legend(Theme.MatplotlibV2, ax => ax.Coastlines(Colors.Black, lineWidth: 2.5));

        Assert.Contains(legend.Elements("line"), line => line["stroke-width"] == "2.5");
    }

    /// <summary>Borders that name no colour wear the theme's neutral grey, and so does their handle.</summary>
    [Fact]
    public void BordersThatNameNoColour_HaveALineInTheNeutralOfTheTheme()
    {
        string legend = Legend(Theme.Nord, ax => ax.Borders());

        Assert.Equal(["#8D8D8D"], LineStrokes(legend));
    }

    [Fact]
    public void ALayerThatFills_KeepsARectangleInItsFill()
    {
        string legend = Legend(Theme.MatplotlibV2, ax => ax.Ocean(Color.FromHex("#1A3A5C")).Land(Color.FromHex("#2D5A27")));

        Assert.Contains("#1A3A5C", MapSvg.HandleFills(legend));
        Assert.Contains("#2D5A27", MapSvg.HandleFills(legend));
        Assert.Empty(LineStrokes(legend));
    }

    /// <summary>A choropleth fills each region by its value; its handle is the first region's colour.</summary>
    [Fact]
    public void AChoropleth_HasARectangleInItsFirstRegionsColour()
    {
        var countries = Data.NaturalEarth110m.Countries();
        string first = countries[0].Properties["ADM0_A3"];
        var values = new Dictionary<string, double> { [first] = 0, ["FRA"] = 10 };

        string legend = Legend(Theme.MatplotlibV2, ax => ax.Choropleth(values, ColorMaps.Viridis));

        Assert.Equal([ColorMaps.Viridis.GetColor(0).ToHex()], MapSvg.HandleFills(legend));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ALayerWithNoLabel_HasNoRow", () => Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax.WithProjection(GeoProjection.Robinson).Choropleth(new Dictionary<string, double> { ["FRA"] = 1 },
                configure: s => s.Label = null)).Build().ToSvg()) { Absent = ["class=\"legend\""] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
