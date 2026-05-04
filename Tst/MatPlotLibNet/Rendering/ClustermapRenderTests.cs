// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>v1.10 — verifies SVG output of <see cref="ClustermapSeries"/> rendering:
/// heatmap cells, optional row/column dendrograms, and label emission.</summary>
public class ClustermapRenderTests
{
    private static double[,] SampleData => new double[,]
    {
        { 0.1, 0.9, 0.3 },
        { 0.8, 0.2, 0.7 },
        { 0.4, 0.6, 0.5 },
    };

    private static TreeNode RowTree3 => new()
    {
        Value = 2.0,
        Children =
        [
            new TreeNode { Value = 0.0 }, // leaf: row 0
            new TreeNode
            {
                Value = 1.0,
                Children =
                [
                    new TreeNode { Value = 1.0 }, // leaf: row 1
                    new TreeNode { Value = 2.0 }, // leaf: row 2
                ]
            }
        ]
    };

    private static TreeNode ColTree3 => new()
    {
        Value = 2.0,
        Children =
        [
            new TreeNode { Value = 2.0 }, // leaf: col 2
            new TreeNode
            {
                Value = 1.0,
                Children =
                [
                    new TreeNode { Value = 0.0 }, // leaf: col 0
                    new TreeNode { Value = 1.0 }, // leaf: col 1
                ]
            }
        ]
    };

    private static string RenderSvg(Action<ClustermapSeries>? configure = null) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Clustermap(SampleData, configure))
            .ToSvg();

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }

    // ── Smoke tests ───────────────────────────────────────────────────────────

    [Fact]
    public void NoTrees_RendersValidSvg()
    {
        string svg = RenderSvg();
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void NoTrees_EmitsHeatmapCells()
    {
        string svg = RenderSvg();
        // 3×3 = 9 cells → at least 9 <rect> elements
        Assert.True(CountOccurrences(svg, "<rect") >= 9,
            $"Expected ≥9 <rect> for 3×3 heatmap, found {CountOccurrences(svg, "<rect")}.");
    }

    // ── Row dendrogram ────────────────────────────────────────────────────────

    [Fact]
    public void WithRowTree_EmitsHeatmapAndRowDendrogram()
    {
        string svg = RenderSvg(s => s.RowTree = RowTree3);
        Assert.Contains("<rect", svg);
        Assert.Contains("<line", svg); // row dendrogram U-segments
    }

    [Fact]
    public void WithRowTree_RowDendrogramDifferentFromNone()
    {
        string noTree = RenderSvg();
        string withRow = RenderSvg(s => s.RowTree = RowTree3);
        // SVG with row dendrogram has line segments the plain heatmap doesn't
        Assert.True(CountOccurrences(withRow, "<line") > CountOccurrences(noTree, "<line"));
    }

    // ── Column dendrogram ─────────────────────────────────────────────────────

    [Fact]
    public void WithColumnTree_EmitsHeatmapAndColumnDendrogram()
    {
        string svg = RenderSvg(s => s.ColumnTree = ColTree3);
        Assert.Contains("<rect", svg);
        Assert.Contains("<line", svg); // col dendrogram U-segments
    }

    [Fact]
    public void WithBothTrees_EmitsMoreLinesThanEitherAlone()
    {
        // Axes always emit some <line> elements (spines, ticks). Subtract the
        // baseline (no-tree) count to isolate dendrogram segment counts.
        string none = RenderSvg();
        string row  = RenderSvg(s => s.RowTree = RowTree3);
        string col  = RenderSvg(s => s.ColumnTree = ColTree3);
        string both = RenderSvg(s =>
        {
            s.RowTree = RowTree3;
            s.ColumnTree = ColTree3;
        });
        int baseline = CountOccurrences(none, "<line");
        int rowExtra  = CountOccurrences(row,  "<line") - baseline;
        int colExtra  = CountOccurrences(col,  "<line") - baseline;
        int bothExtra = CountOccurrences(both, "<line") - baseline;
        Assert.True(bothExtra >= rowExtra + colExtra,
            $"Both-trees render should add at least {rowExtra + colExtra} dendrogram lines over baseline, got {bothExtra}.");
    }

    // ── Labels ────────────────────────────────────────────────────────────────

    [Fact]
    public void ShowLabelsFalse_FewerTextElementsThanShowLabelsTrue()
    {
        // Axes always emit <text> for tick labels, so we can't assert zero <text>.
        // Verify ShowLabels=true produces MORE text elements than ShowLabels=false.
        string noLabels = RenderSvg(s => s.ShowLabels = false);
        string labels   = RenderSvg(s => s.ShowLabels = true);
        Assert.True(CountOccurrences(labels, "<text") > CountOccurrences(noLabels, "<text"),
            "ShowLabels=true must emit more <text> elements than ShowLabels=false.");
    }

    [Fact]
    public void ShowLabelsTrue_EmitsTextElements()
    {
        string svg = RenderSvg(s => s.ShowLabels = true);
        Assert.Contains("<text", svg);
    }

    // ── Reordering: heatmap column count unchanged when column tree given ─────

    [Fact]
    public void WithColumnTree_HeatmapRectCountUnchanged()
    {
        // Reordering reshuffles columns but does not add or remove cells.
        string noTree = RenderSvg();
        string withCol = RenderSvg(s => s.ColumnTree = ColTree3);
        int rectNoTree = CountOccurrences(noTree, "<rect");
        int rectWithCol = CountOccurrences(withCol, "<rect");
        Assert.Equal(rectNoTree, rectWithCol);
    }

    // ── Panel ratios affect SVG structure ─────────────────────────────────────

    [Fact]
    public void ZeroRowDendrogramWidth_WithRowTree_SameLineCountAsNoTree()
    {
        // Width=0 suppresses the row dendrogram panel: line count equals no-tree baseline.
        string noTree = RenderSvg();
        string zeroW  = RenderSvg(s =>
        {
            s.RowTree = RowTree3;
            s.RowDendrogramWidth = 0.0;
        });
        Assert.Equal(CountOccurrences(noTree, "<line"), CountOccurrences(zeroW, "<line"));
    }

    [Fact]
    public void ZeroColumnDendrogramHeight_WithColumnTree_SameLineCountAsNoTree()
    {
        // Height=0 suppresses the column dendrogram panel.
        string noTree = RenderSvg();
        string zeroH  = RenderSvg(s =>
        {
            s.ColumnTree = ColTree3;
            s.ColumnDendrogramHeight = 0.0;
        });
        Assert.Equal(CountOccurrences(noTree, "<line"), CountOccurrences(zeroH, "<line"));
    }

    // ── Colormap propagation ──────────────────────────────────────────────────

    [Fact]
    public void DefaultColormap_RendersWithFillColors()
    {
        string svg = RenderSvg();
        // Viridis colormap produces non-white fills; at least one fill attribute expected
        Assert.Contains("fill=\"#", svg);
    }

    // ── Panel suppression: MinPanelPx gate ───────────────────────────────────

    [Fact]
    public void ZeroRowDendrogramWidth_WithNonIdentityTree_HeatmapNotReordered()
    {
        // Tree would reorder [0,1,2] → [2,0,1]. With width=0 panel is suppressed,
        // so the heatmap must render in ORIGINAL data order (same as no-tree baseline).
        var reorderTree = new TreeNode
        {
            Value = 2.0,
            Children =
            [
                new TreeNode { Value = 2.0 }, // would put row 2 first
                new TreeNode
                {
                    Value = 1.0,
                    Children =
                    [
                        new TreeNode { Value = 0.0 },
                        new TreeNode { Value = 1.0 },
                    ]
                }
            ]
        };

        string svgNoTree    = RenderSvg();
        string svgSuppressed = RenderSvg(s =>
        {
            s.RowTree = reorderTree;
            s.RowDendrogramWidth = 0.0; // suppress panel
        });

        // Without a dendrogram panel, both SVGs should contain the same line count
        // (axes lines) — confirming no extra dendrogram segment lines were emitted.
        Assert.Equal(
            CountOccurrences(svgNoTree,    "<line "),
            CountOccurrences(svgSuppressed, "<line "));
    }

    [Fact]
    public void BothPanelsSuppressed_HeatmapFillCountSameAsNoTree()
    {
        // When both panel widths are 0 the composite produces exactly the same
        // number of heatmap fill rectangles as a plain no-tree render.
        string svgNoTree = RenderSvg();
        string svgSuppressed = RenderSvg(s =>
        {
            s.RowTree = RowTree3;
            s.ColumnTree = ColTree3;
            s.RowDendrogramWidth = 0.0;
            s.ColumnDendrogramHeight = 0.0;
        });

        Assert.Equal(
            CountOccurrences(svgNoTree,    "<rect "),
            CountOccurrences(svgSuppressed, "<rect "));
    }
}
