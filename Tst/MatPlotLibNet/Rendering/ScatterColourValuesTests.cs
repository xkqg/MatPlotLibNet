// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A scatter with a value for each point (<c>C</c>) and no colour map of its own colours its markers through the
/// theme's map, as matplotlib's <c>scatter(c=values)</c> does with <c>image.cmap</c> (matplotlib 3.11.2: <c>'viridis'</c> in
/// the default style, <c>'jet'</c> in the classic one). It used to draw every marker in the series colour, so the values showed
/// nothing.</summary>
public sealed class ScatterColourValuesTests
{
    private static string[] MarkerFills(Theme theme, Action<ScatterSeries> configure) =>
        [.. Plt.Create().WithTheme(theme).WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter([0.0, 1, 2], [0.0, 1, 2], configure)).ToSvg()
            .Elements("circle").Select(circle => circle["fill"]).OfType<string>().Where(fill => fill.StartsWith('#')).Select(fill => fill.ToUpperInvariant())];

    [Fact]
    public void ValuesAndNoMap_AreColouredInTheDefaultThemesMap()
    {
        string[] fills = MarkerFills(Theme.Default, s => s.C = [0.0, 1, 2]);

        Assert.Equal(
            [ColorMaps.Viridis.GetColor(0.0).ToHex().ToUpperInvariant(), ColorMaps.Viridis.GetColor(0.5).ToHex().ToUpperInvariant(), ColorMaps.Viridis.GetColor(1.0).ToHex().ToUpperInvariant()],
            fills);
    }

    /// <summary>The default style colours values in viridis; the classic style in jet (matplotlib 3.11.2:
    /// <c>colormaps['jet'](0.0, bytes=True)</c> is (0, 0, 127) and <c>(1.0, bytes=True)</c> is (127, 0, 0)).</summary>
    [Theory]
    [InlineData("v2", "#440154", "#FDE724")]
    [InlineData("classic", "#00007F", "#7F0000")]
    public void ValuesAndNoMap_AreColouredInTheMapOfTheMatplotlibTheme(string theme, string lowest, string highest)
    {
        string[] fills = MarkerFills(theme.AsTheme(), s => s.C = [0.0, 1, 2]);

        Assert.Equal(lowest, fills[0]);
        Assert.Equal(highest, fills[2]);
    }

    [Fact]
    public void AMapTheSeriesNames_Wins()
    {
        string[] fills = MarkerFills(Theme.MatplotlibClassic, s => { s.C = [0.0, 1, 2]; s.ColorMap = ColorMaps.Viridis; });

        Assert.Equal("#440154", fills[0]);
        Assert.Equal("#FDE724", fills[2]);
    }

    [Fact]
    public void APointColourArray_StillWinsOverTheValues()
    {
        string[] fills = MarkerFills(Theme.Default, s => { s.C = [0.0, 1, 2]; s.Colors = [Colors.Red, Colors.Red, Colors.Red]; });

        Assert.All(fills, fill => Assert.Equal(Colors.Red.ToHex().ToUpperInvariant(), fill));
    }

    [Fact]
    public void NoValues_KeepTheSeriesColour()
    {
        string[] fills = MarkerFills(Theme.MatplotlibClassic, s => { });

        Assert.Single(fills.Distinct());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // The density scatter promises a perceptually uniform map; it keeps naming one, so jet never colours its crowding under the
        // classic theme.
        new DrawnToken("ADensityScatter_StaysInViridisUnderTheClassicTheme", () => Plt.Create().WithTheme(Theme.MatplotlibClassic).WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.DensityScatter([0.01, 0.02, 0.03, 0.04, 10.0], [0.01, 0.02, 0.03, 0.04, 10.0])).ToSvg()) { Absent = ["#00007F"], Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
