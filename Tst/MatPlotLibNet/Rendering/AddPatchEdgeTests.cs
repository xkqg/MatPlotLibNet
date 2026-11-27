// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A shape added with <c>AddPatch</c> is edged by the same rule as every other patch: matplotlib 3.11.2 prints, under the
/// classic style (<c>patch.force_edgecolor</c> True, <c>patch.edgecolor</c> <c>'k'</c>), edgecolor <c>#000000ff</c> at linewidth 1.0
/// for a <c>Rectangle</c>, a <c>Circle</c>, a <c>Polygon</c>, an <c>Ellipse</c> and a <c>Wedge</c> that name none, and under the default
/// style <c>#00000000</c> (none). An edge named stays as named, <c>'red'</c> as <c>#ff0000ff</c> and <c>'none'</c> or
/// <c>(0, 0, 0, 0)</c> as <c>#00000000</c> under both styles; <c>linewidth=0</c> draws none; <c>alpha=0.5</c> makes the forced edge
/// <c>#00000080</c> and the named one <c>#ff000080</c>; an <c>Arc</c> is <c>#000000ff</c> under both.
/// <para>The width rests at <c>patch.linewidth</c>: python prints <c>Rectangle((0, 0), 1, 1, edgecolor='red').get_linewidth()</c>,
/// <c>Arc(...).get_linewidth()</c> and <c>FancyArrow(...).get_linewidth()</c> as 1.0 (points) under both styles, 1.389 pixels at the
/// 100 dpi the library draws at; the library's own themes keep the pixel. A width given (<see cref="Patch.EdgeWidth"/>, in pixels) wins
/// under every theme, 1 included (<c>Rectangle(..., linewidth=1.0)</c> keeps it).</para></summary>
public class AddPatchEdgeTests
{
    private static readonly Color Ink = Color.FromHex("#B0A001");
    private static readonly double P = 1.0.PointsInPixels;

    private static RecordingRenderContext Draw(Theme theme, Patch patch)
    {
        var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.AddPatch(patch).SetXLim(-2, 4).SetYLim(-2, 4)).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return ctx;
    }

    /// <summary>The stroke colour and width of the shapes the patch was drawn as (its face is <see cref="Ink"/>), or of every path when
    /// it has none.</summary>
    private static (Color? Stroke, double Width) Edge(RecordingRenderContext ctx, bool faced = true)
    {
        var paths = ctx.Of<DrawPathCall>()
            .Where(c => !faced || (c.Fill is Color f && f.R == Ink.R && f.G == Ink.G && f.B == Ink.B)).ToList();
        Assert.Single(paths);
        return (paths[0].Stroke, paths[0].StrokeThickness);
    }

    private static Patch Shape(string kind) => kind switch
    {
        "rectangle" => new Rectangle(new DataPoint(0, 0), 1, 1) { FaceColor = Ink },
        "circle" => new Circle(new DataPoint(1, 1), 1) { FaceColor = Ink },
        "polygon" => new Polygon([new DataPoint(0, 0), new DataPoint(2, 0), new DataPoint(1, 2)]) { FaceColor = Ink },
        "ellipse" => new Ellipse(new DataPoint(1, 1), 2, 1) { FaceColor = Ink },
        _ => new Wedge(new DataPoint(1, 1), 1, 30, 300) { FaceColor = Ink },
    };

    [Theory]
    [InlineData("rectangle")]
    [InlineData("circle")]
    [InlineData("polygon")]
    [InlineData("ellipse")]
    [InlineData("wedge")]
    public void UnderClassic_AShapeThatNamesNoEdge_IsEdgedInBlack_AndUnderV2AndTheLibraryThemeNot(string kind)
    {
        Assert.Equal((Colors.Black, P), Edge(Draw(Theme.MatplotlibClassic, Shape(kind))));
        // matplotlib's Patch.draw draws an edge of no opacity at a width of 0.
        Assert.Equal((null, 0), Edge(Draw(Theme.MatplotlibV2, Shape(kind))));
        Assert.Equal((null, 1), Edge(Draw(Theme.Default, Shape(kind))));
    }

    [Fact]
    public void AnEdgeNamed_StaysAsNamed_NoneIncluded_AtPatchLineWidthUnderTheMatplotlibThemes_AndAPixelUnderTheLibrarys()
    {
        foreach (var (theme, width) in new[] { (Theme.MatplotlibClassic, P), (Theme.MatplotlibV2, P), (Theme.Default, 1.0) })
        {
            var red = Shape("rectangle"); red.EdgeColor = Colors.Red;
            var none = Shape("rectangle"); none.EdgeColor = Colors.Transparent;

            Assert.Equal((Colors.Red, width), Edge(Draw(theme, red)));
            Assert.Equal((null, width), Edge(Draw(theme, none)));
        }
    }

    [Fact]
    public void AWidthGiven_WinsUnderEveryTheme_OnePixelIncluded_AndThePropertyStillReadsOneAtRest()
    {
        foreach (var theme in new[] { Theme.MatplotlibClassic, Theme.MatplotlibV2, Theme.Default })
        {
            var one = Shape("rectangle"); one.EdgeColor = Colors.Red; one.EdgeWidth = 1;
            Assert.Equal((Colors.Red, 1), Edge(Draw(theme, one)));
        }

        Assert.Equal(1, Shape("rectangle").EdgeWidth);
    }

    /// <summary>A width given is written to the JSON even where it equals the one a new patch reads, so a pixel given is read back as
    /// given and not as the theme's <c>patch.linewidth</c>; a patch given none writes none.</summary>
    [Fact]
    public void AWidthGiven_SurvivesTheJson_AndOneGivenNoneDoesNotWriteIt()
    {
        var one = Shape("rectangle"); one.EdgeColor = Colors.Red; one.EdgeWidth = 1;
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.AddPatch(one)).Build();
        var back = figure.RoundTripped();

        Assert.Equal((Colors.Red, 1), Edge(Draw(Theme.MatplotlibV2, back.SubPlots[0].Patches[0])));
        Assert.Equal(1, one.Describe().EdgeWidth);
        Assert.Null(Shape("rectangle").Describe().EdgeWidth);
    }

    [Fact]
    public void UnderClassic_AWidthOf0DrawsNoEdge_AndAWidthGivenIsTheForcedEdgesWidth()
    {
        var zero = Shape("rectangle"); zero.EdgeWidth = 0;
        var wide = Shape("rectangle"); wide.EdgeWidth = 3;

        Assert.Equal((null, 0), Edge(Draw(Theme.MatplotlibClassic, zero)));
        Assert.Equal((Colors.Black, 3), Edge(Draw(Theme.MatplotlibClassic, wide)));
    }

    [Fact]
    public void UnderClassic_TheOpacityGiven_ReachesTheForcedEdgeAsItReachesANamedOne()
    {
        var forced = Shape("rectangle"); forced.Alpha = 0.5;
        var red = Shape("rectangle"); red.Alpha = 0.5; red.EdgeColor = Colors.Red;

        Assert.Equal((Colors.Black.WithAlpha(128), P), Edge(Draw(Theme.MatplotlibClassic, forced)));
        Assert.Equal((Colors.Red.WithAlpha(128), P), Edge(Draw(Theme.MatplotlibClassic, red)));
    }

    [Fact]
    public void AnArcAndAnArrow_AreEdgedInBlackUnderEveryTheme_AtTheThemesPatchLineWidth()
    {
        foreach (var (theme, width) in new[] { (Theme.MatplotlibClassic, P), (Theme.MatplotlibV2, P), (Theme.Default, 1.0) })
        {
            Assert.Equal((Colors.Black, width), Edge(Draw(theme, new Arc(new DataPoint(1, 1), 2, 1)), faced: false));
            var arrow = new Arrow(new DataPoint(0, 0), 2, 1) { FaceColor = Ink };
            Assert.Equal((Colors.Black, width), Edge(Draw(theme, arrow)));
        }
    }

    /// <summary>A legend row of a patch is a rectangle in the patch's own style, matplotlib's <c>HandlerPatch</c> copying its edge: it is
    /// edged in black under classic with the patch.</summary>
    [Fact]
    public void UnderClassic_TheLegendRowOfAPatchWithNoEdgeNamed_IsEdgedInBlack()
    {
        var patch = Shape("rectangle"); patch.Label = "patch";
        var figure = Plt.Create().WithTheme(Theme.MatplotlibClassic).AddSubPlot(1, 1, 1, ax => ax.AddPatch(patch).SetXLim(-2, 4).SetYLim(-2, 4).WithLegend()).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);

        var handle = ctx.Of<DrawRectangleCall>().Last(c => c.Fill is Color f && f.R == Ink.R && f.G == Ink.G && f.B == Ink.B);
        Assert.Equal(Colors.Black, handle.Stroke);
        Assert.Equal(P, handle.StrokeThickness);
    }

    /// <summary>An edge named as none is a value given, and is written even though a new patch's edge is none too: read back under
    /// classic, it is still none, not the forced black of a patch that named nothing.</summary>
    [Fact]
    public void AnEdgeNamedNone_SurvivesTheJson_UnderClassic()
    {
        var none = Shape("rectangle"); none.EdgeColor = Colors.Transparent;
        var plain = Shape("rectangle");

        foreach (var (patch, edge) in new[] { (none, (Color?)null), (plain, Colors.Black) })
        {
            var figure = Plt.Create().WithTheme(Theme.MatplotlibClassic).AddSubPlot(1, 1, 1, ax => ax.AddPatch(patch)).Build();
            var back = figure.RoundTripped();

            Assert.Equal(figure.ToSvg(), back.ToSvg());
            Assert.Equal(edge, Edge(Draw(Theme.MatplotlibClassic, back.SubPlots[0].Patches[0])).Stroke);
        }
        Assert.Equal(Colors.Transparent, none.Describe().EdgeColor);
        Assert.Null(plain.Describe().EdgeColor);
    }
}
