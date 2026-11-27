// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The colour of a hatch where its author names none. matplotlib 3.11.2: <c>rcParams['hatch.color']</c> is <c>'edge'</c> in
/// the default style and <c>'k'</c> in the classic one, and <c>Patch.get_hatchcolor()</c> for <c>'edge'</c> is the edge colour, or
/// <c>rcParams['patch.edgecolor']</c> (black) when the edge is fully transparent: so the hatch of a v2 patch is drawn in its edge
/// colour, black when it has none, and the hatch of a classic one is black whatever its edge. matplotlib has the one rule for every
/// patch, a bar's as a Rectangle's, and so has the library: under its own themes a hatch is in the edge its author gave, else in the
/// colour that reads best against its fill (black on a light one, white on a dark one), black over a fill no one can see. A hatch
/// colour that is named is drawn as named, at the opacity given to its shape.</summary>
public class HatchColorTests
{
    private static readonly string[] Categories = ["a", "b"];
    private static readonly double[] Values = [3.0, 5.0];
    private static readonly double[] X = [0.0, 1.0, 2.0, 3.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0, 4.0];

    private static readonly Theme V2 = Theme.MatplotlibV2;
    private static readonly Theme Classic = Theme.MatplotlibClassic;

    private static XDocument Doc(Theme theme, Action<AxesBuilder> build) =>
        Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, build).ToSvg().Xml();

    /// <summary>The colours the hatch strokes of the picture are drawn in: the marks inside its pattern tiles that are not the tile's
    /// own background.</summary>
    private static List<string> HatchColours(XDocument doc) =>
        [.. doc.Each("pattern").SelectMany(p => p.Elements())
            .Where(e => e.Name.LocalName != "rect")
            .Select(e => (string?)e.Attribute("stroke") ?? (string?)e.Attribute("fill") ?? "?").Distinct()];

    private static Action<AxesBuilder> Bar(Action<BarSeries> configure) =>
        ax => ax.Bar(Categories, Values, s => { s.Hatch = HatchPattern.ForwardDiagonal; configure(s); });

    // ── a bar: no edge unless it names a width ────────────────────────────────────────────────────────────────────

    [Fact]
    public void UnderV2_ABarsHatchThatNamesNoColour_IsBlack_BecauseItHasNoEdge()
    {
        Assert.Equal(["#000000"], HatchColours(Doc(V2, Bar(_ => { }))));
    }

    [Fact]
    public void UnderV2_ABarsHatch_FollowsItsEdgeColour()
    {
        var doc = Doc(V2, Bar(s => { s.EdgeColor = Colors.Red; s.LineWidth = 1; }));

        Assert.Equal([Colors.Red.ToHex()], HatchColours(doc));
    }

    [Fact]
    public void UnderClassic_ABarsHatch_IsBlack_WhateverItsEdge()
    {
        Assert.Equal(["#000000"], HatchColours(Doc(Classic, Bar(_ => { }))));
        Assert.Equal(["#000000"], HatchColours(Doc(Classic, Bar(s => { s.EdgeColor = Colors.Red; s.LineWidth = 1; }))));
    }

    [Fact]
    public void UnderTheLibrarysThemes_ABarsHatch_IsTheColourThatReadsBestAgainstItsFill_AsBefore()
    {
        var colours = HatchColours(Doc(Theme.Default, Bar(_ => { })));

        Assert.Single(colours);
        Assert.Equal(Theme.Default.CycleColors[0].ContrastingTextColor().ToHex(), colours[0]);
    }

    /// <summary>One rule for every patch under the library's themes, as matplotlib has one for every patch (a bar is a
    /// <c>Rectangle</c>: <c>ax.bar(.., hatch='//', edgecolor='red').get_hatchcolor()</c> is red, as a Rectangle's is): a hatch
    /// follows the edge its author gave.</summary>
    [Fact]
    public void UnderTheLibrarysThemes_ABarsHatch_FollowsTheEdgeItsAuthorGave_AsAPatchsDoes()
    {
        var doc = Doc(Theme.Default, Bar(s => { s.EdgeColor = Colors.Red; s.LineWidth = 1; }));

        Assert.Equal([Colors.Red.ToHex()], HatchColours(doc));
    }

    /// <summary>A patch given no edge is hatched as a bar given none: in the colour that reads best against its face.</summary>
    [Fact]
    public void UnderTheLibrarysThemes_APatchsHatchWithNoEdge_ReadsBestAgainstItsFace_AsABarsDoes()
    {
        var face = Theme.Default.CycleColors[0];
        var doc = Doc(Theme.Default, ax => ax.Plot(X, Y).AddPatch(new Rectangle(new(0.5, 1.0), 1.0, 1.0) { Hatch = HatchPattern.ForwardDiagonal }));

        Assert.Equal([face.ContrastingTextColor().ToHex()], HatchColours(doc));
    }

    /// <summary>Over a fill no one can see there is nothing to read against: the hatch is in the edge its author gave, black when
    /// none (matplotlib's <c>patch.edgecolor</c>), for a bar as for a patch.</summary>
    [Fact]
    public void UnderTheLibrarysThemes_AHatchOverAClearFill_IsTheEdgeItsAuthorGave_ElseBlack()
    {
        Assert.Equal(["#000000"], HatchColours(Doc(Theme.Default,
            ax => ax.Plot(X, Y).AddPatch(new Rectangle(new(0.5, 1.0), 1.0, 1.0) { Hatch = HatchPattern.ForwardDiagonal, FaceColor = Colors.Transparent }))));
    }

    /// <summary>A hatch colour named takes the opacity given to its shape, as matplotlib's <c>_set_hatchcolor</c> makes it
    /// (<c>colors.to_rgba(color, self._alpha)</c>), for a bar as for a patch, under every theme.</summary>
    [Fact]
    public void AHatchColourThatIsNamed_TakesTheOpacityGivenToItsShape_UnderTheLibrarysThemesToo()
    {
        var bar = new BarSeries(Categories, Values) { Hatch = HatchPattern.Cross, HatchColor = Colors.Green, Alpha = 0.5 };

        var style = Theme.Default.PatchEdgesOf(bar).StyleOf(Colors.Blue.WithOpacity(0.5), (Color?)null, bar.Hatching);

        Assert.Equal(Colors.Green.WithOpacity(0.5), style.HatchColor);
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    [InlineData("library")]
    public void AHatchColourThatIsNamed_IsDrawnAsNamed(string theme)
    {
        var chosen = theme switch { "v2" => V2, "classic" => Classic, _ => Theme.Default };

        Assert.Equal([Colors.Green.ToHex()], HatchColours(Doc(chosen, Bar(s => { s.HatchColor = Colors.Green; s.EdgeColor = Colors.Red; s.LineWidth = 1; }))));
    }

    // ── the other filled series matplotlib hatches ────────────────────────────────────────────────────────────────

    [Fact]
    public void AHistogramAnAreaAStackAPieAndAFilledContour_FollowTheSameRule()
    {
        var charts = new Dictionary<string, Action<AxesBuilder>>
        {
            ["hist"] = ax => ax.Hist([1.0, 2, 2, 3, 3, 3, 4], 4, s => s.Hatch = HatchPattern.ForwardDiagonal),
            ["area"] = ax => ax.FillBetween(X, Y, null, s => s.Hatch = HatchPattern.ForwardDiagonal),
            ["stack"] = ax => ax.StackPlot(X, [Y, Y], s => s.Hatch = HatchPattern.ForwardDiagonal),
            ["pie"] = ax => ax.Pie([1.0, 2.0], null, s => s.Hatches = [HatchPattern.ForwardDiagonal, HatchPattern.ForwardDiagonal]),
            ["contourf"] = ax => ax.Contourf([0.0, 1, 2], [0.0, 1, 2], new double[,] { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } }, s => s.Hatches = [HatchPattern.ForwardDiagonal, HatchPattern.ForwardDiagonal]),
        };

        foreach (var (name, build) in charts)
        {
            Assert.True(HatchColours(Doc(V2, build)) is ["#000000"], $"{name} under v2");
            Assert.True(HatchColours(Doc(Classic, build)) is ["#000000"], $"{name} under classic");
        }
    }

    // ── a patch: its edge colour, black when it has none; black under classic ─────────────────────────────────────

    private static Action<AxesBuilder> Patch(Color edge) =>
        ax => ax.Plot(X, Y).AddPatch(new Rectangle(new(0.5, 1.0), 1.0, 1.0) { Hatch = HatchPattern.ForwardDiagonal, EdgeColor = edge });

    [Fact]
    public void APatchsHatch_FollowsItsEdgeUnderV2_AndTheLibrarysThemes_ButIsBlackUnderClassic()
    {
        Assert.Contains(Colors.Red.ToHex(), HatchColours(Doc(V2, Patch(Colors.Red))));
        Assert.Contains(Colors.Red.ToHex(), HatchColours(Doc(Theme.Default, Patch(Colors.Red))));
        Assert.Equal(["#000000"], HatchColours(Doc(Classic, Patch(Colors.Red))));
        Assert.Equal(["#000000"], HatchColours(Doc(V2, Patch(Colors.Transparent))));
    }

    // ── a pie's slice and a treemap's tile: the same rule as every other patch ───────────────────────────────────

    /// <summary>A theme that draws patches as matplotlib does and edges every one in red, whose hatch follows its edge (matplotlib's
    /// <c>hatch.color = 'edge'</c> with <c>patch.force_edgecolor</c> and <c>patch.edgecolor = 'r'</c>).</summary>
    private static readonly Theme RedEdges = new("red edges", Theme.MatplotlibV2)
    {
        Looks = Theme.MatplotlibV2.Looks with
        {
            Patch = Theme.MatplotlibV2.Looks.Patch with { ForceEdge = true, EdgeColor = Colors.Red, EdgeWidth = 1 },
        },
    };

    [Fact]
    public void AHatchedPieSlice_FollowsItsEdgeColour_AsEveryOtherPatchDoes()
    {
        var doc = Doc(RedEdges, ax => ax.Pie([1.0, 2.0], null, s => s.Hatches = [HatchPattern.ForwardDiagonal, HatchPattern.ForwardDiagonal]));

        Assert.Equal([Colors.Red.ToHex()], HatchColours(doc));
    }

    [Fact]
    public void AHatchedPieSlice_IsBlackWhereItHasNoEdge_UnderV2_AndUnderClassic()
    {
        Action<AxesBuilder> pie = ax => ax.Pie([1.0, 2.0], null, s => s.Hatches = [HatchPattern.ForwardDiagonal, HatchPattern.ForwardDiagonal]);

        Assert.Equal(["#000000"], HatchColours(Doc(V2, pie)));
        Assert.Equal(["#000000"], HatchColours(Doc(Classic, pie)));
    }

    private static TreeNode Tiles(Color? hatchColor = null) => new()
    {
        Label = "root",
        Children = [new TreeNode { Label = "a", Value = 3, Hatch = HatchPattern.ForwardDiagonal, HatchColor = hatchColor }, new TreeNode { Label = "b", Value = 2 }],
    };

    [Fact]
    public void AHatchedTreemapTile_IsBlackUnderV2AndClassic_BecauseItsEdgeIsNoneOrBlack_AsABarsIs()
    {
        Assert.Equal(["#000000"], HatchColours(Doc(V2, ax => ax.Treemap(Tiles()))));
        Assert.Equal(["#000000"], HatchColours(Doc(Classic, ax => ax.Treemap(Tiles()))));
    }

    [Fact]
    public void AHatchedTreemapTile_FollowsItsEdgeColour_UnderAThemeThatEdgesEveryPatch()
    {
        Assert.Equal([Colors.Red.ToHex()], HatchColours(Doc(RedEdges, ax => ax.Treemap(Tiles()))));
    }

    [Fact]
    public void AHatchedTreemapTile_IsTheColourThatReadsBestAgainstItsFill_UnderTheLibrarysThemes_AsBefore()
    {
        var colours = HatchColours(Doc(Theme.Default, ax => ax.Treemap(Tiles())));

        Assert.Single(colours);
    }

    [Fact]
    public void AHatchColourThatANodeNames_IsDrawnAsNamed_UnderEveryTheme()
    {
        foreach (var theme in new[] { V2, Classic, Theme.Default, RedEdges })
        {
            Assert.Equal([Colors.Green.ToHex()], HatchColours(Doc(theme, ax => ax.Treemap(Tiles(Colors.Green)))));
        }
    }

    // ── the setting travels with the theme ────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheRule_IsTheMatplotlibThemes_AndSurvivesTheThemeJson()
    {
        Assert.True(Theme.MatplotlibV2.Looks.Patch.Hatch!.Value.FollowsEdge);
        Assert.Equal(Colors.Black, Theme.MatplotlibV2.Looks.Patch.Hatch!.Value.Fallback);
        Assert.False(Theme.MatplotlibClassic.Looks.Patch.Hatch!.Value.FollowsEdge);
        Assert.Equal(Colors.Black, Theme.MatplotlibClassic.Looks.Patch.Hatch!.Value.Fallback);
        Assert.Null(Theme.Default.Looks.Patch.Hatch);
        Assert.False(Theme.MatplotlibClassic.ThroughDto().Looks.Patch.Hatch!.Value.FollowsEdge);
        Assert.True(Theme.MatplotlibV2.ThroughDto().Looks.Patch.Hatch!.Value.FollowsEdge);
        Assert.Null(Theme.Default.ThroughDto().Looks.Patch.Hatch);
        Assert.False(Theme.MatplotlibClassic.Cloned().Looks.Patch.Hatch!.Value.FollowsEdge);
    }
}
