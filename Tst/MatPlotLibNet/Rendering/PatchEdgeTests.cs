// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The edge of a bar-like patch under the matplotlib themes is matplotlib's: <c>Patch._set_edgecolor</c> takes the colour
/// given, else <c>patch.edgecolor</c> when <c>patch.force_edgecolor</c> is on, else none, and the width given, else
/// <c>patch.linewidth</c>. matplotlib 3.11.2, printed with python: under the classic style <c>force_edgecolor True</c>, edgecolor
/// <c>'k'</c>, linewidth 1.0 pt, so <c>ax.bar([1, 2], [3, 4])[0]</c> has edgecolor (0, 0, 0, 1) and linewidth 1.0, as have a
/// <c>barh</c>, a <c>hist</c> bin, a <c>broken_barh</c> and their legend handles; under the default style <c>force_edgecolor
/// False</c>, so a bar has edgecolor (0, 0, 0, 0) (none) even at <c>linewidth=3</c>, and <c>edgecolor='r'</c> draws red at 1.0 pt.
/// The library's own themes keep each series' own rule.</summary>
public class PatchEdgeTests
{
    private static readonly Color Ink = Color.FromHex("#B0A001");
    private static readonly double OnePoint = 1.0.PointsInPixels;

    private static RecordingRenderContext Render(Theme theme, Func<AxesBuilder, AxesBuilder> build)
    {
        var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => build(ax)).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return ctx;
    }

    /// <summary>Every rectangle filled in <paramref name="fill"/>: its edge colour and width.</summary>
    private static List<(Color? Stroke, double Width)> Edges(RecordingRenderContext ctx, Color fill) =>
        [.. ctx.Of<DrawRectangleCall>().Where(c => Equals((object?)c.Fill, fill))
            .Select(c => (c.Stroke, c.StrokeThickness))];

    private static void AllEdged(List<(Color? Stroke, double Width)> edges, Color? stroke, double width)
    {
        Assert.NotEmpty(edges);
        Assert.All(edges, e =>
        {
            Assert.Equal(stroke, e.Stroke);
            Assert.Equal(width, e.Width, 9);
        });
    }

    // ---- the rule ------------------------------------------------------------------------------------------------------------

    [Fact]
    public void UnderClassic_APatchIsEdgedInBlackAtOnePoint_AndAGivenColourOrWidthWins()
    {
        var classic = Theme.MatplotlibClassic;

        Assert.Equal(new PatchEdge(Colors.Black, OnePoint), classic.Looks.Patch.EdgeOver(new(null, null)));
        Assert.Equal(new PatchEdge(Colors.Red, OnePoint), classic.Looks.Patch.EdgeOver(new(Colors.Red, null)));
        Assert.Equal(new PatchEdge(Colors.Black, 3), classic.Looks.Patch.EdgeOver(new(null, 3)));
        Assert.Equal(PatchEdge.None, classic.Looks.Patch.EdgeOver(new(null, 0)));
    }

    [Fact]
    public void UnderV2_APatchHasNoEdgeUnlessAColourIsGiven_WhichIsDrawnAtOnePoint()
    {
        var v2 = Theme.MatplotlibV2;

        Assert.Equal(PatchEdge.None, v2.Looks.Patch.EdgeOver(new(null, null)));
        Assert.Equal(PatchEdge.None, v2.Looks.Patch.EdgeOver(new(null, 3)));
        Assert.Equal(new PatchEdge(Colors.Red, OnePoint), v2.Looks.Patch.EdgeOver(new(Colors.Red, null)));
        Assert.Equal(new PatchEdge(Colors.Red, 2), v2.Looks.Patch.EdgeOver(new(Colors.Red, 2)));
    }

    [Theory]
    [InlineData("default")]
    [InlineData("dark")]
    public void UnderALibraryTheme_TheThemeDoesNotDecide(string name)
    {
        var theme = name.AsTheme();

        Assert.Null(theme.Looks.Patch.EdgeOver(new(null, null)));
        Assert.Null(theme.Looks.Patch.EdgeOver(new(Colors.Red, 2)));
    }

    // ---- the series under classic: the repro ----------------------------------------------------------------------------------

    [Fact]
    public void UnderClassic_ABarIsEdgedInBlackAtOnePoint()
    {
        var ctx = Render(Theme.MatplotlibClassic, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => b.Color = Ink));

        AllEdged(Edges(ctx, Ink), Colors.Black, OnePoint);
    }

    [Fact]
    public void UnderClassic_ACountedBarIsEdgedInBlackAtOnePoint()
    {
        var ctx = Render(Theme.MatplotlibClassic, ax => ax.Countplot(["a", "b", "a"], c => c.Color = Ink));

        AllEdged(Edges(ctx, Ink), Colors.Black, OnePoint);
    }

    [Fact]
    public void UnderClassic_AWaterfallStepIsEdgedInBlackAtOnePoint()
    {
        var ctx = Render(Theme.MatplotlibClassic, ax => ax.Waterfall(["a", "b"], [3.0, -1.0], w =>
        {
            w.IncreaseColor = Ink;
            w.DecreaseColor = Ink;
        }));

        AllEdged(Edges(ctx, Ink), Colors.Black, OnePoint);
    }

    [Fact]
    public void UnderClassic_ABrokenBarIsEdgedInBlackAtOnePoint()
    {
        var ctx = Render(Theme.MatplotlibClassic, ax => ax.BrokenBarH([[new BarRange(1, 2), new BarRange(4, 1)]], b => b.Color = Ink));

        AllEdged(Edges(ctx, Ink), Colors.Black, OnePoint);
    }

    [Fact]
    public void UnderClassic_AHistogramBinIsEdgedInBlackAtOnePoint()
    {
        var ctx = Render(Theme.MatplotlibClassic, ax => ax.Hist([1.0, 2, 2, 3, 3, 3], 3, h => h.Color = Ink));

        AllEdged(Edges(ctx, Ink), Colors.Black, OnePoint);
    }

    [Fact]
    public void UnderClassic_ABarGivenAnEdgeColourAndWidth_IsEdgedAsGiven_AndAWidthOf0DrawsNone()
    {
        var given = Render(Theme.MatplotlibClassic, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.EdgeColor = Colors.Red; b.LineWidth = 3; }));
        var none = Render(Theme.MatplotlibClassic, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.LineWidth = 0; }));

        AllEdged(Edges(given, Ink), Colors.Red, 3);
        AllEdged(Edges(none, Ink), null, 0);
    }

    // ---- v2 and the library's own themes ------------------------------------------------------------------------------------------

    [Fact]
    public void UnderV2_ABarHasNoEdge_AndOneGivenAColourIsEdgedAtOnePoint()
    {
        var plain = Render(Theme.MatplotlibV2, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.LineWidth = 3; }));
        var red = Render(Theme.MatplotlibV2, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.EdgeColor = Colors.Red; }));

        AllEdged(Edges(plain, Ink), null, 0);
        AllEdged(Edges(red, Ink), Colors.Red, OnePoint);
    }

    [Fact]
    public void UnderTheLibraryTheme_TheBarsAndTheHistogramKeepTheirOwnEdges()
    {
        var plain = Render(Theme.Default, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => b.Color = Ink));
        var red = Render(Theme.Default, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.EdgeColor = Colors.Red; }));
        var wide = Render(Theme.Default, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.LineWidth = 2; }));
        var hist = Render(Theme.Default, ax => ax.Hist([1.0, 2, 2, 3], 3, h => { h.Color = Ink; h.EdgeColor = Colors.Red; }));

        AllEdged(Edges(plain, Ink), null, 0);
        AllEdged(Edges(red, Ink), Colors.Red, 1);
        AllEdged(Edges(wide, Ink), Ink, 2);
        AllEdged(Edges(hist, Ink), Colors.Red, 0.5);
    }

    // ---- the legend handles ----------------------------------------------------------------------------------------------------

    /// <summary>A legend handle copies the edge of the patch it stands for (matplotlib's <c>HandlerPatch</c> updates the handle from
    /// it): black at 1 pt under classic, none under v2 and under the library's own themes.</summary>
    [Theory]
    [InlineData("classic", true)]
    [InlineData("v2", false)]
    [InlineData("default", false)]
    public void TheLegendHandleOfABar_HasTheEdgeOfTheBar(string name, bool edged)
    {
        var theme = name.AsTheme();
        var ctx = Render(theme, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.Label = "bars"; }).WithLegend());

        var edges = Edges(ctx, Ink);
        Assert.Equal(3, edges.Count);
        Assert.Equal(edged ? Colors.Black : null, edges[^1].Stroke);
        Assert.Equal(edged ? OnePoint : 0, edges[^1].Width, 9);
    }

    /// <summary>The edge a bar was given reaches its handle under the matplotlib themes, colour and width, and never under the
    /// library's own (whose handle is a plain rectangle).</summary>
    [Theory]
    [InlineData("classic", true)]
    [InlineData("v2", true)]
    [InlineData("default", false)]
    public void TheLegendHandleOfABarGivenAnEdge_HasTheGivenEdge(string name, bool edged)
    {
        var theme = name.AsTheme();
        var ctx = Render(theme, ax => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.EdgeColor = Colors.Red; b.LineWidth = 3; b.Label = "bars"; })
            .WithLegend());

        var handle = Edges(ctx, Ink)[^1];
        Assert.Equal(edged ? Colors.Red : null, handle.Stroke);
        Assert.Equal(edged ? 3 : 0, handle.Width, 9);
    }

    [Theory]
    [InlineData("count")]
    [InlineData("waterfall")]
    [InlineData("brokenbar")]
    [InlineData("hist")]
    public void UnderClassic_TheLegendHandleOfEveryBarLikeSeries_IsEdgedInBlack(string kind)
    {
        var ctx = Render(Theme.MatplotlibClassic, ax => (kind switch
        {
            "count" => ax.Countplot(["a", "b"], s => { s.Color = Ink; s.Label = "s"; }),
            "waterfall" => ax.Waterfall(["a"], [3.0], s => { s.IncreaseColor = Ink; s.Label = "s"; }),
            "brokenbar" => ax.BrokenBarH([[new BarRange(1, 2)]], s => { s.Color = Ink; s.Label = "s"; }),
            _ => ax.Hist([1.0, 2], 2, s => { s.Color = Ink; s.Label = "s"; }),
        }).WithLegend());

        var handle = ctx.Of<DrawRectangleCall>().Last();
        Assert.Equal(Colors.Black, handle.Stroke);
        Assert.Equal(OnePoint, handle.StrokeThickness, 9);
    }

    // ---- the other patches of the same rule: a pie's wedge, a donut's, a polar bar, a Gantt bar ---------------------------------

    /// <summary>Every shape of <paramref name="kind"/> filled in <paramref name="fill"/>: its edge colour and width.</summary>
    private static List<(Color? Stroke, double Width)> EdgesOf(RecordingRenderContext ctx, Type kind, Color fill) =>
        [.. ctx.Calls.OfType<ShapeCall>().Where(c => c.GetType() == kind && c.Fill == fill)
            .Select(c => (c.Stroke, c.StrokeThickness))];

    private static RecordingRenderContext Other(Theme theme, string kind) => Render(theme, ax => kind switch
    {
        "pie" => ax.Pie([1.0, 2.0], configure: p => p.Colors = [Ink, Ink]),
        "donut" => ax.Donut([1.0, 2.0], configure: d => d.Colors = [Ink, Ink]),
        "polarbar" => ax.PolarBar([1.0, 2.0], [0.0, 1.0], b => { b.Color = Ink; b.Alpha = 1; }),
        _ => ax.Gantt(["a", "b"], [0.0, 1.0], [2.0, 3.0], g => g.Color = Ink),
    });

    private static Type KindOf(string kind) => kind switch
    {
        "pie" or "donut" => typeof(DrawPathCall),
        "polarbar" => typeof(DrawPolygonCall),
        _ => typeof(DrawRectangleCall),
    };

    /// <summary>matplotlib 3.11.2, printed under the classic style: a <c>pie</c> wedge (a donut is a pie of wedges given a width), a
    /// polar <c>bar</c> and a <c>barh</c> (a Gantt chart's bars) are edged (0, 0, 0, 1) at 1.0 pt; under the default style none is.</summary>
    [Theory]
    [InlineData("pie")]
    [InlineData("donut")]
    [InlineData("polarbar")]
    [InlineData("gantt")]
    public void UnderTheMatplotlibThemes_APieWedgeADonutWedgeAPolarBarAndAGanttBar_AreEdgedByThePatchRule(string kind)
    {
        AllEdged(EdgesOf(Other(Theme.MatplotlibClassic, kind), KindOf(kind), Ink), Colors.Black, OnePoint);
        AllEdged(EdgesOf(Other(Theme.MatplotlibV2, kind), KindOf(kind), Ink), null, 0);
    }

    /// <summary>The library's own themes keep each one's own edge: a white pixel round a pie wedge, half a pixel in the bar's colour round a
    /// polar bar, none round a donut wedge or a Gantt bar.</summary>
    [Theory]
    [InlineData("pie", "#FFFFFF", 1)]
    [InlineData("donut", null, 0)]
    [InlineData("polarbar", "#B0A001", 0.5)]
    [InlineData("gantt", null, 0)]
    public void UnderTheLibraryTheme_APieWedgeADonutWedgeAPolarBarAndAGanttBar_KeepTheirOwnEdges(string kind, string? stroke, double width) =>
        AllEdged(EdgesOf(Other(Theme.Default, kind), KindOf(kind), Ink), stroke is null ? null : Color.FromHex(stroke), width);

    /// <summary>A polar bar given no alpha is opaque under the matplotlib themes (printed: a polar <c>bar</c>'s alpha None, face alpha 1.0
    /// under both styles) and 0.8 under the library's own; an alpha given is drawn under every theme, 0.8 included, and only a given one
    /// is written to JSON.</summary>
    [Fact]
    public void APolarBarGivenNoAlpha_IsOpaqueUnderTheMatplotlibThemes_And08UnderTheLibraryTheme()
    {
        byte AlphaOf(Theme theme, double? given)
        {
            var ctx = Render(theme, ax => ax.PolarBar([1.0, 2.0], [0.0, 1.0], b => { b.Color = Ink; if (given is { } a) b.Alpha = a; }));
            return (ctx.Of<DrawPolygonCall>().First().Fill)!.Value.A;
        }

        Assert.Equal(255, AlphaOf(Theme.MatplotlibV2, null));
        Assert.Equal(255, AlphaOf(Theme.MatplotlibClassic, null));
        Assert.InRange(AlphaOf(Theme.Default, null), 203, 204);
        Assert.InRange(AlphaOf(Theme.MatplotlibV2, 0.8), 203, 204);
        Assert.Null(new PolarBarSeries([1.0], [0.0]).ToSeriesDto().Alpha);
        Assert.Equal(0.8, new PolarBarSeries([1.0], [0.0]) { Alpha = 0.8 }.ToSeriesDto().Alpha);
    }

    /// <summary>A violin's body is a collection: matplotlib 3.11.2 prints its edge (0, 0, 0, 0.3) at 1.0 pt under the classic style (the
    /// forced black at the body's alpha) and none under the default one. The library's own themes keep the outline in the body's own
    /// colour, 1 pixel wide.</summary>
    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [InlineData("default")]
    public void AViolinsBody_IsEdgedByThePatchRuleAtItsAlpha_UnderTheMatplotlibThemes(string name)
    {
        var theme = name.AsTheme();
        var ctx = Render(theme, ax => ax.Violin([[1.0, 2, 3, 4], [2.0, 3, 4, 5]], v => v.Color = Ink));

        var bodies = ctx.Of<DrawPolygonCall>().Select(c => (c.Stroke, c.StrokeThickness)).ToList();
        Assert.NotEmpty(bodies);
        Assert.All(bodies, b =>
        {
            switch (name)
            {
                case "classic":
                    Assert.Equal(Colors.Black.WithAlpha((byte)Math.Round(0.3 * 255)), b.Item1);
                    Assert.Equal(OnePoint, b.Item2, 9);
                    break;
                case "v2":
                    Assert.Null(b.Item1);
                    break;
                default:
                    Assert.Equal(Ink, b.Item1);
                    Assert.Equal(1, b.Item2);
                    break;
            }
        });
    }

    /// <summary>A violin's extrema and centre bars are lines: matplotlib 3.11.2 prints their width as <c>lines.linewidth</c>, 1.5 pt under
    /// the default style and 1.0 pt under the classic one; the library's own themes keep 1.5 pixels.</summary>
    [Theory]
    [InlineData("classic", 1.0)]
    [InlineData("v2", 1.5)]
    [InlineData("default", 0.0)]
    public void AViolinsStatLines_AreAsWideAsTheThemesLines(string name, double points)
    {
        var theme = name.AsTheme();
        var ctx = Render(theme, ax => ax.Violin([[1.0, 2, 3, 4], [2.0, 3, 4, 5]], v => v.Color = Ink));

        double expected = points > 0 ? points.PointsInPixels : 1.5;
        var stats = Ink;   // the colour the series names wears its lines too
        var bars = ctx.Of<DrawLineCall>().Where(c => Equals((object?)c.Color, stats))
            .Select(c => c.Thickness).ToList();
        Assert.Equal(6, bars.Count);   // per violin: the bar, the cap at the minimum, the cap at the maximum
        Assert.All(bars, width => Assert.Equal(expected, width, 9));
    }

    /// <summary>Their legend handles carry the same edge under classic.</summary>
    [Theory]
    [InlineData("pie")]
    [InlineData("donut")]
    [InlineData("polarbar")]
    [InlineData("gantt")]
    public void UnderClassic_TheLegendHandleOfAPieADonutAPolarBarAndAGanttChart_IsEdgedInBlack(string kind)
    {
        var ctx = Render(Theme.MatplotlibClassic, ax => (kind switch
        {
            "pie" => ax.Pie([1.0, 2.0], ["a", "b"]),
            "donut" => ax.Donut([1.0, 2.0], ["a", "b"]),
            "polarbar" => ax.PolarBar([1.0, 2.0], [0.0, 1.0], b => b.Label = "s"),
            _ => ax.Gantt(["a", "b"], [0.0, 1.0], [2.0, 3.0], g => g.Label = "s"),
        }).WithLegend());

        var handle = ctx.Of<DrawRectangleCall>().Last();
        Assert.Equal(Colors.Black, handle.Stroke);
        Assert.Equal(OnePoint, handle.StrokeThickness, 9);
    }

    // ---- the 3-D boxes and bars -------------------------------------------------------------------------------------------------

    /// <summary>Every face a 3-D series filled in a shade of red: its edge colour and width (the panes of the cube are grey).</summary>
    private static List<(Color? Stroke, double Width)> RedFaceEdges(RecordingRenderContext ctx) =>
        [.. ctx.Of<DrawPolygonCall>().Where(c => c.Fill is Color { } f && f.R > 2 * f.G + 20)
            .Select(c => (c.Stroke, c.StrokeThickness))];

    private static RecordingRenderContext RenderThreeD(Theme theme, string kind) => Render(theme, ax => kind switch
    {
        "bar3d" => ax.Bar3D([0.0], [0.0], [2.0], s => s.Color = Colors.Red),
        "voxels" => ax.Voxels(new bool[1, 1, 1] { { { true } } }, s => s.Color = Colors.Red),
        "planarbar3d" => ax.PlanarBar3D([0.0, 1.0], [0.0, 0.0], [2.0, 3.0], s => s.Color = Colors.Red),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    });

    /// <summary>matplotlib 3.11.2, printed with python: <c>ax.bar3d(...)</c> and <c>ax.voxels(...)</c> (a <c>Poly3DCollection</c>) and
    /// <c>ax.bar(..., zs=0, zdir='y')</c> on a 3-D axes (a <c>Patch</c>) have <c>get_edgecolor()</c> black (0, 0, 0, 1) at
    /// <c>get_linewidth()</c> 1.0 under the classic style (<c>patch.force_edgecolor</c>) and none under the default style
    /// (<c>[]</c> for the collections, (0, 0, 0, 0) for the bar); a planar bar drawn at the library's opacity 0.8 (<c>alpha=0.8</c>) has
    /// its edge at that opacity, (0, 0, 0, 0.8). So a 3-D box or bar is edged as any patch is under the matplotlib themes;
    /// the library's own themes keep its own hairline: black for a bar, black at 80 of 255 for a voxel.</summary>
    [Theory]
    [InlineData("bar3d", "classic")]
    [InlineData("bar3d", "v2")]
    [InlineData("bar3d", "library")]
    [InlineData("voxels", "classic")]
    [InlineData("voxels", "v2")]
    [InlineData("voxels", "library")]
    [InlineData("planarbar3d", "classic")]
    [InlineData("planarbar3d", "v2")]
    [InlineData("planarbar3d", "library")]
    public void AThreeDBoxOrBar_IsEdgedAsAPatchUnderTheMatplotlibThemes_AndByItsOwnHairlineElse(string kind, string themeId)
    {
        var theme = themeId.AsTheme();

        var edges = RedFaceEdges(RenderThreeD(theme, kind));

        if (themeId == "v2")
        {
            Assert.NotEmpty(edges);
            Assert.All(edges, e => Assert.Null(e.Stroke));
        }
        else if (themeId == "classic") AllEdged(edges, kind == "planarbar3d" ? Colors.Black.WithAlpha(204) : Colors.Black, OnePoint);
        else AllEdged(edges, kind == "voxels" ? Colors.Black.WithAlpha(80) : Colors.Black, 0.5);
    }

    /// <summary>An edge colour given to a planar 3-D bar wins under every theme: at matplotlib's patch width and the bar's opacity under
    /// the matplotlib themes (<c>ax.bar(..., alpha=0.8, edgecolor='r')</c> has <c>get_edgecolor()</c> (1, 0, 0, 0.8) at 1.0), as the
    /// library's own hairline else.</summary>
    [Fact]
    public void AGivenEdgeOfAPlanarBar_WinsUnderEveryTheme()
    {
        RecordingRenderContext Planar(Theme theme) => Render(theme, ax => ax.PlanarBar3D([0.0], [0.0], [2.0], s =>
        {
            s.Color = Colors.Red;
            s.EdgeColor = Colors.Blue;
        }));

        AllEdged(RedFaceEdges(Planar(Theme.MatplotlibV2)), Colors.Blue.WithAlpha(204), OnePoint);
        AllEdged(RedFaceEdges(Planar(Theme.Default)), Colors.Blue, 0.5);
    }

    // ---- the JSON ----------------------------------------------------------------------------------------------------------------

    /// <summary>A bar's width is written only when it was given, 0 included, and reads back as given.</summary>
    [Fact]
    public void ABarsGivenWidth_IsWrittenAndReadBackAsGiven_AndAnUnsetOneIsNotWritten()
    {
        var unset = new BarSeries(["a"], [1.0]);
        var zero = new BarSeries(["a"], [1.0]) { LineWidth = 0 };

        Assert.Null(unset.ToSeriesDto().LineWidth);
        Assert.Null(unset.GivenLineWidth);
        Assert.Equal(0, zero.ToSeriesDto().LineWidth);

        var figure = Plt.Create().WithTheme(Theme.MatplotlibClassic).AddSubPlot(1, 1, 1, ax => ax.Bar(["a"], [1.0], b => b.LineWidth = 0)).Build();
        var back = (BarSeries)figure.RoundTripped().SubPlots[0].Series[0];
        Assert.Equal(0, back.GivenLineWidth);
        Assert.Equal(figure.ToSvg(), figure.RoundTripped().ToSvg());

        var plain = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Bar(["a"], [1.0])).Build();
        Assert.Null(((BarSeries)plain.RoundTripped().SubPlots[0].Series[0]).GivenLineWidth);
    }
}
