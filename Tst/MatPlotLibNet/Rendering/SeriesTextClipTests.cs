// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A clip cuts the marks drawn under it, never a text. matplotlib 3.11.2 (printed): <c>ax.text</c> is <c>clip_on False</c>
/// (<c>Axes.text</c> passes it), and the Annotations of <c>ax.bar_label</c> and <c>ax.annotate</c> carry neither a clip box nor a clip path, so
/// a bar label above a set <c>ylim</c> is drawn whole (174 dark pixels of 'HIGHXXXX' above the axes top), while the bar, the line and every
/// other artist an axes adds is <c>clip_on True</c>. So the label of a series is never inside the clip of its marks, under any theme, and the
/// kinds of the library's own are clipped like every other artist with their names and values drawn whole.</summary>
[Collection("ChartServicesGlobalState")] // sets GlyphPathProviderDefault.Current
public class SeriesTextClipTests
{
    // ── the surface: SvgRenderContext ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A text drawn between two marks under one clip stands outside it, and both marks inside it.</summary>
    [Fact]
    public void ATextDrawnUnderAClip_StandsOutsideIt_WhileTheMarksAroundItAreCut()
    {
        using (GlyphPathProviderDefault.Holding(null))
        {
            var ctx = new SvgRenderContext();
            ctx.PushClip(new Rect(0, 0, 100, 100));
            ctx.DrawRectangle(new Rect(10, 10, 20, 20), new ShapeStyle(Colors.Red, null, 0));
            ctx.DrawText("label", new Point(50, 120), new Font(), TextAlignment.Left);
            ctx.DrawRectangle(new Rect(40, 40, 20, 20), new ShapeStyle(Colors.Blue, null, 0));
            ctx.PopClip();

            var doc = Parsed(ctx.GetOutput());
            Assert.All(Elements(doc, "rect").Where(r => r.Parent?.Name.LocalName != "clipPath"), r => Assert.True(Clipped(r)));
            Assert.False(Clipped(Assert.Single(Elements(doc, "text"))));
            Assert.Single(doc.Each("clipPath"));
        }
    }

    /// <summary>A text written as its glyph outline (the Skia package installed) is a text too: outside the clip.</summary>
    [Fact]
    public void ATextWrittenAsItsGlyphOutline_StandsOutsideTheClip()
    {
        using (GlyphPathProviderDefault.Holding(new TestGlyphPaths("M 0 0 L 1 1")))
        {
            var ctx = new SvgRenderContext();
            ctx.PushClip(new Rect(0, 0, 100, 100));
            ctx.DrawCircle(new Point(10, 10), 3, new ShapeStyle(Colors.Red, null, 0));
            ctx.DrawText("label", new Point(50, 120), new Font(), TextAlignment.Left);
            ctx.PopClip();

            var doc = Parsed(ctx.GetOutput());
            Assert.True(Clipped(Assert.Single(Elements(doc, "circle"))));
            var outline = Assert.Single(Elements(doc, "path"), p => p.Parent?.Name.LocalName != "clipPath");
            Assert.False(Clipped(outline));
        }
    }

    /// <summary>A text inside a group opened under the clip (an opacity, a link) is outside the clip too, and the marks in that group
    /// inside it.</summary>
    [Fact]
    public void ATextInAGroupOpenedUnderTheClip_StandsOutsideIt()
    {
        using (GlyphPathProviderDefault.Holding(null))
        {
            var ctx = new SvgRenderContext();
            ctx.PushClip(new Rect(0, 0, 100, 100));
            ctx.SetOpacity(0.5);
            ctx.DrawRectangle(new Rect(10, 10, 20, 20), new ShapeStyle(Colors.Red, null, 0));
            ctx.DrawText("faded", new Point(50, 120), new Font(), TextAlignment.Left);
            ctx.SetOpacity(1.0);
            ctx.BeginHyperlink("https://example.org", "link");
            ctx.DrawText("linked", new Point(50, 130), new Font(), TextAlignment.Left);
            ctx.DrawRectangle(new Rect(40, 40, 20, 20), new ShapeStyle(Colors.Blue, null, 0));
            ctx.EndHyperlink();
            ctx.PopClip();

            var doc = Parsed(ctx.GetOutput());
            Assert.All(Elements(doc, "rect").Where(r => r.Parent?.Name.LocalName != "clipPath"), r => Assert.True(Clipped(r)));
            Assert.All(Elements(doc, "text"), t => Assert.False(Clipped(t)));
            Assert.Equal(2, Elements(doc, "text").Count());
        }
    }

    // ── a chart: every text outside every clip, the marks of every kind clipped under the matplotlib themes ────────────────────

    /// <summary>The labels of the bars (<c>ax.bar_label</c>) stand outside the clip that cuts the bars, also the label of a bar that ends
    /// at the limit, which stands past the plot (a bar that ends past the limit has no label: matplotlib draws no annotation of a point
    /// the axes do not show).</summary>
    [Theory]
    [MemberData(nameof(MatplotlibThemes))]
    public void UnderTheMatplotlibThemes_TheLabelsOfTheBars_StandOutsideTheClipOfTheBars(Theme theme)
    {
        using (GlyphPathProviderDefault.Holding(null))
        {
            var svg = Plt.Create().WithTheme(theme)
                .AddSubPlot(1, 1, 1, ax => ax.Bar(["a", "b"], [1.0, 2.0], s => s.ShowLabels = true).SetYLim(0, 2))
                .Build().ToSvg();

            var doc = Parsed(svg);
            Assert.Contains(Elements(doc, "rect"), Clipped);
            Assert.Contains(Elements(doc, "text"), t => t.Value == "2");
            Assert.All(Elements(doc, "text"), t => Assert.False(Clipped(t), $"'{t.Value}' is inside a clip"));
        }
    }

    /// <summary>The kinds of the library's own (a Sankey diagram, an arc diagram, a relative rotation graph) are clipped like every
    /// artist an axes adds under the matplotlib themes, and their values and names stand outside the clip, drawn whole.</summary>
    [Theory]
    [MemberData(nameof(MatplotlibThemes))]
    public void UnderTheMatplotlibThemes_AKindOfTheLibrarysOwn_IsClipped_AndItsTextIsWhole(Theme theme)
    {
        using (GlyphPathProviderDefault.Holding(null))
        {
            foreach (var build in OwnKinds)
            {
                var svg = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, build).Build().ToSvg();

                var doc = Parsed(svg);
                Assert.Contains(Elements(doc, "path").Concat(Elements(doc, "polyline")).Concat(Elements(doc, "circle")), Clipped);
                Assert.Contains(Elements(doc, "text"), t => t.Value is "Costs" or "Ada" or "Asset");
                Assert.NotEmpty(Elements(doc, "text"));
                Assert.All(Elements(doc, "text"), t => Assert.False(Clipped(t), $"'{t.Value}' is inside a clip"));
            }
        }
    }

    /// <summary>A Sankey diagram, an arc diagram and a relative rotation graph, each with its names.</summary>
    private static readonly Action<AxesBuilder>[] OwnKinds =
    [
        ax => ax.Sankey([new SankeyNode("Revenue"), new SankeyNode("Costs"), new SankeyNode("Profit")],
            [new SankeyLink(0, 1, 6), new SankeyLink(0, 2, 4)]),
        ax => ax.NetworkGraph([new GraphNode("Ada"), new GraphNode("Ben"), new GraphNode("Cleo")],
            [new GraphEdge("Ada", "Cleo"), new GraphEdge("Ben", "Cleo")], s => s.Layout = GraphLayout.Arc),
        ax => ax.RelativeRotation([Enumerable.Range(0, 80).Select(i => 100 + i * 0.7 + (i % 7) * 1.5).ToArray()],
            Enumerable.Range(0, 80).Select(i => 100 + i * 0.4 + (i % 5) * 0.8).ToArray(), ["Asset"]),
    ];

    public static TheoryData<Theme> MatplotlibThemes => new() { Theme.MatplotlibV2, Theme.MatplotlibClassic };

    // ── a text matplotlib clips: the labels of a contour (clabel's Text has the axes' box as its clip box) ─────────────────────

    /// <summary>A text drawn while the surface cuts its texts (<c>CuttingTexts</c>) stands inside the clip, as a mark does, written as a
    /// text element or as its glyph outline; the text drawn after the scope ends stands outside it again.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ATextDrawnWhileTheTextsAreCut_StandsInsideTheClip_AndTheNextTextOutsideIt(bool outlined)
    {
        using (GlyphPathProviderDefault.Holding(outlined ? new TestGlyphPaths("M 0 0 L 1 1") : null))
        {
            var ctx = new SvgRenderContext();
            ctx.PushClip(new Rect(0, 0, 100, 100));
            ctx.DrawRectangle(new Rect(10, 10, 20, 20), new ShapeStyle(Colors.Red, null, 0));
            using (ctx.CuttingTexts())
            {
                ctx.DrawText("cut", new Point(50, 120), new Font(), TextAlignment.Left);
            }
            ctx.DrawText("whole", new Point(50, 130), new Font(), TextAlignment.Left);
            ctx.PopClip();

            var doc = Parsed(ctx.GetOutput());
            var texts = (outlined ? Elements(doc, "path") : Elements(doc, "text")).ToList();
            Assert.Equal(2, texts.Count);
            Assert.True(Clipped(texts[0]), "the text drawn while the texts are cut stands outside the clip");
            Assert.False(Clipped(texts[1]), "the text drawn after the scope is inside the clip");
        }
    }

    public static TheoryData<Theme> EveryStyle => new() { Theme.Default, Theme.MatplotlibV2, Theme.MatplotlibClassic };

    /// <summary>matplotlib cuts the labels of a contour at the axes as it cuts its lines (<c>clabel</c> gives each label the axes' box as
    /// its clip box, under every style): the label of a line past the axes is cut off with it, and the labels inside stand in the same
    /// clip. Every other text of the chart (the ticks, a title) stands outside every clip.</summary>
    [Theory]
    [MemberData(nameof(EveryStyle))]
    public void TheLabelsOfAContour_AreCutAtTheAxes_AsItsLinesAre(Theme theme)
    {
        using (GlyphPathProviderDefault.Holding(null))
        {
            var doc = Parsed(theme.LabelledContourPastTheAxes().ToSvg());

            var labels = Elements(doc, "text").Where(t => t.Value is "2.5" or "5.5" or "8.5").ToList();
            Assert.Equal(3, labels.Count);
            Assert.All(labels, t => Assert.True(Clipped(t), $"the contour label '{t.Value}' stands outside the clip"));
            Assert.All(Elements(doc, "text").Except(labels), t => Assert.False(Clipped(t), $"'{t.Value}' is inside a clip"));
        }
    }

    // ── reading the SVG ──────────────────────────────────────────────────────────────────────────────────────────────────

    private static XDocument Parsed(string svg) =>
        (svg.TrimStart().StartsWith("<svg", StringComparison.Ordinal) ? svg : $"<svg>{svg}</svg>").Xml();

    private static IEnumerable<XElement> Elements(XDocument doc, string name) =>
        doc.Each(name).Where(e => !e.Ancestors().Any(a => a.Name.LocalName is "clipPath" or "defs"));

    /// <summary>Whether an element is drawn inside a clip: an ancestor carries <c>clip-path</c>.</summary>
    private static bool Clipped(XElement element) => element.Ancestors().Any(a => a.Attribute("clip-path") is not null);
}
