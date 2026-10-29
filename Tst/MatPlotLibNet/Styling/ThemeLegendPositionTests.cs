// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The place a legend takes when nobody named one is the theme's (matplotlib's <c>legend.loc</c>): the classic
/// style puts it in the upper right, every other theme lets it look for room.</summary>
public class ThemeLegendPositionTests
{
    public static TheoryData<string> OtherThemes { get; } =
        new(typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Theme) && p.Name != nameof(Theme.MatplotlibClassic))
            .Select(p => p.Name));

    private static Theme BuiltIn(string name) => (Theme)typeof(Theme).GetProperty(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    [Fact]
    public void TheClassicTheme_PutsTheLegendInTheUpperRight()
        => Assert.Equal(LegendPosition.UpperRight, Theme.MatplotlibClassic.DefaultLegendPosition);

    [Theory]
    [MemberData(nameof(OtherThemes))]
    public void EveryOtherTheme_LetsTheLegendLookForRoom(string name)
        => Assert.Equal(LegendPosition.Best, BuiltIn(name).DefaultLegendPosition);

    [Fact]
    public void ADerivedTheme_KeepsTheLegendPositionOfItsBase()
    {
        var derived = Theme.CreateFrom(Theme.MatplotlibClassic).WithBackground(Colors.Black).Build();

        Assert.Equal(LegendPosition.UpperRight, derived.DefaultLegendPosition);
    }

    [Fact]
    public void TheLegendPosition_SurvivesTheFigureJson()
    {
        var read = ThemeDto.From(Theme.MatplotlibClassic).ToTheme();

        Assert.Equal(LegendPosition.UpperRight, read.DefaultLegendPosition);
    }

    [Fact]
    public void ATheme_ThatLetsTheLegendLookForRoom_WritesNoLegendPosition()
        => Assert.Null(ThemeDto.From(Theme.MatplotlibV2).DefaultLegendPosition);

    /// <summary>The line climbs into the upper right, where a legend that looks for room would not stay.</summary>
    private static Figure Climbing(Theme theme, LegendPosition position) => Plt.Create()
        .WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5], s => s.Label = "climb")
            .WithLegend(position))
        .Build();

    [Fact]
    public void ALegendLeftAtBest_UnderTheClassicTheme_StandsInTheUpperRightWhateverIsThere()
        => Assert.Equal(
            Climbing(Theme.MatplotlibClassic, LegendPosition.UpperRight).ToSvg(),
            Climbing(Theme.MatplotlibClassic, LegendPosition.Best).ToSvg());

    [Fact]
    public void ALegendLeftAtBest_UnderTheDefaultTheme_MovesOutOfTheWayOfTheLine()
        => Assert.NotEqual(
            Climbing(Theme.MatplotlibV2, LegendPosition.UpperRight).ToSvg(),
            Climbing(Theme.MatplotlibV2, LegendPosition.Best).ToSvg());

    [Fact]
    public void ALegendThatNamesItsPlace_UnderTheClassicTheme_StaysThere()
        => Assert.NotEqual(
            Climbing(Theme.MatplotlibClassic, LegendPosition.UpperRight).ToSvg(),
            Climbing(Theme.MatplotlibClassic, LegendPosition.LowerRight).ToSvg());
}
