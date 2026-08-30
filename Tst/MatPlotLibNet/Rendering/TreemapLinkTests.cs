// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A treemap node can LEAD somewhere: the rect and its text are wrapped in an SVG <c>&lt;a href&gt;</c>,
/// so drilling into a subtree is a URL and needs no script at all.
/// <para>The library also ships a self-contained click script (<c>WithTreemapDrilldown</c>), and it works in a
/// saved SVG — but a document that injects the SVG as markup never runs it: a <c>&lt;script&gt;</c> inserted
/// through <c>innerHTML</c> does not execute (HTML spec, MDN). A link works everywhere, and the URL is state a
/// server-rendered page can hold.</para></summary>
public class TreemapLinkTests
{
    private static TreeNode Fleet(string? url = "/?process=Ait.Binance", bool? expanded = null) => new()
    {
        Label = "Ait",
        Children =
        [
            new() { Label = "Ait.Binance", Value = 1, Url = url, Expanded = expanded },
            new() { Label = "Ait.Cortex", Value = 1 },
        ],
    };

    private static string Render(TreeNode root) =>
        Plt.Create().WithSize(600, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(root))
            .ToSvg();

    [Fact]
    public void ANodeWithAUrl_IsAnAnchorAroundItsCell()
    {
        string svg = Render(Fleet());

        Assert.Contains("<a href=\"/?process=Ait.Binance\"", svg);
        Assert.Contains("cursor:pointer", svg);
        Assert.Contains("aria-label=\"Ait.Binance\"", svg);
        Assert.Contains("</a>", svg);
    }

    [Fact]
    public void TheAnchorWrapsTheRectAndItsLabel()
    {
        string svg = Render(Fleet());

        int open = svg.IndexOf("<a href", StringComparison.Ordinal);
        int close = svg.IndexOf("</a>", StringComparison.Ordinal);
        string inside = svg[open..close];
        Assert.Contains("<rect", inside);
        Assert.Contains("Ait.Binance", inside);
    }

    [Fact]
    public void ANodeWithoutAUrl_IsNotAnAnchor()
    {
        string svg = Render(Fleet(url: null));

        Assert.DoesNotContain("<a href", svg);
    }

    [Fact]
    public void AnExpandableNode_SaysWhetherItIsOpen()
    {
        Assert.Contains("aria-expanded=\"false\"", Render(Fleet(expanded: false)));
        Assert.Contains("aria-expanded=\"true\"", Render(Fleet(expanded: true)));
        Assert.DoesNotContain("aria-expanded", Render(Fleet()));
    }

    /// <summary>An INTERIOR node links too — that is the whole point of a drill-down: its frame, its header and
    /// everything nested inside it belong to the same anchor.</summary>
    [Fact]
    public void AnInteriorNodeLinks_AndItsChildrenSitInsideThatAnchor()
    {
        var nested = new TreeNode
        {
            Label = "Ait",
            Children =
            [
                new()
                {
                    Label = "Ait.Binance", Url = "/?process=", Expanded = true,
                    Children = [new() { Label = "BinanceKline", Value = 10 }],
                },
            ],
        };

        string svg = Render(nested);

        int open = svg.IndexOf("<a href", StringComparison.Ordinal);
        int close = svg.IndexOf("</a>", StringComparison.Ordinal);
        Assert.Contains("BinanceKline", svg[open..close]);
    }

    [Fact]
    public void TheUrl_IsEscapedForXml()
    {
        string svg = Render(new TreeNode { Children = [new() { Label = "a", Value = 1, Url = "/?p=x&q=y" }] });

        Assert.Contains("href=\"/?p=x&amp;q=y\"", svg);
    }

    [Fact]
    public void TheTreemapsOwnDataAttributes_SurviveTheAnchor()
    {
        string svg = Render(Fleet());

        Assert.Equal(4, Regex.Matches(svg, "data-treemap-node=\"0\\.[01]\"").Count);
    }
}
