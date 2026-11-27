// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
    private static TreeNode Fleet(string? url = "/?process=Svc.Billing", bool? expanded = null) => new()
    {
        Label = "Svc",
        Children =
        [
            new() { Label = "Svc.Billing", Value = 1, Url = url, Expanded = expanded },
            new() { Label = "Svc.Ingest", Value = 1 },
        ],
    };

    private static string Render(TreeNode root) =>
        Plt.Create().WithSize(600, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(root))
            .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ANodeWithAUrl_IsAnAnchorAroundItsCell", () => Render(Fleet()), "<a href=\"/?process=Svc.Billing\"", "cursor:pointer", "aria-label=\"Svc.Billing\"", "</a>"),
        new DrawnToken("ANodeWithoutAUrl_IsNotAnAnchor", () => Render(Fleet(url: null))) { Absent = ["<a href"] },
        new DrawnToken("TheUrl_IsEscapedForXml", () => Render(new TreeNode { Children = [new() { Label = "a", Value = 1, Url = "/?p=x&q=y" }] }), "href=\"/?p=x&amp;q=y\""),
        new DrawnToken("TheAnchorWrapsTheRectAndItsLabel", () =>
        {
            string svg = Render(Fleet());
            int open = svg.IndexOf("<a href", StringComparison.Ordinal);
            int close = svg.IndexOf("</a>", StringComparison.Ordinal);
            return svg[open..close];
        }, "<rect", "Svc.Billing"),
        new DrawnToken("AnExpandableNode_SaysWhetherItIsOpen (#1)", () => Render(Fleet(expanded: false)), "aria-expanded=\"false\""),
        new DrawnToken("AnExpandableNode_SaysWhetherItIsOpen (#2)", () => Render(Fleet(expanded: true)), "aria-expanded=\"true\""),
        new DrawnToken("AnExpandableNode_SaysWhetherItIsOpen (#3)", () => Render(Fleet())) { Absent = ["aria-expanded"] },
        // An INTERIOR node links too — that is the whole point of a drill-down: its frame, its header and everything nested inside it
        // belong to the same anchor.
        new DrawnToken("AnInteriorNodeLinks_AndItsChildrenSitInsideThatAnchor", () =>
        {
            var nested = new TreeNode
            {
                Label = "Svc",
                Children =
                [
                    new()
                    {
                        Label = "Svc.Billing", Url = "/?process=", Expanded = true,
                        Children = [new() { Label = "BillingCycle", Value = 10 }],
                    },
                ],
            };
            string svg = Render(nested);
            int open = svg.IndexOf("<a href", StringComparison.Ordinal);
            int close = svg.IndexOf("</a>", StringComparison.Ordinal);
            return svg[open..close];
        }, "BillingCycle"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void TheTreemapsOwnDataAttributes_SurviveTheAnchor()
    {
        string svg = Render(Fleet());

        Assert.Equal(4, svg.AttributeValues("data-treemap-node").Count(node => node is "0.0" or "0.1"));
    }
}
