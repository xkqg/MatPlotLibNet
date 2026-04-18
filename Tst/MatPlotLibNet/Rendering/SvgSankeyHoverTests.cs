// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Phase G.7 of the v1.7.2 follow-on plan — static emission tests for
/// <see cref="MatPlotLibNet.Rendering.Svg.SvgSankeyHoverScript"/>.
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

    [Fact]
    public void WithSankeyHover_EmitsScript()
    {
        var svg = Plt.Create()
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes())
            .Build()
            .ToSvg();
        Assert.Contains("data-sankey-node-id", svg);
        Assert.Contains("data-sankey-link-source", svg);
        // Script IIFE markers
        Assert.Contains("function reachable(", svg);
        Assert.Contains("function highlight(", svg);
        Assert.Contains("function restore(", svg);
    }

    [Fact]
    public void WithSankeyHover_WiresHoverAndFocusListeners()
    {
        var svg = Plt.Create()
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes())
            .Build()
            .ToSvg();
        Assert.Contains("mouseenter", svg);
        Assert.Contains("mouseleave", svg);
        Assert.Contains("'focus'", svg);
        Assert.Contains("'blur'", svg);
    }

    [Fact]
    public void WithSankeyHover_SetsCursorPointerAndTabindex()
    {
        var svg = Plt.Create()
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes())
            .Build()
            .ToSvg();
        Assert.Contains("cursor = 'pointer'", svg);
        Assert.Contains("'tabindex', '0'", svg);
    }

    [Fact]
    public void WithoutSankeyHover_NoScript()
    {
        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes())
            .Build()
            .ToSvg();
        Assert.DoesNotContain("function reachable(", svg);
    }

    [Fact]
    public void WithSankeyHover_PerChartIsolation_SelfLocatesViaCurrentScript()
    {
        var svg = Plt.Create()
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes())
            .Build()
            .ToSvg();
        // Phase 2 of v1.7.2 plan — per-chart self-locate pattern.
        Assert.Contains("document.currentScript && document.currentScript.parentNode", svg);
    }
}
