// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What a treemap on a live wall needs (council M16, 2026-08-30, measured): an EMPTY tree draws
/// nothing rather than one full-area "healthy" rect; a node whose source went silent wears a HATCH — the
/// fleet's own absence vocabulary, never a colour; and a label that does not fit its rect is fitted or
/// truncated instead of painted across the neighbours (9 of 21 labels overflowed at 20 processes).</summary>
public class TreemapWallTests
{
    private static string Render(TreeNode root, Action<TreemapSeries>? configure = null, double width = 400, double height = 200) =>
        Plt.Create().WithSize(width, height)
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, configure))
            .ToSvg();

    private static int Rects(string svg) => Regex.Matches(svg, "<rect ").Count;

    [Fact]
    public void AChildlessRoot_DrawsNoRect()
    {
        string withChildren = Render(new TreeNode { Label = "Ait", Children = [new() { Label = "a", Value = 1 }] });
        string empty = Render(new TreeNode { Label = "Ait" });

        Assert.Equal(Rects(withChildren) - 2, Rects(empty)); // neither the root frame nor the leaf
        Assert.DoesNotContain("data-treemap-node", empty);
    }

    [Fact]
    public void ANodeWithAHatch_IsHatched()
    {
        var root = new TreeNode
        {
            Children =
            [
                new() { Label = "live", Value = 10 },
                new() { Label = "silent", Value = 10, Hatch = HatchPattern.ForwardDiagonal, HatchColor = Colors.Gray },
            ]
        };

        string svg = Render(root);

        Assert.Contains("<pattern", svg);
        Assert.Single(Regex.Matches(svg, "fill=\"url\\(#"));
    }

    [Fact]
    public void TheDefault_PaintsEveryLabel_AsBefore()
    {
        string svg = Render(Twenty(), s => s.ShowLabels = true);

        Assert.Equal(20, Regex.Matches(svg, ">Ldr#").Count); // every label drawn as text, however small its rect
        Assert.Equal(TreemapLabelFit.Always, new TreemapSeries(Twenty()).LabelFit);
    }

    [Fact]
    public void Fit_DrawsOnlyTheLabelsThatFit()
    {
        string svg = Render(Twenty(), s => s.LabelFit = TreemapLabelFit.Fit, width: 1400, height: 300); // the wall's size

        int drawn = Regex.Matches(svg, ">Ldr#").Count;
        Assert.InRange(drawn, 1, 19);
        Assert.Equal(20, Regex.Matches(svg, "data-treemap-label=\"Ldr#").Count); // the rect still carries it
    }

    [Fact]
    public void Truncate_ShortensWithAnEllipsis_AndKeepsEveryLabel()
    {
        string svg = Render(Twenty(), s => s.LabelFit = TreemapLabelFit.Truncate, width: 1400, height: 300);

        Assert.Contains("…", svg);
        int full = Regex.Matches(svg, ">Ldr#[^<]*[0-9] %<").Count;      // fitted as-is
        int cut = Regex.Matches(svg, ">[^<]*…<").Count;                 // shortened
        Assert.Equal(20, full + cut);
    }

    private static TreeNode Twenty() => new()
    {
        Label = "Ait",
        Children = [.. Enumerable.Range(0, 20).Select(i => new TreeNode { Label = $"Ldr#{i:x4} · {100 + i} %", Value = 40 + (i * 13 % 250) })]
    };
}
