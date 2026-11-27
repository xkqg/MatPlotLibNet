// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>How wide a spine is drawn when its author gave it no width: matplotlib's <c>axes.linewidth</c>, which is in points
/// and which the library's matplotlib themes turn into pixels at the 100 dpi the references are drawn at, as they do for their font
/// sizes. matplotlib 3.11, <c>style.use('classic')</c>: <c>rcParams['axes.linewidth']</c> is 1.0; the default style: 0.8
/// (<c>matplotlib.spines.Spine.__init__</c> reads it). Every other theme keeps the 0.8 pixels the library has always drawn. A width
/// an author gave stays, whatever the theme says.</summary>
public class SpineWidthTests
{
    private const double PointsToPixels = 100.0 / 72.0;

    private static Figure Of(Theme theme, Func<SpinesConfig, SpinesConfig>? spines = null) => Plt.Create().WithSize(600, 400).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 10.0], [0.0, 10.0]).SetXLim(0, 10).SetYLim(0, 10);
            if (spines is not null) ax.WithSpines(spines);
        })
        .Build();

    /// <summary>The thickness each spine was drawn with, bottom, top, left, right as they are drawn.</summary>
    private static double[] SpineThicknesses(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        var widths = new List<double>();
        for (int i = 0; i < recording.Calls.Count - 1; i++)
        {
            if (recording.Calls[i] is not BeginGroupCall || recording.Calls[i + 1] is not DrawLineCall) continue;
            if (((BeginGroupCall)recording.Calls[i]).CssClass != "spine") continue;
            widths.Add(((StrokeCall)recording.Calls[i + 1]).Thickness);
        }

        return [.. widths];
    }

    [Fact]
    public void ATheme_ThatSaysNothing_DrawsItsSpinesPointEightPixelsWide()
    {
        Assert.Equal([0.8, 0.8, 0.8, 0.8], SpineThicknesses(Of(Theme.Default)));
    }

    /// <summary>The default style draws its spines 0.8 points wide, the classic style 1 point.</summary>
    [Theory]
    [InlineData("v2", 0.8)]
    [InlineData("classic", 1.0)]
    public void TheMatplotlibTheme_DrawsItsSpinesAsWideAsItsStyleSays(string theme, double points)
    {
        double expected = points * PointsToPixels;

        Assert.Equal([expected, expected, expected, expected], SpineThicknesses(Of(theme.AsTheme())));
    }

    [Theory]
    [InlineData("matplotlib-classic")]
    [InlineData("matplotlib-v2")]
    public void TheSpineRulesOfAMatplotlibTheme_SurviveJson(string name)
    {
        var theme = name.AsTheme();

        var again = Of(theme).RoundTripped().Theme;

        Assert.Equal(theme.Looks.Frame.SpineWidth, again.Looks.Frame.SpineWidth);
        Assert.True(again.Looks.Raster.StrokesOnPixelGrid);
    }

    [Fact]
    public void ASpineGivenAWidth_KeepsItUnderAnyTheme()
    {
        var figure = Of(Theme.MatplotlibClassic, s => s with { Left = s.Left with { LineWidth = 3 } });

        double[] widths = SpineThicknesses(figure);

        Assert.Equal([1.0 * PointsToPixels, 1.0 * PointsToPixels, 3, 1.0 * PointsToPixels], widths);
    }

    [Fact]
    public void ASpineGivenTheDefaultWidthItself_KeepsItUnderTheTheme()
    {
        var figure = Of(Theme.MatplotlibClassic, s => s with { Bottom = s.Bottom with { LineWidth = 0.8 } });

        Assert.Equal(0.8, SpineThicknesses(figure)[0]);
    }

    [Fact]
    public void ASpineGivenTheDefaultWidth_IsNotTheSpineGivenNone()
    {
        Assert.NotEqual(new SpineConfig(), new SpineConfig { LineWidth = 0.8 });
    }

    [Fact]
    public void TheSpinesOfAClassicFigure_KeepFollowingTheThemeThroughJson()
    {
        var figure = Of(Theme.MatplotlibClassic, s => s with { Top = s.Top with { Visible = false } });

        var again = figure.RoundTripped();

        Assert.Equal(SpineThicknesses(figure), SpineThicknesses(again));
        Assert.Equal(1.0 * PointsToPixels, SpineThicknesses(again)[0]);
    }

    [Fact]
    public void AWidthGivenToASpine_SurvivesJson()
    {
        var figure = Of(Theme.MatplotlibClassic, s => s with { Left = s.Left with { LineWidth = 0.8 } });

        var again = figure.RoundTripped();

        Assert.Equal(0.8, SpineThicknesses(again)[2]);
    }
}
