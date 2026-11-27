// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The colour a verb chooses for what its caller named no colour for is the verb's, not a colour the caller gave: a cartogram
/// is drawn in the blue its verb chose under the library's own themes, and in the colour the theme's cycle gives under the matplotlib
/// themes, as a collection of patches takes the style's first colour; and the JSON says so (<c>verbColor</c>), not that the caller named
/// it (<c>color</c>), so the figure read back and drawn under another theme follows that theme. A colour the caller names wins
/// over both. Measured before the change: a cartogram's JSON carried <c>"color":"#1F77B4"</c> and it was drawn #1F77B4 under every theme,
/// where matplotlib's classic style draws a patch collection in <c>b</c> (#0000FF) and v2 in <c>C0</c>.</summary>
[Collection("ChartSerializerGlobalState")]
public class GeoVerbColourTests
{
    private static readonly IReadOnlyDictionary<string, double> Values = new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 2, ["ESP"] = 4 };

    private static Figure Cartogram(Theme theme, Action<GeoPolygonSeries>? configure = null) =>
        Plt.Create().WithSize(600, 330).WithTheme(theme).AddSubPlot(1, 1, 1, ax =>
            ax.WithProjection(GeoProjection.PlateCarree).Cartogram(Values, configure).WithLegend()).Build();

    private static string[] LegendFills(string svg)
    {
        return MapSvg.HandleFills(svg.GroupOf("legend"));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ACartogramsJson_SaysTheVerbChoseItsColour_NotThatTheCallerNamedIt", () => Cartogram(Theme.Default).ToJson(), "\"verbColor\":\"#1F77B4\"") { Absent = ["\"color\":\"#1F77B4\""] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void AColourTheCallerNames_IsGiven_AndWinsOverTheVerbsInTheJsonAndTheDrawing()
    {
        var figure = Cartogram(Theme.MatplotlibClassic, series => series.Color = Colors.Red);

        string json = figure.ToJson();
        string svg = figure.ToSvg();

        Assert.Contains("\"color\":\"#FF0000\"", json);
        Assert.Contains("#FF0000", MapSvg.PathFills(svg));
        Assert.DoesNotContain(Theme.MatplotlibClassic.CycleColors[0].ToHex(), MapSvg.PathFills(svg));
    }

    [Theory]
    [InlineData("default")]
    [InlineData("Nord")]
    [InlineData("dark")]
    public void UnderTheLibrarysThemes_ACartogramIsTheBlueItsVerbChose(string name)
    {
        var theme = name.AsTheme();

        var figure = Cartogram(theme);

        Assert.Contains("#1F77B4", MapSvg.PathFills(figure.ToSvg()));
        Assert.Equal(["#1F77B4"], LegendFills(figure.ToSvg()));
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void UnderTheMatplotlibThemes_ACartogramIsTheColourOfTheThemesCycle_AndSoIsItsLegendHandle(string style)
    {
        var theme = style.AsTheme();
        string cycle = theme.CycleColors[0].ToHex();

        string svg = Cartogram(theme).ToSvg();

        Assert.Contains(cycle, MapSvg.PathFills(svg));
        Assert.Equal([cycle], LegendFills(svg));
    }

    [Fact]
    public void AMapReadBackFromItsJson_FollowsTheThemeItIsDrawnUnder()
    {
        var back = Cartogram(Theme.Default).RoundTripped();
        back.Theme = Theme.MatplotlibClassic;

        Assert.Contains(Theme.MatplotlibClassic.CycleColors[0].ToHex(), MapSvg.PathFills(back.ToSvg()));
    }
}
