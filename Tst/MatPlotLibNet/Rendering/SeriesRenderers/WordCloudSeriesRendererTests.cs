// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A word cloud is drawn word by word, the heaviest first, each inside the plot and clear of the others, in a colour that
/// can be read against the background. In SVG each word says how wide it is to be set (so a browser's own font cannot make it
/// run into the next) and carries its weight as a title. A word that found no room is announced, never dropped without a word.</summary>
[Collection("ChartDiagnosticsGlobalState")]
public class WordCloudSeriesRendererTests
{
    private static readonly Rect Box = new(0, 0, 800, 500);

    private static readonly string[] Words = ["data", "chart", "plot", "axis", "series", "legend"];
    private static readonly double[] Weights = [100, 40, 4, 40, 18, 9];

    private static Axes CloudAxes(out WordCloudSeries series, string[]? words = null, double[]? weights = null)
    {
        var axes = new Axes();
        series = axes.WordCloud(words ?? Words, weights ?? Weights);
        return axes;
    }

    private static DrawTextCall[] Texts(RecordingRenderContext ctx) => [.. ctx.Of<DrawTextCall>()];

    [Fact]
    public void EveryWord_IsDrawn_TheHeaviestFirst_TiesInOrdinalOrder()
    {
        var texts = Texts(CloudAxes(out _).Drawn(Box));

        Assert.Equal(["data", "axis", "chart", "series", "legend", "plot"], texts.Select(t => t.Text));
    }

    [Fact]
    public void TheSizeOfAWord_IsTheOneItsWeightGives()
    {
        var texts = Texts(CloudAxes(out _).Drawn(Box));

        Assert.Equal(60.0, (texts[0].Font).Size, 1e-9);        // data, the heaviest
        Assert.Equal(12.0, (texts[^1].Font).Size, 1e-9);       // plot, the lightest
        Assert.True((texts[1].Font).Size > (texts[3].Font).Size);
    }

    [Fact]
    public void TheWords_StandInsideThePlot_AndTheHeaviestIsAtTheCentre()
    {
        var ctx = CloudAxes(out _).Drawn(Box);
        var texts = Texts(ctx);

        foreach (var text in texts)
        {
            var at = text.Position;
            Assert.InRange(at.X, Box.X, Box.X + Box.Width);
            Assert.InRange(at.Y, Box.Y, Box.Y + Box.Height);
        }

        // The heaviest word is set at the centre: its box is centred, so its left edge is half its width left of the centre.
        var first = texts[0].Position;
        double width = ctx.MeasureText("data", texts[0].Font).Width;   // the one source the layout measures with: the surface it is drawn on
        Assert.Equal(Box.X + Box.Width / 2 - width / 2, first.X, 3.0);
    }

    [Fact]
    public void EveryWord_CanBeReadAgainstTheBackground()
    {
        // W7: 4.5 to 1 for text under 24 px, 3 to 1 for larger text. The colours of the cycle are not all that dark.
        var texts = Texts(CloudAxes(out _).Drawn(Box));
        var background = Theme.Default.AxesBackground;

        foreach (var text in texts)
        {
            var font = text.Font;
            double needed = font.Size < 24 ? 4.5 : 3.0;
            Assert.True(font.Color!.Value.ContrastRatioWith(background) >= needed - 1e-9,
                $"{text.Text} at {font.Size} px in {font.Color} reads {font.Color.Value.ContrastRatioWith(background):F2}:1 against the background");
        }
    }

    [Fact]
    public void AnExplicitColour_IsKept_WhenItCanBeRead()
    {
        var axes = CloudAxes(out var series);
        series.Colors = [Color.FromHex("#202020")];

        var texts = Texts(axes.Drawn(Box));

        Assert.Equal(Color.FromHex("#202020"), (texts[0].Font).Color);
    }

    // ── SVG ─────────────────────────────────────────────────────────────────────

    private static string Svg(string[] words, double[] weights)
    {
        var figure = new Figure { Width = 800, Height = 500 };
        figure.AddSubPlot(1, 1, 1).WordCloud(words, weights);
        return figure.ToSvg();
    }

    [Fact]
    public void W8_TheTextElements_ComeHeaviestFirst_EachWithItsWordAndWeightAsATitle()
    {
        string svg = Svg(Words, Weights);

        var titles = svg.Elements("title").Select(title => title.Text).Where(title => title.Length > 0).ToArray();
        Assert.Equal(["data: 100", "axis: 40", "chart: 40", "series: 18", "legend: 9", "plot: 4"], titles);
        var order = svg.Elements("text").Select(text => text.Text).Where(text => text.Length > 0 && text.All(char.IsLetterOrDigit)).ToArray();
        Assert.Equal(["data", "axis", "chart", "series", "legend", "plot"], order);
    }

    [Fact]
    public void EveryWord_SaysHowWideItIsToBeSet()
    {
        string svg = Svg(Words, Weights);

        var lengths = svg.AttributeValues("textLength").Select(length => length.Number()).ToArray();

        Assert.Equal(6, lengths.Length);
        Assert.All(lengths, length => Assert.True(length > 0));
        Assert.Equal(6, svg.AttributeValues("lengthAdjust").Count(adjust => adjust == "spacingAndGlyphs"));
    }

    // ── what does not fit ───────────────────────────────────────────────────────
    [Fact]
    public void AWordThatFoundNoRoom_IsAnnouncedByNameAndCount_AndTheOthersAreStillDrawn()
    {
        string[] words = [.. Enumerable.Range(0, 30).Select(i => "longerword" + i)];
        double[] weights = [.. Enumerable.Range(0, 30).Select(i => 1.0 + i)];
        var axes = CloudAxes(out _, words, weights);
        RecordingRenderContext? ctx = null;

        var seen = DiagnosticsCapture.During(() => ctx = axes.Drawn(new Rect(0, 0, 120, 60)));

        var announcement = Assert.Single(seen);
        Assert.Contains("did not fit", announcement.Message, StringComparison.Ordinal);
        Assert.Contains("of 30", announcement.Message, StringComparison.Ordinal);
        Assert.Contains("longerword0", announcement.Message, StringComparison.Ordinal);   // the lightest is the first to have no room
        Assert.True(Texts(ctx!).Length is > 0 and < 30);
    }

    [Fact]
    public void WhenEveryWordFits_NothingIsAnnounced_EvenIfTheyWereShrunk()
    {
        string[] words = [.. Enumerable.Range(0, 14).Select(i => "token" + i)];
        double[] weights = [.. Enumerable.Range(0, 14).Select(i => 1.0 + i)];
        var axes = CloudAxes(out _, words, weights);
        RecordingRenderContext? ctx = null;

        var seen = DiagnosticsCapture.During(() => ctx = axes.Drawn(new Rect(0, 0, 330, 150)));

        Assert.Empty(seen);
        Assert.Equal(14, Texts(ctx!).Length);
    }

    [Fact]
    public void ACloudWithNoWords_DrawsNothing()
    {
        var axes = CloudAxes(out _, [], []);

        Assert.Empty(Texts(axes.Drawn(Box)));
    }
}
