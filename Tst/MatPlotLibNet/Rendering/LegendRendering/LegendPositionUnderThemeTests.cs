// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>The place a legend stands under its theme is one rule, <c>Legend.PositionUnder</c>: the place the legend names, or, for
/// one left at <see cref="LegendPosition.Best"/>, the theme's own. Every reader of a legend's place asks it: the layout of the box,
/// the renderer's decision to collect the marks, the figure legend's place, and the room the constrained layout keeps for a legend
/// outside the plot (an axes' legend and a figure's).</summary>
public class LegendPositionUnderThemeTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0];

    private static Theme ThemeWithLegendAt(LegendPosition position) => new("legend-at-" + position, Theme.Default) { DefaultLegendPosition = position };

    [Theory]
    [InlineData(LegendPosition.Best, LegendPosition.UpperRight)]
    [InlineData(LegendPosition.UpperRight, LegendPosition.UpperRight)]
    [InlineData(LegendPosition.LowerLeft, LegendPosition.LowerLeft)]
    [InlineData(LegendPosition.OutsideRight, LegendPosition.OutsideRight)]
    public void ALegend_StandsWhereItIsNamed_OrWhereTheThemeSays_WhenItNamesNone(LegendPosition given, LegendPosition expected)
    {
        var legend = new Legend { Position = given };

        Assert.Equal(expected, legend.PositionUnder(Theme.MatplotlibClassic));
    }

    [Fact]
    public void ABestLegend_UnderAThemeThatLeavesItAtBest_IsBest()
    {
        Assert.Equal(LegendPosition.Best, new Legend().PositionUnder(Theme.MatplotlibV2));
    }

    [Theory]
    [InlineData(LegendPosition.LowerLeft, LegendPosition.LowerLeft)]
    [InlineData(LegendPosition.OutsideRight, LegendPosition.CenterRight)]
    [InlineData(LegendPosition.OutsideBottom, LegendPosition.LowerCenter)]
    public void OnTheFigure_ALegendThatNamesNone_StandsWhereTheThemeSays_OutsideAnEdgeIncluded(LegendPosition themeSays, LegendPosition onTheFigure)
    {
        var legend = new Legend { Title = "kept" };

        Assert.Equal(legend with { Position = onTheFigure }, legend.OnTheFigure(ThemeWithLegendAt(themeSays)));
    }

    private static Figure AxesLegend(Theme theme, LegendPosition position) =>
        Plt.Create().WithSize(800, 600).WithTheme(theme).ConstrainedLayout()
            .AddSubPlot(1, 1, 1, ax => ax.WithLegend(position).Plot(X, Y, s => s.Label = "a longer label"))
            .Build();

    private static Figure FigureLegend(Theme theme, LegendPosition position) =>
        Plt.Create().WithSize(800, 600).WithTheme(theme).ConstrainedLayout()
            .AddSubPlot(1, 1, 1, ax => ax.WithLegend(visible: false).Plot(X, Y, s => s.Label = "a longer label"))
            .FigLegend(legend => legend with { Position = position }).Build();

    private static SubPlotSpacing SpacingOf(Figure figure) => new ChartRenderer().FrameOf(figure, new RecordingRenderContext()).Spacing;

    /// <summary>An axes' legend at Best, under a theme whose place is outside the plot, keeps the room the constrained layout keeps
    /// for that very place named on the legend: the layout reads the place the box is drawn at.</summary>
    [Theory]
    [InlineData(LegendPosition.OutsideRight)]
    [InlineData(LegendPosition.OutsideLeft)]
    [InlineData(LegendPosition.OutsideTop)]
    [InlineData(LegendPosition.OutsideBottom)]
    public void TheConstrainedLayout_KeepsRoomForAnAxesLegend_AtTheOutsidePlaceTheThemeGives(LegendPosition place)
    {
        var named = SpacingOf(AxesLegend(Theme.Default, place));
        var byTheme = SpacingOf(AxesLegend(ThemeWithLegendAt(place), LegendPosition.Best));

        Assert.Equal(named, byTheme);
        Assert.NotEqual(SpacingOf(AxesLegend(Theme.Default, LegendPosition.Best)), byTheme);
    }

    /// <summary>The same for the cap the margin of that edge may grow to: a legend wider than the resting cap lifts it, whether the
    /// place was named on the legend or by the theme.</summary>
    [Fact]
    public void TheCapOfAnEdge_IsLiftedByAWideAxesLegend_AtTheOutsidePlaceTheThemeGives()
    {
        const string wide = "a very long descriptive label that is much wider than the cap the right margin rests at, by far";
        Figure Make(Theme theme, LegendPosition position) => Plt.Create().WithSize(1400, 600).WithTheme(theme).ConstrainedLayout()
            .AddSubPlot(1, 1, 1, ax => ax.WithLegend(position).Plot(X, Y, s => s.Label = wide)).Build();

        var named = SpacingOf(Make(Theme.Default, LegendPosition.OutsideRight));
        var byTheme = SpacingOf(Make(ThemeWithLegendAt(LegendPosition.OutsideRight), LegendPosition.Best));

        Assert.Equal(named, byTheme);
    }

    /// <summary>A figure legend that names no place stands, and keeps its room, where the theme says: a theme that puts a legend
    /// outside on the right gives the figure legend the room an explicit outside right gives it.</summary>
    [Theory]
    [InlineData(LegendPosition.OutsideRight)]
    [InlineData(LegendPosition.OutsideBottom)]
    public void TheConstrainedLayout_KeepsRoomForAFigureLegend_AtTheOutsidePlaceTheThemeGives(LegendPosition place)
    {
        var named = SpacingOf(FigureLegend(Theme.Default, place));
        var byTheme = SpacingOf(FigureLegend(ThemeWithLegendAt(place), LegendPosition.Best));

        Assert.Equal(named, byTheme);
        Assert.NotEqual(SpacingOf(FigureLegend(Theme.Default, LegendPosition.Best)), byTheme);
    }
}
