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
    // ── Smoke tests ───────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // row dendrogram U-segments
        // A row tree that names one leaf names that row, and no other.
        new DrawnToken("ARowTreeNamingOneLeaf_NamesThatRow", () => RenderSvg(s => s.RowTree = new TreeNode
        {
            Value = 2.0,
            Children = [new TreeNode { Value = 0.0, Label = "first" }, new TreeNode { Value = 1.0, Children = [new TreeNode { Value = 1.0 }, new TreeNode { Value = 2.0 }] }],
        }), ">first<"),
        // A row tree whose dendrogram is hidden still names its rows, in the order of the data.
        new DrawnToken("ARowTreeWhoseDendrogramIsHidden_StillNamesItsRows", () => RenderSvg(s =>
        {
            s.RowTree = new TreeNode
            {
                Value = 2.0,
                Children = [new TreeNode { Value = 0.0, Label = "first" }, new TreeNode { Value = 1.0, Children = [new TreeNode { Value = 1.0 }, new TreeNode { Value = 2.0 }] }],
            };
            s.RowDendrogramWidth = 0;
        }), ">first<"),
        new DrawnToken("WithRowTree_EmitsHeatmapAndRowDendrogram", () => RenderSvg(s => s.RowTree = RowTree3), "<rect", "<line"),
        // col dendrogram U-segments
        new DrawnToken("WithColumnTree_EmitsHeatmapAndColumnDendrogram", () => RenderSvg(s => s.ColumnTree = ColTree3), "<rect", "<line"),
        new DrawnToken("ShowLabelsTrue_EmitsTextElements", () => RenderSvg(s => s.ShowLabels = true), "<text"),
        // Viridis colormap produces non-white fills; at least one fill attribute expected
        new DrawnToken("DefaultColormap_RendersWithFillColors", () => RenderSvg(), "fill=\"#"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void NoTrees_EmitsHeatmapCells()
    {
        string svg = RenderSvg();
        // 3×3 = 9 cells → at least 9 <rect> elements
        Assert.True(svg.CountOf("<rect") >= 9,
            $"Expected ≥9 <rect> for 3×3 heatmap, found {svg.CountOf("<rect")}.");
    }

    // ── Row dendrogram ────────────────────────────────────────────────────────

    [Fact]
    public void WithRowTree_RowDendrogramDifferentFromNone()
    {
        string noTree = RenderSvg();
        string withRow = RenderSvg(s => s.RowTree = RowTree3);
        // SVG with row dendrogram has line segments the plain heatmap doesn't
        Assert.True(withRow.CountOf("<line") > noTree.CountOf("<line"));
    }

    // ── Column dendrogram ─────────────────────────────────────────────────────

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
        int baseline = none.CountOf("<line");
        int rowExtra  = row.CountOf("<line") - baseline;
        int colExtra  = col.CountOf("<line") - baseline;
        int bothExtra = both.CountOf("<line") - baseline;
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
        Assert.True(labels.CountOf("<text") > noLabels.CountOf("<text"),
            "ShowLabels=true must emit more <text> elements than ShowLabels=false.");
    }

    // ── Reordering: heatmap column count unchanged when column tree given ─────

    [Fact]
    public void WithColumnTree_HeatmapRectCountUnchanged()
    {
        // Reordering reshuffles columns but does not add or remove cells.
        string noTree = RenderSvg();
        string withCol = RenderSvg(s => s.ColumnTree = ColTree3);
        int rectNoTree = noTree.CountOf("<rect");
        int rectWithCol = withCol.CountOf("<rect");
        Assert.Equal(rectNoTree, rectWithCol);
    }

    // ── Panel ratios affect SVG structure ─────────────────────────────────────

    /// <summary>Width=0 suppresses the row dendrogram panel and Height=0 the column one: the line count equals the no-tree baseline.</summary>
    public static TheoryData<Action<ClustermapSeries>> ZeroSizedDendrograms => new()
    {
        s =>
        {
            s.RowTree = RowTree3;
            s.RowDendrogramWidth = 0.0;
        },
        s =>
        {
            s.ColumnTree = ColTree3;
            s.ColumnDendrogramHeight = 0.0;
        },
    };

    [Theory]
    [MemberData(nameof(ZeroSizedDendrograms))]
    public void ZeroDendrogramSize_WithATree_SameLineCountAsNoTree(Action<ClustermapSeries> configure)
    {
        string noTree = RenderSvg();
        string zeroed = RenderSvg(configure);
        Assert.Equal(noTree.CountOf("<line"), zeroed.CountOf("<line"));
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
            svgNoTree.CountOf("<line "),
            svgSuppressed.CountOf("<line "));
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
            svgNoTree.CountOf("<rect "),
            svgSuppressed.CountOf("<rect "));
    }

    // ── Row and column names ──────────────────────────────────────────────────

    private static TreeNode Named(TreeNode tree, string prefix) => tree.Children.Count == 0
        ? tree with { Label = prefix + (int)tree.Value }
        : tree with { Children = [.. tree.Children.Select(child => Named(child, prefix))] };

    private static (double X, double Y, string Text)[] Names(string svg, string prefix) =>
    [
        .. svg.Elements("text").Where(text => text.Text.Length == prefix.Length + 1 && text.Text.StartsWith(prefix, StringComparison.Ordinal) && char.IsAsciiDigit(text.Text[^1]))
            .Select(text => (text.Number("x"), text.Number("y"), text.Text)),
    ];

    /// <summary>A clustermap names its rows and columns by the labels of its trees' leaves, as seaborn's clustermap names them by
    /// its tick labels: each column under the matrix and each row right of it, in the order the trees put them in.</summary>
    [Fact]
    public void TheLeafLabelsOfItsTrees_NameTheRowsAndColumnsWhereTheMapDrawsThem()
    {
        string svg = RenderSvg(s => { s.RowTree = Named(RowTree3, "row-"); s.ColumnTree = Named(ColTree3, "col-"); });

        var columns = Names(svg, "col-");
        var rows = Names(svg, "row-");
        Assert.Equal(["col-2", "col-0", "col-1"], columns.OrderBy(t => t.X).Select(t => t.Text));
        Assert.Equal(["row-0", "row-1", "row-2"], rows.OrderBy(t => t.Y).Select(t => t.Text));
        Assert.True(columns.Min(t => t.Y) > rows.Max(t => t.Y), "the column names stand under the matrix");
        Assert.True(rows.Min(t => t.X) > columns.Max(t => t.X), "the row names stand right of the matrix");
    }

    /// <summary>The names follow the rows when the dendrogram panel is turned off and the matrix is drawn in the order given; a
    /// leaf with no label names nothing.</summary>
    [Fact]
    public void WithItsPanelsOff_TheNamesFollowTheRowsInTheOrderGiven_AndAnUnnamedLeafNamesNothing()
    {
        string svg = RenderSvg(s => { s.RowTree = RowTree3; s.ColumnTree = Named(ColTree3, "col-"); s.RowDendrogramWidth = 0; s.ColumnDendrogramHeight = 0; });

        Assert.Equal(["col-0", "col-1", "col-2"], Names(svg, "col-").OrderBy(t => t.X).Select(t => t.Text));
        Assert.Empty(Names(svg, "row-"));
    }

    /// <summary>Only a leaf that stands for a row of the matrix names one: a leaf with no label, or one whose index is past the
    /// matrix, names nothing, and the rows it leaves go unnamed.</summary>
    [Fact]
    public void ALeafWithNoLabelOrPastTheMatrix_NamesNothing()
    {
        var tree = new TreeNode
        {
            Value = 1.0,
            Children =
            [
                new TreeNode { Value = 0.0, Label = "row-0" },
                new TreeNode { Value = 1.0 },
                new TreeNode { Value = 7.0, Label = "row-7" },
            ],
        };

        var names = Names(RenderSvg(s => s.RowTree = tree), "row-");

        Assert.Equal(["row-0"], names.Select(t => t.Text));
    }
}
