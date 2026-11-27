// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Phase G.7 of the v1.7.2 follow-on plan — static emission tests for
/// <see cref="Transforms.SvgTransform.SankeyHoverScript"/>.
///
/// <para>Mirrors the pattern in <c>SvgLegendToggleTests.cs</c> —
/// script-string-presence assertions on the rendered SVG. Behavioural tests
/// (mouseenter / mouseleave / BFS traversal) live in
/// <c>SankeyHoverTests.cs</c> under the Interaction namespace.</para></summary>
public class SvgSankeyHoverTests
{
    private static readonly SankeyNode[] Nodes = new[]
    {
        new SankeyNode("A"),
        new SankeyNode("B"),
        new SankeyNode("C"),
    };
    private static readonly SankeyLink[] Links = new[]
    {
        new SankeyLink(0, 1, 10),
        new SankeyLink(1, 2, 8),
    };

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Script IIFE markers
        new DrawnToken("WithSankeyHover_EmitsScript", () => Plt.Create()
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes())
            .Build()
            .ToSvg(), "data-sankey-node-id", "data-sankey-link-source", "function reachable(", "function highlight(", "function restore("),
        new DrawnToken("WithSankeyHover_WiresHoverAndFocusListeners", () => Plt.Create()
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes())
            .Build()
            .ToSvg(), "mouseenter", "mouseleave", "'focus'", "'blur'"),
        new DrawnToken("WithSankeyHover_SetsCursorPointerAndTabindex", () => Plt.Create()
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes())
            .Build()
            .ToSvg(), "cursor = 'pointer'", "'tabindex', '0'"),
        new DrawnToken("WithoutSankeyHover_NoScript", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes())
            .Build()
            .ToSvg()) { Absent = ["function reachable("] },
        // Phase 2 of v1.7.2 plan — per-chart self-locate pattern.
        new DrawnToken("WithSankeyHover_PerChartIsolation_SelfLocatesViaCurrentScript", () => Plt.Create()
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes())
            .Build()
            .ToSvg(), "document.currentScript && document.currentScript.parentNode"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
