// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase W (v1.7.2, 2026-04-19) — pins the constant-font + always-render
/// contract for treemap labels. Pre-W the renderer shrank fonts with depth
/// (`Math.Max(8.0, 14.0 - depth * 1.5)`) AND hid labels via a rect-fit gate. Post-W
/// every label renders at a single readable 12 pt size at every depth, even when its
/// tile is narrower than the text. Children paint OVER parents (Shneiderman z-order),
/// so the visible label in any region is the deepest-visible one. Users navigate
/// overflowing labels via `WithBrowserInteraction()` pan/zoom; static SVG users get
/// the full hierarchy without information loss ("when no browserInteraction he sees all").</summary>
public class TreemapSeriesRendererTests
{
    private static string RenderSvg(TreeNode root, int width, int height) =>
        Plt.Create()
           .WithSize(width, height)
           .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, s => s.ShowLabels = true).HideAllAxes())
           .ToSvg();

    /// <summary>Find the <c>font-size="N"</c> attribute on the &lt;text&gt; element whose
    /// content equals <paramref name="label"/>. Returns the numeric size or NaN if not
    /// found. Tolerates element-attribute order variations.</summary>
    private static double FontSizeOfLabel(string svg, string label)
    {
        // Match <text ... font-size="X" ... >label</text>  OR  <text ...>label</text>
        // where font-size could come from a parent/attribute. Simplest: scan every
        // <text ...>label</text> and parse its font-size from the same tag.
        return svg.Elements("text").Where(element => element.Text == label && element["font-size"] is not null)
            .Select(element => element.Number("font-size")).DefaultIfEmpty(double.NaN).First();
    }

    [Fact]
    public void Font_IsConstant_AcrossDepths()
    {
        // Identical-named "Big" leaves at depth 1 vs depth 5 must produce identical
        // font sizes. Pre-W: depth-1 = 14 pt, depth-5 = 8 pt floor. Post-W: 12 pt both.
        var depth1 = new TreeNode { Label = "Root", Children = [
            new() { Label = "Big", Value = 100 } ] };
        var depth5 = new TreeNode { Label = "Root", Children = [
            new() { Label = "A", Value = 100, Children = [
                new() { Label = "B", Value = 100, Children = [
                    new() { Label = "C", Value = 100, Children = [
                        new() { Label = "D", Value = 100, Children = [
                            new() { Label = "Big", Value = 100 } ] } ] } ] } ] } ] };

        var f1 = FontSizeOfLabel(RenderSvg(depth1, 800, 600), "Big");
        var fE = FontSizeOfLabel(RenderSvg(depth5, 800, 600), "Big");
        f1.AssertFinite("depth-1 'Big' label not found in SVG");
        fE.AssertFinite("depth-5 'Big' label not found in SVG");
        // The figure's default theme is matplotlib's v2 style: a name that names no size is its font size, at every depth.
        Assert.Equal(Math.Round(Theme.MatplotlibV2.DefaultFont.Size, 2), f1);
        Assert.Equal(f1, fE);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Label_RendersEvenWhenTileNarrowerThanText", () =>
        {
            // A leaf with a long label inside a tiny rect must STILL render the label.
            // Pre-W the rect-fit gate (`size.Width + 8 <= bounds.Width`) hid it; post-W
            // it overflows visually but the data is in the SVG. Users zoom in via
            // `WithBrowserInteraction()`, or call `.WithAutoSize(root)` for a canvas big
            // enough to avoid overflow. "When no browserInteraction he sees all."
            var tree = new TreeNode { Label = "Root", Children = [
                new() { Label = "Big",         Value = 100 },
                new() { Label = "Smartphones", Value = 1   },   // tiny tile, long label
            ]};
            return RenderSvg(tree, 400, 300);
        }, ">Big<", ">Smartphones<"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A value that is not a number, or an infinite one, makes the total of its siblings one too, so they have no area
    /// to share: the parent is drawn and its children are not, as a sunburst leaves them out. Before, every child was drawn at
    /// coordinates of "NaN", which no SVG reader can place.</summary>
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ChildrenWhoseTotalIsNotAFiniteNumber_AreNotDrawn(double value)
    {
        var tree = new TreeNode { Label = "Root", Children = [
            new() { Label = "Apples",  Value = 3 },
            new() { Label = "Unknown", Value = value },
            new() { Label = "Pears",   Value = 1 },
        ]};

        string svg = RenderSvg(tree, 400, 300);

        Assert.Contains(">Root<", svg);
        svg.AssertNoNonFiniteNumber();
        Assert.DoesNotContain(">Apples<", svg);
    }
}
