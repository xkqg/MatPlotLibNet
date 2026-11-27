// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Where a polar axes puts its spoke labels and its title. matplotlib 3.11.2, read with python:
/// <c>ThetaTick.update_position</c> centres a label (<c>ha='center', va='center'</c>) <c>pad = self._pad + 7</c> points out from the circle
/// along its angle, <c>_pad</c> being the tick's pad and, for a tick pointing out, its length: the 90 degree label's centre stands 19.44 px
/// (14 pt) above the circle under the default style and 15.28 px (11 pt) under the classic one, whose ticks point in. <c>set_title</c> puts the
/// title at <c>axes.titley</c>: None under the default style, so <c>_update_title_position</c> raises it over the theta labels' box (title
/// bottom 41.28 px for labels topping at 45.61 px, a 6 pt pad less the descent), 1.0 under the classic one, so it stands at the top of the
/// box (<c>PolarAxes.clear</c>'s <c>set_y(1.05)</c> never survives <c>set_title</c>). The library's own themes keep their look: the title
/// 0.05 of the circle's side above it, the labels where they always stood.</summary>
public class PolarTitleTests
{
    private const string Title = "Polar title";

    private static Figure Polar(Theme theme) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.WithTitle(Title).PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]))
        .Build();

    private static List<(string Text, Point At, TextAlignment Alignment)> Texts(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return [.. recording.Of<DrawTextCall>().Select(call =>
            (call.Text, call.Position, call.Alignment))];
    }

    private static double AscentOf(string text, Theme theme) => new RecordingRenderContext().ExtentOf(TextLabel.Plain(text), theme.TickFont()).Ascent;

    [Theory]
    [InlineData("v2", 14.0)]
    [InlineData("classic", 11.0)]
    public void UnderTheMatplotlibThemes_TheTopSpokeLabel_IsCentredItsPadAndSevenPointsAboveTheCircle(string themeId, double points)
    {
        var theme = themeId.AsTheme();
        var figure = Polar(theme);
        var plot = figure.FirstPlotArea(new RecordingRenderContext());

        var top = Texts(figure).Single(t => t.Text == "90°");

        Assert.Equal(TextAlignment.Center, top.Alignment);
        Assert.Equal(plot.X + plot.Width / 2, top.At.X, 6);
        // The label's middle is the anchor; its baseline stands a third of its ascent below it (TextBox's centre rule).
        double centre = plot.Y + plot.Height / 2 - RoundFrame.In(plot).Radius - points.PointsInPixels;
        Assert.Equal(centre + 0.35 * AscentOf("90°", theme), top.At.Y, 6);
    }

    [Fact]
    public void UnderTheDefaultMatplotlibTheme_TheTitle_StandsItsPadAboveTheTopSpokeLabel()
    {
        var figure = Polar(Theme.MatplotlibV2);
        var texts = Texts(figure);

        var label = texts.Single(t => t.Text == "90°");
        var title = texts.Single(t => t.Text == Title);

        double labelTop = label.At.Y - AscentOf("90°", Theme.MatplotlibV2);
        Assert.Equal(labelTop - Theme.MatplotlibV2.TitleGapUnder, title.At.Y, 6);   // axes.titlepad 6 pt
        Assert.Equal(6.0.PointsInPixels, Theme.MatplotlibV2.TitleGapUnder, 6);
    }

    [Fact]
    public void UnderTheClassicTheme_TheTitle_StandsItsPadAboveTheBox()
    {
        var figure = Polar(Theme.MatplotlibClassic);
        var plot = figure.FirstPlotArea(new RecordingRenderContext());

        var title = Texts(figure).Single(t => t.Text == Title);

        Assert.Equal(plot.Y - Theme.MatplotlibClassic.TitleGapUnder, title.At.Y, 6);   // axes.titlepad 5 pt
        Assert.Equal(5.0.PointsInPixels, Theme.MatplotlibClassic.TitleGapUnder, 6);
    }

    [Fact]
    public void UnderTheLibrarysThemes_TheTitleAndTheLabels_StandWhereTheyAlwaysStood()
    {
        var figure = Polar(Theme.Default);
        var plot = figure.FirstPlotArea(new RecordingRenderContext());
        var texts = Texts(figure);

        var title = texts.Single(t => t.Text == Title);
        var top = texts.Single(t => t.Text == "90°");

        Assert.Equal(plot.Y - Theme.Default.TitleGapUnder - 0.05 * 2 * RoundFrame.In(plot).Radius, title.At.Y, 6);
        Assert.Equal(8, Math.Abs(top.At.X - (plot.X + plot.Width / 2)), 6);
        Assert.Equal(plot.Y + plot.Height / 2 - RoundFrame.In(plot).Radius - 4, top.At.Y, 6);
        Assert.Equal(TextAlignment.Left, top.Alignment);
    }

    [Fact]
    public void ThePolarLooks_AreValues_ThatSurviveTheThemesJson()
    {
        Assert.Equal(PolarLook.Library, Theme.Default.Looks.Polar);
        Assert.Equal(PolarLook.Matplotlib, Theme.MatplotlibV2.Looks.Polar);
        Assert.Equal(PolarLook.Matplotlib, Theme.MatplotlibClassic.Looks.Polar);
        // Whether the title rises over what is below it (axes.titley None) is the text look's, one fact for every axes: v2 only.
        Assert.True(Theme.MatplotlibV2.Looks.Text.TitleOverWhatIsBelow);
        Assert.False(Theme.MatplotlibClassic.Looks.Text.TitleOverWhatIsBelow);
        Assert.False(Theme.Default.Looks.Text.TitleOverWhatIsBelow);

        foreach (var theme in new[] { Theme.Default, Theme.MatplotlibV2, Theme.MatplotlibClassic })
        {
            Assert.Equal(theme.Looks.Polar, theme.ThroughDto().Looks.Polar);
            Assert.Equal(theme.Looks.Text.TitleOverWhatIsBelow, theme.ThroughDto().Looks.Text.TitleOverWhatIsBelow);
        }
    }
}
