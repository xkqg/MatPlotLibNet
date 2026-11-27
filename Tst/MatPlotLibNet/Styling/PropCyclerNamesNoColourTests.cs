// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A cycler built with no colours leaves the colours to its theme, as one built with no widths leaves the width
/// (<see cref="PropCycler"/>, matplotlib's <c>cycler('linestyle', [...])</c> over the style's colours): the theme's cycle colours its
/// series, and the JSON writes the colours the cycler was given, none for a cycler given none, so a theme reads back as it was.
/// What the cycler hands out when asked for its colour directly stays the blue its builder documents.</summary>
public class PropCyclerNamesNoColourTests
{
    private static PropCycler DashesOnly() => new PropCyclerBuilder().WithLineStyles(LineStyle.Dashed, LineStyle.Dotted).Build();

    private static Theme ThemeWith(PropCycler cycler) => Theme.CreateFrom(Theme.ColorBlindSafe).WithPropCycler(cycler).Build();

    [Fact]
    public void ACyclerGivenNoColours_LeavesTheSlotsColourToTheThemesCycle()
    {
        var theme = ThemeWith(DashesOnly());

        for (int slot = 0; slot < 10; slot++)
        {
            Assert.Equal(Theme.ColorBlindSafe.CycleColors[slot % Theme.ColorBlindSafe.CycleColors.Length], theme.SeriesSlot(slot).CycleColor);
        }
    }

    [Fact]
    public void ACyclerGivenNoColours_StillHandsOutItsDashes()
    {
        var theme = ThemeWith(DashesOnly());

        Assert.Equal(LineStyle.Dashed, theme.SeriesSlot(0).CycledProperties!.Value.LineStyle);
        Assert.Equal(LineStyle.Dotted, theme.SeriesSlot(1).CycledProperties!.Value.LineStyle);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ASeriesUnderACyclerGivenNoColours_IsDrawnInTheThemesColours", () =>
        {
            var theme = ThemeWith(DashesOnly());
            return Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [0.0, 1])).ToSvg();
        }, Theme.ColorBlindSafe.CycleColors[0].ToHex()) { Absent = [Colors.Tab10Blue.ToHex()], Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void ACyclerGivenColours_HandsThemOut_AsItAlwaysDid()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green).Build();
        var theme = ThemeWith(cycler);

        Assert.Equal(Colors.Red, theme.SeriesSlot(0).CycleColor);
        Assert.Equal(Colors.Green, theme.SeriesSlot(1).CycleColor);
        Assert.Equal(Colors.Red, theme.SeriesSlot(2).CycleColor);
    }

    [Fact]
    public void AskedForItsColourDirectly_ACyclerGivenNone_HandsOutTheBlueItsBuilderDocuments()
    {
        Assert.Equal(Colors.Tab10Blue, DashesOnly()[0].Color);
        Assert.Equal(Colors.Tab10Blue, DashesOnly()[3].Color);
    }

    [Fact]
    public void ACyclerGivenNoColours_HasTheLengthItAlwaysHad()
    {
        Assert.Equal(2, DashesOnly().Length);
    }

    [Fact]
    public void TheCycleMapOfAThemeWithACyclerGivenNoColours_IsTheThemesCycle()
    {
        var theme = ThemeWith(DashesOnly());

        Assert.Equal(Theme.ColorBlindSafe.CycleColors.Length, theme.CycleMap.ColorCount);
        Assert.Equal(Theme.ColorBlindSafe.CycleColors[0], theme.CycleMap.GetColor(0));
    }

    [Fact]
    public void TheJsonOfACyclerGivenNoColours_WritesNoColours_AndReadsBackAsOneGivenNone()
    {
        var theme = ThemeWith(DashesOnly());
        var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [0.0, 1])).Build();

        string json = new ChartSerializer().ToJson(figure);
        using var document = JsonDocument.Parse(json);
        var cycler = document.RootElement.GetProperty("theme").GetProperty("propCycler");
        var back = new ChartSerializer().FromJson(json).Theme!;

        Assert.False(cycler.TryGetProperty("colors", out _));
        Assert.Equal(Theme.ColorBlindSafe.CycleColors[0], back.SeriesSlot(0).CycleColor);
        Assert.Equal(LineStyle.Dashed, back.SeriesSlot(0).CycledProperties!.Value.LineStyle);
    }

    [Fact]
    public void TheJsonOfACyclerGivenColours_WritesThemAsItAlwaysDid()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green).Build();
        var figure = Plt.Create().WithTheme(ThemeWith(cycler)).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [0.0, 1])).Build();

        string json = new ChartSerializer().ToJson(figure);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(2, document.RootElement.GetProperty("theme").GetProperty("propCycler").GetProperty("colors").GetArrayLength());
    }
}
