// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Builders;

namespace MatPlotLibNet.Tests;

/// <summary>
/// Wave J.1 branch close-out for <see cref="FigureTemplates"/>. Two missing branches that are reachable
/// from the public API are closed: the multi-panel <c>ScientificPaper</c> overload's title-set arm
/// (L138) and the <c>RenderDendrogram</c> empty-merges early return (L295), exercised via a degenerate
/// 1×1 <c>Clustermap</c> whose distance matrix produces a dendrogram with zero merges. Other
/// <c>RenderDendrogram</c> sub-100 branches (L300, L310, L311) are defensive paths against malformed
/// dendrograms that <c>HierarchicalClustering.Cluster</c> cannot produce.
/// </summary>
public class FigureTemplatesCoverageTests
{
    // ── L138 (true) ───────────────────────────────────────────────────────────
    /// <summary>Multi-panel <see cref="FigureTemplates.ScientificPaper(int,int,Action{AxesBuilder}[],string,double,double)"/>
    /// with a non-null title hits the <c>if (title is not null) builder.WithTitle(title)</c> true arm.
    /// Existing tests only exercise the no-title overload.</summary>
    [Fact]
    public void ScientificPaper_MultiPanel_WithTitle_SetsFigureTitle()
    {
        var builder = FigureTemplates.ScientificPaper(
            rows: 1, cols: 2,
            configures:
            [
                ax => ax.Plot([1.0, 2.0], [1.0, 4.0]),
                ax => ax.Plot([1.0, 2.0], [2.0, 8.0]),
            ],
            title: "Pair of panels");

        var svg = builder.Build().ToSvg();
        Assert.Contains("Pair of panels", svg);
    }

    // ── L295 (true) ───────────────────────────────────────────────────────────
    /// <summary>A 1×1 <see cref="FigureTemplates.Clustermap(double[,], string[], string[])"/> produces
    /// trivial row/col dendrograms whose <c>Merges</c> arrays are empty (HierarchicalClustering returns
    /// <c>Dendrogram([], [0])</c> for n=1). The internal <c>RenderDendrogram</c> early-return at line
    /// 295 fires for both the row and column dendrogram passes — the only natural path to the true arm.</summary>
    [Fact]
    public void Clustermap_DegenerateOneByOne_RendersWithEmptyDendrograms()
    {
        var data = new double[,] { { 42.0 } };

        var builder = FigureTemplates.Clustermap(data);
        var svg = builder.Build().ToSvg();

        Assert.Contains("<svg", svg);
    }

    // ── L138 (false) ──────────────────────────────────────────────────────────
    /// <summary>Sanity guard: the false arm of L138 (no title) still produces a valid figure.
    /// Catches accidental regressions where the optional-title parameter starts being treated
    /// as required.</summary>
    [Fact]
    public void ScientificPaper_MultiPanel_WithoutTitle_ProducesValidFigure()
    {
        var builder = FigureTemplates.ScientificPaper(
            rows: 1, cols: 1,
            configures: [ax => ax.Plot([1.0, 2.0], [1.0, 4.0])]);

        var svg = builder.Build().ToSvg();
        Assert.Contains("<svg", svg);
    }
}
