// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Tests.Rendering;

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
        var root = svg.Elements("svg")[0];
        return ((int)root.Number("width"), (int)root.Number("height"));
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

    private static TreeNode Tree(int groups, int leavesPerGroup, string leafLabel) => new()
    {
        Label = "Root",
        Children = [.. Enumerable.Range(0, groups).Select(g => new TreeNode
        {
            Label = "Group " + g,
            Children = [.. Enumerable.Range(0, leavesPerGroup).Select(l => new TreeNode { Label = leafLabel, Value = l + 1 })],
        })],
    };

    private static IEnumerable<TreeNode> Nodes(TreeNode node) => new[] { node }.Concat(node.Children.SelectMany(Nodes));

    /// <summary>The size <see cref="FigureBuilder.WithAutoSize(TreeNode)"/> gives the figure.</summary>
    private static (int Width, int Height) Canvas(TreeNode root)
    {
        var figure = Plt.Create().WithAutoSize(root).Build();
        return ((int)figure.Width, (int)figure.Height);
    }

    /// <summary>The canvas as the sizing rule is written down (leaf count, average label length over every node, 16:9, a floor of
    /// 800 by 600), over the nodes listed out in full.</summary>
    private static (int Width, int Height) ByTheRule(TreeNode root)
    {
        var nodes = Nodes(root).ToList();
        int leaves = nodes.Count(n => n.Children.Count == 0);
        double averageChars = nodes.Average(n => (double)n.Label.Length);
        double canvasArea = leaves * ((averageChars * 7.0 + 16.0) * 22.0) * 1.5;
        const double aspect = 16.0 / 9.0;
        int width = (int)Math.Sqrt(canvasArea * aspect);
        int height = (int)(width / aspect);
        return (Math.Max(800, width), Math.Max(600, height));
    }

    [Theory]
    [InlineData(40, 30, "Leaf-0001")]
    [InlineData(12, 50, "a longer leaf label")]
    [InlineData(100, 20, "x")]
    public void ABigTree_NeedsTheCanvasThatTheRuleSays(int groups, int leavesPerGroup, string leafLabel)
    {
        var tree = Tree(groups, leavesPerGroup, leafLabel);

        var canvas = Canvas(tree);

        Assert.Equal(ByTheRule(tree), canvas);
        Assert.True(canvas.Width > 800, "the case is meant to grow past the floor");
    }

    [Fact]
    public void ATinyTree_AndALoneLeaf_StandOnTheFloor()
    {
        Assert.Equal((800, 600), Canvas(Tree(2, 2, "a")));
        Assert.Equal((800, 600), Canvas(new TreeNode { Label = "only", Value = 1 }));
    }

    [Fact]
    public void ANodeWithoutALabel_CountsAsNoCharacters()
    {
        // 2 000 leaves with a null label each: only the root's four characters are in the average over 2 001 nodes.
        var leaves = Enumerable.Range(0, 2000).Select(_ => new TreeNode { Label = null!, Value = 1 }).ToList();
        var tree = new TreeNode { Label = "Root", Children = leaves };

        double averageChars = 4.0 / 2001;
        double canvasArea = 2000 * ((averageChars * 7.0 + 16.0) * 22.0) * 1.5;
        int width = (int)Math.Sqrt(canvasArea * (16.0 / 9.0));
        Assert.Equal((Math.Max(800, width), Math.Max(600, (int)(width / (16.0 / 9.0)))), Canvas(tree));
    }
}
