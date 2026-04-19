// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Phase W (v1.7.2, 2026-04-19) — pins the new
/// <see cref="MatPlotLibNet.Builders.FigureBuilder.WithAutoSize(TreeNode)"/> contract.
/// Opt-in fluent that picks a canvas big enough for a treemap's labels to render at
/// the default 12 pt font without overflow. Most useful for static SVG output (no
/// `WithBrowserInteraction()`) where the user can't pan/zoom to read overflowing
/// labels — "when no browserInteraction he sees all".</summary>
public class FigureBuilderAutoSizeTests
{
    private static (int W, int H) SizeOf(string svg)
    {
        var w = int.Parse(Regex.Match(svg, "<svg[^>]+width=\"(\\d+)").Groups[1].Value);
        var h = int.Parse(Regex.Match(svg, "<svg[^>]+height=\"(\\d+)").Groups[1].Value);
        return (w, h);
    }

    // Realistic ~ 10-char labels at every node so the canvas calculation reflects
    // a real-world treemap (not a synthetic single-letter case where everything floors).
    private static TreeNode BalancedTree(int depth, int fanOut)
    {
        int counter = 0;
        TreeNode Build(int d) => d == 0
            ? new TreeNode { Label = "Leaf-" + (++counter).ToString("D4"), Value = 1 }
            : new TreeNode
            {
                Label = "Branch-" + d.ToString(),
                Children = Enumerable.Range(0, fanOut).Select(_ => Build(d - 1)).ToList(),
            };
        return Build(depth);
    }

    [Fact]
    public void WithAutoSize_Floors_AtDefault_ForTinyTree()
    {
        var small = new TreeNode { Label = "Root", Children = [
            new() { Label = "A", Value = 1 },
            new() { Label = "B", Value = 1 } ] };
        string svg = Plt.Create()
            .WithAutoSize(small)
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(small).HideAllAxes())
            .ToSvg();
        var (w, h) = SizeOf(svg);
        Assert.True(w >= 800, $"floor width 800; got {w}");
        Assert.True(h >= 600, $"floor height 600; got {h}");
    }

    [Fact]
    public void WithAutoSize_GrowsWith_LeafCount()
    {
        // Both trees use realistic ~10-char labels. Small tree may floor at 800×600;
        // big tree (256 leaves) must clear the floor on both axes — strictly larger.
        var small = BalancedTree(depth: 2, fanOut: 4);    // 16 leaves
        var big   = BalancedTree(depth: 4, fanOut: 4);    // 256 leaves

        var (wS, hS) = SizeOf(Plt.Create().WithAutoSize(small)
            .AddSubPlot(1,1,1, ax => ax.Treemap(small).HideAllAxes()).ToSvg());
        var (wB, hB) = SizeOf(Plt.Create().WithAutoSize(big)
            .AddSubPlot(1,1,1, ax => ax.Treemap(big).HideAllAxes()).ToSvg());

        Assert.True(wB > wS, $"more leaves should grow width: small={wS}, big={wB}");
        Assert.True(hB > hS, $"more leaves should grow height: small={hS}, big={hB}");
    }

    [Fact]
    public void WithAutoSize_HoldsAspect_NearSixteenByNine()
    {
        // Aspect contract holds when the canvas exceeds the 800×600 floor (small trees
        // floor at 4:3 by definition). Use a tree large enough to clear the floor on both
        // axes — 256 leaves with 10-char labels is comfortably above.
        var tree = BalancedTree(depth: 4, fanOut: 4);     // 256 leaves
        var (w, h) = SizeOf(Plt.Create().WithAutoSize(tree)
            .AddSubPlot(1,1,1, ax => ax.Treemap(tree).HideAllAxes()).ToSvg());
        var aspect = (double)w / h;
        Assert.InRange(aspect, 1.7, 1.8);   // 16:9 ≈ 1.778
    }
}
