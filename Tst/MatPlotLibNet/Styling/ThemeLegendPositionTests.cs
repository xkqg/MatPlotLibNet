// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The place a legend takes when nobody named one is the theme's (matplotlib's <c>legend.loc</c>): the classic
/// style puts it in the upper right, every other theme lets it look for room.</summary>
public class ThemeLegendPositionTests
{
    public static TheoryData<string> OtherThemes { get; } =
        new(typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Theme) && p.Name != nameof(Theme.MatplotlibClassic))
            .Select(p => p.Name));
    [Fact]
    public void TheClassicTheme_PutsTheLegendInTheUpperRight()
        => Assert.Equal(LegendPosition.UpperRight, Theme.MatplotlibClassic.DefaultLegendPosition);

    [Theory]
    [MemberData(nameof(OtherThemes))]
    public void EveryOtherTheme_LetsTheLegendLookForRoom(string name)
        => Assert.Equal(LegendPosition.Best, name.AsTheme().DefaultLegendPosition);

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

    /// <summary>A legend left at Best stands in the upper right under the classic theme, whatever is there, and moves out of the way of
    /// the line under the default one; a legend that names its place stays there under the classic theme too.</summary>
    [Theory]
    [InlineData("classic", LegendPosition.Best, true)]
    [InlineData("v2", LegendPosition.Best, false)]
    [InlineData("classic", LegendPosition.LowerRight, false)]
    public void ALegend_StandsInTheUpperRight_OnlyWhereTheThemeAndItsPlaceSaySo(string theme, LegendPosition position, bool upperRight)
    {
        Assert.Equal(upperRight, Climbing(theme.AsTheme(), LegendPosition.UpperRight).ToSvg() == Climbing(theme.AsTheme(), position).ToSvg());
    }
}
