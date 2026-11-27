// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
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

    [Fact]
    public void AChildlessRoot_DrawsNoRect()
    {
        string withChildren = Render(new TreeNode { Label = "Svc", Children = [new() { Label = "a", Value = 1 }] });
        string empty = Render(new TreeNode { Label = "Svc" });

        // The root frame and the leaf are the two nodes of the tree with a child; the tree without one draws neither. (A clip's own rect is
        // not a node: the matplotlib themes clip the tiles to the axes, as matplotlib clips a bar.)
        Assert.Equal(2, withChildren.Elements("rect").Count(rect => rect["data-treemap-node"] is not null));
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
        Assert.Single(svg.AllElements(), element => element["fill"]?.StartsWith("url(#", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void TheDefault_PaintsEveryLabel_AsBefore()
    {
        string svg = Render(Twenty(), s => s.ShowLabels = true);

        Assert.Equal(20, svg.Elements("text").Count(text => text.Text.StartsWith("Ldr#", StringComparison.Ordinal))); // every label drawn as text, however small its rect
    }

    [Fact]
    public void Fit_DrawsOnlyTheLabelsThatFit()
    {
        string svg = Render(Twenty(), s => s.LabelFit = TreemapLabelFit.Fit, width: 1400, height: 300); // the wall's size

        int drawn = svg.Elements("text").Count(text => text.Text.StartsWith("Ldr#", StringComparison.Ordinal));
        Assert.InRange(drawn, 1, 19);
        Assert.Equal(20, svg.AttributeValues("data-treemap-label").Count(label => label.StartsWith("Ldr#", StringComparison.Ordinal))); // the rect still carries it
    }

    [Fact]
    public void Truncate_ShortensWithAnEllipsis_AndKeepsEveryLabel()
    {
        string svg = Render(Twenty(), s => s.LabelFit = TreemapLabelFit.Truncate, width: 1400, height: 300);

        Assert.Contains("…", svg);
        int full = svg.Elements("text").Count(text => text.Text.StartsWith("Ldr#", StringComparison.Ordinal) && text.Text.EndsWith(" %", StringComparison.Ordinal) && char.IsAsciiDigit(text.Text[^3]));      // fitted as-is
        int cut = svg.Elements("text").Count(text => text.Text.EndsWith('…'));                 // shortened
        Assert.Equal(20, full + cut);
    }

    private static TreeNode Twenty() => new()
    {
        Label = "Svc",
        Children = [.. Enumerable.Range(0, 20).Select(i => new TreeNode { Label = $"Ldr#{i:x4} · {100 + i} %", Value = 40 + (i * 13 % 250) })]
    };
}
