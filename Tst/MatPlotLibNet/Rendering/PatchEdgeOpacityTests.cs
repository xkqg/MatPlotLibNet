// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The opacity given to a patch series reaches the edge the matplotlib themes give it, as matplotlib's <c>alpha</c> reaches
/// every colour of the artist (<c>Patch._set_edgecolor</c>: <c>to_rgba(color, self._alpha)</c>). matplotlib 3.11.2 prints (python):
/// <list type="bullet">
/// <item>default style: <c>ax.bar([1], [1], alpha=.5, edgecolor='r').patches[0].get_edgecolor()</c> (1, 0, 0, 0.5);
/// <c>ax.hist([1, 2, 2, 3], alpha=.5, edgecolor='r')[2][0].get_edgecolor()</c> (1, 0, 0, 0.5);
/// <c>ax.fill_between([0, 1, 2], [0, 1, 0], alpha=.5, edgecolor='r').get_edgecolor()</c> [[1, 0, 0, 0.5]];
/// <c>ax.bar([1], [1], alpha=.5)</c> no edge (0, 0, 0, 0).</item>
/// <item>classic style: <c>ax.bar([1], [1], alpha=.5)</c> edge (0, 0, 0, 0.5); <c>ax.stackplot([0, 1], [1, 2], alpha=.5)[0]</c>
/// [[0, 0, 0, 0.5]]; a polar <c>bar([0.0], [1.0], alpha=.5)</c> (0, 0, 0, 0.5).</item>
/// </list>
/// The library's own themes draw the edge each kind has always drawn, its opacity its own.</summary>
public class PatchEdgeOpacityTests
{
    private static readonly Color Ink = Color.FromHex("#B0A001");

    private static string Hex(Color c) => string.Create(CultureInfo.InvariantCulture, $"{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}");

    /// <summary>The distinct strokes of every shape of <paramref name="kind"/> filled in <see cref="Ink"/>, or of every one of them when
    /// <paramref name="anyFill"/>; <c>none</c> for no stroke.</summary>
    private static string Strokes(Theme theme, Func<AxesBuilder, AxesBuilder> build, Type kind, bool anyFill = false)
    {
        var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => build(ax)).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        var rows = ctx.Calls.OfType<ShapeCall>().Where(c => c.GetType() == kind)
            .Where(c => anyFill || (c.Fill is Color f && f.R == Ink.R && f.G == Ink.G && f.B == Ink.B))
            .Select(c => c.Stroke is { } s ? Hex(s) : "none")
            .Distinct().ToList();
        return rows.Count == 0 ? "no shape" : string.Join(" ", rows);
    }

    private static AxesBuilder RedBar(AxesBuilder ax) => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.EdgeColor = Colors.Red; b.Alpha = 0.5; });
    private static AxesBuilder PlainBar(AxesBuilder ax) => ax.Bar(["a", "b"], [3.0, 4.0], b => { b.Color = Ink; b.Alpha = 0.5; });
    private static AxesBuilder RedHist(AxesBuilder ax) => ax.Hist([1.0, 2, 2, 3, 3, 3], 3, h => { h.Color = Ink; h.EdgeColor = Colors.Red; h.Alpha = 0.5; });
    private static AxesBuilder RedArea(AxesBuilder ax) => ax.FillBetween([1.0, 2, 3], [1.0, 3, 2], null, a => { a.Color = Ink; a.EdgeColor = Colors.Red; a.Alpha = 0.5; });
    private static AxesBuilder RedAreaNoAlpha(AxesBuilder ax) => ax.FillBetween([1.0, 2, 3], [1.0, 3, 2], null, a => { a.Color = Ink; a.EdgeColor = Colors.Red; });
    private static AxesBuilder Stack(AxesBuilder ax) => ax.StackPlot([1.0, 2, 3], [[1.0, 2, 3], [2.0, 1, 2]], s => s.Alpha = 0.5);
    private static AxesBuilder PolarBar(AxesBuilder ax) => ax.PolarBar([1.0, 2.0], [0.0, 1.0], b => { b.Color = Ink; b.Alpha = 0.5; });

    [Fact]
    public void UnderTheDefaultStyle_ANamedEdge_TakesTheOpacityOfABarAHistogramAndAFill()
    {
        Assert.Equal("FF000080", Strokes(Theme.MatplotlibV2, RedBar, typeof(DrawRectangleCall)));
        Assert.Equal("FF000080", Strokes(Theme.MatplotlibV2, RedHist, typeof(DrawRectangleCall)));
        Assert.Equal("FF000080", Strokes(Theme.MatplotlibV2, RedArea, typeof(DrawPolygonCall)));
        Assert.Equal("none", Strokes(Theme.MatplotlibV2, PlainBar, typeof(DrawRectangleCall)));
    }

    [Fact]
    public void UnderTheClassicStyle_TheForcedEdge_TakesTheOpacityOfEveryKindGivenOne()
    {
        Assert.Equal("00000080", Strokes(Theme.MatplotlibClassic, PlainBar, typeof(DrawRectangleCall)));
        Assert.Equal("FF000080", Strokes(Theme.MatplotlibClassic, RedBar, typeof(DrawRectangleCall)));
        Assert.Equal("FF000080", Strokes(Theme.MatplotlibClassic, RedHist, typeof(DrawRectangleCall)));
        Assert.Equal("FF000080", Strokes(Theme.MatplotlibClassic, RedArea, typeof(DrawPolygonCall)));
        Assert.Equal("00000080", Strokes(Theme.MatplotlibClassic, Stack, typeof(DrawPolygonCall), anyFill: true));
        Assert.Equal("00000080", Strokes(Theme.MatplotlibClassic, PolarBar, typeof(DrawPolygonCall)));
    }

    /// <summary>A fill given no opacity keeps the opacity of its edge colour, as matplotlib's does with <c>alpha=None</c>.</summary>
    [Fact]
    public void AFillGivenNoOpacity_LeavesItsEdgeAsNamed()
    {
        Assert.Equal("FF0000FF", Strokes(Theme.MatplotlibV2, RedAreaNoAlpha, typeof(DrawPolygonCall)));
        Assert.Equal("FF0000FF", Strokes(Theme.MatplotlibClassic, RedAreaNoAlpha, typeof(DrawPolygonCall)));
    }

    /// <summary>The hatch is a colour of the patch too: matplotlib 3.11.2 prints, for <c>ax.bar([1], [1], alpha=.5, hatch='/', edgecolor='r')</c>,
    /// the hatch colour (0, 0, 0, 0.5) under the classic style (<c>hatch.color</c> 'k' at the bar's alpha) and 'edge' under the default
    /// style, the edge (1, 0, 0, 0.5); <c>Rectangle(.., hatchcolor='r', alpha=0.5).get_hatchcolor()</c> is (1, 0, 0, 0.5). Under the
    /// library's own themes follow the same rule where the author gave an edge or a hatch colour: the hatch is in it, at the opacity given.</summary>
    [Fact]
    public void TheHatchOfABarAHistogramAndAFill_TakesTheOpacityGiven_UnderTheMatplotlibThemes()
    {
        string Hatch(Theme theme, Func<AxesBuilder, AxesBuilder> build, Type kind)
        {
            var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => build(ax)).Build();
            var ctx = new RecordingRenderContext();
            new ChartRenderer().Render(figure, ctx);
            var hatches = ctx.Calls.OfType<ShapeCall>().Where(c => c.GetType() == kind && c.Fill is Color f && f.R == Ink.R && f.G == Ink.G && f.B == Ink.B)
                .Select(c => c.Shape.HatchColor is { } h ? Hex(h) : "none").Distinct();
            return string.Join(" ", hatches);
        }

        AxesBuilder Bar(AxesBuilder ax) => ax.Bar(["a"], [3.0], b => { b.Color = Ink; b.EdgeColor = Colors.Red; b.Alpha = 0.5; b.Hatch = HatchPattern.ForwardDiagonal; });
        AxesBuilder Named(AxesBuilder ax) => ax.Hist([1.0, 2, 2, 3], 3, h => { h.Color = Ink; h.Alpha = 0.5; h.Hatch = HatchPattern.ForwardDiagonal; h.HatchColor = Colors.Red; });
        AxesBuilder Fill(AxesBuilder ax) => ax.FillBetween([1.0, 2, 3], [1.0, 3, 2], null, a => { a.Color = Ink; a.Alpha = 0.5; a.Hatch = HatchPattern.ForwardDiagonal; });

        Assert.Equal("00000080", Hatch(Theme.MatplotlibClassic, Bar, typeof(DrawRectangleCall)));
        Assert.Equal("FF000080", Hatch(Theme.MatplotlibV2, Bar, typeof(DrawRectangleCall)));
        Assert.Equal("FF000080", Hatch(Theme.MatplotlibClassic, Named, typeof(DrawRectangleCall)));
        Assert.Equal("FF000080", Hatch(Theme.MatplotlibV2, Named, typeof(DrawRectangleCall)));
        Assert.Equal("00000080", Hatch(Theme.MatplotlibClassic, Fill, typeof(DrawPolygonCall)));
        Assert.Equal("FF000080", Hatch(Theme.Default, Bar, typeof(DrawRectangleCall)));
        Assert.Equal("FF000080", Hatch(Theme.Default, Named, typeof(DrawRectangleCall)));
    }

    /// <summary>A filled step histogram is hatched by the same rule as its bars: python prints <c>ax.hist([1, 2, 2, 3], histtype='stepfilled',
    /// hatch='/')[2][0]._hatch_color</c> as (0, 0, 0, 1) under the classic style (<c>hatch.color</c> 'k'); the library drew the hatch
    /// colour named only, leaving the rest to the backend under every theme.</summary>
    [Fact]
    public void AFilledStepHistogram_IsHatchedByTheThemesRule_AsItsBarsAre()
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibClassic).AddSubPlot(1, 1, 1, ax =>
            ax.Hist([1.0, 2, 2, 3], 3, h => { h.Color = Ink; h.HistType = HistType.StepFilled; h.Hatch = HatchPattern.ForwardDiagonal; })).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);

        var outline = Assert.Single(ctx.Of<DrawPolygonCall>(), k => k.Fill is Color f && f.R == Ink.R && f.G == Ink.G && f.B == Ink.B);
        Assert.Equal(Colors.Black, outline.Shape.HatchColor);
    }

    [Fact]
    public void UnderALibraryTheme_EveryKindKeepsTheEdgeItHasAlwaysDrawn()
    {
        Assert.Equal("FF0000FF", Strokes(Theme.Default, RedBar, typeof(DrawRectangleCall)));
        Assert.Equal("FF0000FF", Strokes(Theme.Default, RedHist, typeof(DrawRectangleCall)));
        Assert.Equal("none", Strokes(Theme.Default, PlainBar, typeof(DrawRectangleCall)));
        Assert.Equal("B0A001FF", Strokes(Theme.Default, PolarBar, typeof(DrawPolygonCall)));
    }
}
