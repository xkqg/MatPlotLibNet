// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>v1.10 — verifies SVG output of <see cref="DendrogramSeries"/> rendering across
/// the four orientations, cut-line emission, and cluster colouring.</summary>
public class DendrogramRenderTests
{
    /// <summary>
    /// Three-leaf merge tree:
    /// <code>
    ///       root (Value = 2.0)
    ///       /  \
    ///   merge   C (leaf, Value = 0)
    ///   (1.0)
    ///    / \
    ///   A   B   (both leaves, Value = 0)
    /// </code>
    /// </summary>
    private static TreeNode ThreeLeafTree => new()
    {
        Label = "root",
        Value = 2.0,
        Children =
        [
            new TreeNode
            {
                Label = "AB",
                Value = 1.0,
                Children =
                [
                    new TreeNode { Label = "A", Value = 0 },
                    new TreeNode { Label = "B", Value = 0 },
                ],
            },
            new TreeNode { Label = "C", Value = 0 },
        ],
    };

    /// <summary>Seven-leaf tree, cut at 1.5 produces 3 clusters: {A,B}, {C,D,E}, {F,G}.</summary>
    private static TreeNode SevenLeafTree => new()
    {
        Label = "root",
        Value = 3.0,
        Children =
        [
            new TreeNode
            {
                Label = "left",
                Value = 2.0,
                Children =
                [
                    new TreeNode
                    {
                        Label = "AB",
                        Value = 1.0,
                        Children = [new TreeNode { Label = "A" }, new TreeNode { Label = "B" }],
                    },
                    new TreeNode
                    {
                        Label = "CDE",
                        Value = 1.2,
                        Children =
                        [
                            new TreeNode { Label = "C" },
                            new TreeNode
                            {
                                Label = "DE",
                                Value = 0.5,
                                Children = [new TreeNode { Label = "D" }, new TreeNode { Label = "E" }],
                            },
                        ],
                    },
                ],
            },
            new TreeNode
            {
                Label = "FG",
                Value = 1.0,
                Children = [new TreeNode { Label = "F" }, new TreeNode { Label = "G" }],
            },
        ],
    };

    private static string RenderSvg(TreeNode root, Action<DendrogramSeries>? configure = null) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Dendrogram(root, configure))
            .ToSvg();
    private static int CountDistinctStrokeColors(string svg)
    {
        return svg.Elements("line").Select(line => line["stroke"]).OfType<string>().Where(stroke => stroke.StartsWith('#')).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    }

    // ── Smoke tests for all four orientations ───────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ThreeLeaf_Left_RendersValidSvg", () => RenderSvg(ThreeLeafTree, s => s.Orientation = DendrogramOrientation.Left), "<svg", "<line"),
        new DrawnToken("ThreeLeaf_Right_RendersValidSvg", () => RenderSvg(ThreeLeafTree, s => s.Orientation = DendrogramOrientation.Right), "<svg", "<line"),
        new DrawnToken("ThreeLeaf_Top_ShowLabelsTrue_EmitsLeafLabels", () => RenderSvg(ThreeLeafTree, s => s.ShowLabels = true), ">A<", ">B<", ">C<"),
        new DrawnToken("ThreeLeaf_Top_ShowLabelsFalse_NoLeafLabels", () => RenderSvg(ThreeLeafTree, s => s.ShowLabels = false)) { Absent = [">A<", ">B<", ">C<"] },
        // Left/Right orientations rotate labels so they remain readable along the leaf axis.
        new DrawnToken("ThreeLeaf_Left_RotatesLeafLabels", () => RenderSvg(ThreeLeafTree, s =>
        {
            s.Orientation = DendrogramOrientation.Left;
            s.ShowLabels = true;
        }), "transform=\"rotate("),
        // Cut height greater than every node's Value collapses to a single cluster root
        // (the series root itself). AssignClusterColors' singletonT=0.0 path samples
        // the colormap at index 0, yielding Tab10's first colour (#1F77B4).
        new DrawnToken("CutAboveRoot_AllSegmentsShareSingletonClusterColor", () => RenderSvg(SevenLeafTree, s =>
        {
            s.CutHeight = 999;
            s.ColorByCluster = true;
            s.ColorMap = QualitativeColorMaps.Tab10;
        }), "#1F77B4") { Comparison = StringComparison.OrdinalIgnoreCase },
        // The Bottom case of DrawLabels' switch was previously unhit; ensure leaf labels
        // appear when ShowLabels is enabled in this orientation.
        new DrawnToken("Bottom_ShowLabelsTrue_EmitsLeafLabels", () => RenderSvg(ThreeLeafTree, s =>
        {
            s.Orientation = DendrogramOrientation.Bottom;
            s.ShowLabels = true;
        }), ">A<", ">B<", ">C<"),
        new DrawnToken("AllZeroMergeValues_RendersWithoutNaN", () =>
        {
            // Multi-node tree where every merge distance is zero. Exercises ComputeMaxMerge's
            // <1e-10 → 1.0 fallback at the internal-node path (SingleLeaf only hits it via
            // an empty layout). Without the guard, MergePixel would divide by zero and emit
            // NaN coordinates into the SVG.
            var tree = new TreeNode
            {
                Label = "root",
                Value = 0,
                Children =
                [
                    new TreeNode { Label = "A", Value = 0 },
                    new TreeNode { Label = "B", Value = 0 },
                ],
            };
            return RenderSvg(tree);
        }, "<svg") { Finite = true },
        new DrawnToken("PureLeafTree_LeafIsClusterRootViaChildrenCountArm", () =>
        {
            // Tree where every leaf's Value is greater than the cut. The Value<cut arm of
            // CollectClusterRoots fails for leaves; only the Children.Count==0 short-circuit
            // adds them as cluster roots. With two leaves we get exactly two cluster roots
            // → Tab10 samples at t=0 and t=1 (#1F77B4 and #17BECF respectively).
            var tree = new TreeNode
            {
                Label = "root",
                Value = 10,
                Children =
                [
                    new TreeNode { Label = "A", Value = 5 },
                    new TreeNode { Label = "B", Value = 5 },
                ],
            };
            return RenderSvg(tree, s =>
            {
                s.CutHeight = 3;
                s.ColorByCluster = true;
                s.ColorMap = QualitativeColorMaps.Tab10;
            });
        }, "#1F77B4", "#17BECF") { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void ThreeLeaf_Top_EmitsLineSegments()
    {
        // 3 leaves → 2 internal merges → 6 line segments (3 per U-shape).
        string svg = RenderSvg(ThreeLeafTree);
        Assert.True(svg.CountOf("<line") >= 6,
            $"Expected at least 6 <line> segments for a 3-leaf dendrogram, found {svg.CountOf("<line")}.");
    }

    [Fact]
    public void ThreeLeaf_Bottom_DiffersFromTop()
    {
        string top    = RenderSvg(ThreeLeafTree, s => s.Orientation = DendrogramOrientation.Top);
        string bottom = RenderSvg(ThreeLeafTree, s => s.Orientation = DendrogramOrientation.Bottom);
        Assert.NotEqual(top, bottom);
    }

    // ── Cut height ──────────────────────────────────────────────────────────

    [Fact]
    public void ThreeLeaf_CutHeight_AddsExtraLine()
    {
        string none   = RenderSvg(ThreeLeafTree);
        string cut    = RenderSvg(ThreeLeafTree, s => s.CutHeight = 1.5);
        Assert.True(cut.CountOf("<line") > none.CountOf("<line"),
            "Expected cut-line emission to add at least one extra <line> segment.");
    }

    // ── Cluster colouring ───────────────────────────────────────────────────

    [Fact]
    public void SevenLeaf_ColorByCluster_True_EmitsMultipleColors()
    {
        string svg = RenderSvg(SevenLeafTree, s =>
        {
            s.CutHeight = 1.5;
            s.ColorByCluster = true;
            s.ColorMap = QualitativeColorMaps.Tab10;
        });
        Assert.True(CountDistinctStrokeColors(svg) >= 2,
            $"Expected at least 2 distinct stroke colors with ColorByCluster=true, found {CountDistinctStrokeColors(svg)}.");
    }

    [Fact]
    public void SevenLeaf_ColorByCluster_False_SingleSegmentColor()
    {
        // With ColorByCluster=false the dendrogram segments share one stroke colour.
        // The cut line uses CutLineColor (or its default) which may differ — we exclude
        // it by examining the segment colours only via a known explicit cut-line colour.
        string svg = RenderSvg(SevenLeafTree, s =>
        {
            s.CutHeight = 1.5;
            s.ColorByCluster = false;
            s.CutLineColor = Colors.Magenta;
        });
        // Strip the cut-line stroke (Magenta) and count remaining distinct stroke colours.
        var set = svg.Elements("line").Select(line => line["stroke"]).OfType<string>()
            .Where(stroke => stroke.StartsWith('#') && !stroke.Equals(Colors.Magenta.ToHex(), StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Single(set);
    }

    // ── Degenerate inputs ───────────────────────────────────────────────────

    [Fact]
    public void SingleLeaf_LabelCenteredHorizontallyInPlot()
    {
        // Single-leaf tree: LeafPixel's leafCount<=1 branch returns t=0.5, placing the
        // sole label at the X-midpoint of the plot area (≈ figure-width / 2 after margins).
        var tree = new TreeNode { Label = "Only" };
        string svg = Plt.Create()
            .WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Dendrogram(tree))
            .ToSvg();

        var only = Assert.Single(svg.Elements("text"), text => text.Text == "Only");
        double labelX = only.Number("x");
        Assert.InRange(labelX, 200.0, 400.0);   // central third of a 600-wide figure
    }

}
