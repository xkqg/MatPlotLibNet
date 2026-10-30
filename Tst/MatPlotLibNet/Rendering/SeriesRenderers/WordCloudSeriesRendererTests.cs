// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
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

    private static RecordingRenderContext Draw(Axes axes, Rect? box = null)
    {
        var ctx = new RecordingRenderContext();
        AxesRenderer.Create(axes, box ?? Box, ctx, Theme.Default).Render();
        return ctx;
    }

    private static dynamic[] Texts(RecordingRenderContext ctx) =>
        [.. ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!)];

    [Fact]
    public void EveryWord_IsDrawn_TheHeaviestFirst_TiesInOrdinalOrder()
    {
        var texts = Texts(Draw(CloudAxes(out _)));

        Assert.Equal(["data", "axis", "chart", "series", "legend", "plot"], texts.Select(t => (string)t.text));
    }

    [Fact]
    public void TheSizeOfAWord_IsTheOneItsWeightGives()
    {
        var texts = Texts(Draw(CloudAxes(out _)));

        Assert.Equal(60.0, ((Font)texts[0].font).Size, 1e-9);        // data, the heaviest
        Assert.Equal(12.0, ((Font)texts[^1].font).Size, 1e-9);       // plot, the lightest
        Assert.True(((Font)texts[1].font).Size > ((Font)texts[3].font).Size);
    }

    [Fact]
    public void TheWords_StandInsideThePlot_AndTheHeaviestIsAtTheCentre()
    {
        var ctx = Draw(CloudAxes(out _));
        var texts = Texts(ctx);

        foreach (var text in texts)
        {
            var at = (Point)text.position;
            Assert.InRange(at.X, Box.X, Box.X + Box.Width);
            Assert.InRange(at.Y, Box.Y, Box.Y + Box.Height);
        }

        // The heaviest word is set at the centre: its box is centred, so its left edge is half its width left of the centre.
        var first = (Point)texts[0].position;
        double width = FontMetricsDefault.Current.Measure("data", (Font)texts[0].font).Width;   // the one source the layout measures with
        Assert.Equal(Box.X + Box.Width / 2 - width / 2, first.X, 3.0);
    }

    [Fact]
    public void EveryWord_CanBeReadAgainstTheBackground()
    {
        // W7: 4.5 to 1 for text under 24 px, 3 to 1 for larger text. The colours of the cycle are not all that dark.
        var texts = Texts(Draw(CloudAxes(out var series)));
        var background = Theme.Default.AxesBackground;

        foreach (var text in texts)
        {
            var font = (Font)text.font;
            double needed = font.Size < 24 ? 4.5 : 3.0;
            Assert.True(font.Color!.Value.ContrastRatioWith(background) >= needed - 1e-9,
                $"{(string)text.text} at {font.Size} px in {font.Color} reads {font.Color.Value.ContrastRatioWith(background):F2}:1 against the background");
        }
    }

    [Fact]
    public void AnExplicitColour_IsKept_WhenItCanBeRead()
    {
        var axes = CloudAxes(out var series);
        series.Colors = [Color.FromHex("#202020")];

        var texts = Texts(Draw(axes));

        Assert.Equal(Color.FromHex("#202020"), ((Font)texts[0].font).Color);
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

        var titles = Regex.Matches(svg, "<title>([^<]*)</title>").Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(["data: 100", "axis: 40", "chart: 40", "series: 18", "legend: 9", "plot: 4"], titles);
        var order = Regex.Matches(svg, @"<text [^>]*>(\w+)</text>").Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(["data", "axis", "chart", "series", "legend", "plot"], order);
    }

    [Fact]
    public void EveryWord_SaysHowWideItIsToBeSet()
    {
        string svg = Svg(Words, Weights);

        var lengths = Regex.Matches(svg, "textLength=\"([0-9.]+)\"").Select(m => double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

        Assert.Equal(6, lengths.Length);
        Assert.All(lengths, length => Assert.True(length > 0));
        Assert.Equal(6, Regex.Matches(svg, "lengthAdjust=\"spacingAndGlyphs\"").Count);
    }

    // ── what does not fit ───────────────────────────────────────────────────────

    private static List<ChartDiagnostic> Announced(Action act)
    {
        var seen = new List<ChartDiagnostic>();
        void Collect(ChartDiagnostic d) => seen.Add(d);
        ChartDiagnostics.Emitted += Collect;
        try
        {
            act();
        }
        finally
        {
            ChartDiagnostics.Emitted -= Collect;
        }

        return seen;
    }

    [Fact]
    public void AWordThatFoundNoRoom_IsAnnouncedByNameAndCount_AndTheOthersAreStillDrawn()
    {
        string[] words = [.. Enumerable.Range(0, 30).Select(i => "longerword" + i)];
        double[] weights = [.. Enumerable.Range(0, 30).Select(i => 1.0 + i)];
        var axes = CloudAxes(out _, words, weights);
        RecordingRenderContext? ctx = null;

        var seen = Announced(() => ctx = Draw(axes, new Rect(0, 0, 120, 60)));

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

        var seen = Announced(() => ctx = Draw(axes, new Rect(0, 0, 330, 150)));

        Assert.Empty(seen);
        Assert.Equal(14, Texts(ctx!).Length);
    }

    [Fact]
    public void ACloudWithNoWords_DrawsNothing()
    {
        var axes = CloudAxes(out _, [], []);

        Assert.Empty(Texts(Draw(axes)));
    }
}
