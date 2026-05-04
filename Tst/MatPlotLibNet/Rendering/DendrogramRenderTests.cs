// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
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

    private static int CountDistinctStrokeColors(string svg)
    {
        var matches = Regex.Matches(svg, @"<line[^>]*stroke=""(#[0-9a-fA-F]{6,8})""");
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in matches) set.Add(m.Groups[1].Value);
        return set.Count;
    }

    // ── Smoke tests for all four orientations ───────────────────────────────

    [Fact]
    public void ThreeLeaf_Top_RendersValidSvg()
    {
        string svg = RenderSvg(ThreeLeafTree, s => s.Orientation = DendrogramOrientation.Top);
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void ThreeLeaf_Top_EmitsLineSegments()
    {
        // 3 leaves → 2 internal merges → 6 line segments (3 per U-shape).
        string svg = RenderSvg(ThreeLeafTree);
        Assert.True(CountOccurrences(svg, "<line") >= 6,
            $"Expected at least 6 <line> segments for a 3-leaf dendrogram, found {CountOccurrences(svg, "<line")}.");
    }

    [Fact]
    public void ThreeLeaf_Bottom_DiffersFromTop()
    {
        string top    = RenderSvg(ThreeLeafTree, s => s.Orientation = DendrogramOrientation.Top);
        string bottom = RenderSvg(ThreeLeafTree, s => s.Orientation = DendrogramOrientation.Bottom);
        Assert.NotEqual(top, bottom);
    }

    [Fact]
    public void ThreeLeaf_Left_RendersValidSvg()
    {
        string svg = RenderSvg(ThreeLeafTree, s => s.Orientation = DendrogramOrientation.Left);
        Assert.Contains("<svg", svg);
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void ThreeLeaf_Right_RendersValidSvg()
    {
        string svg = RenderSvg(ThreeLeafTree, s => s.Orientation = DendrogramOrientation.Right);
        Assert.Contains("<svg", svg);
        Assert.Contains("<line", svg);
    }

    // ── Labels ──────────────────────────────────────────────────────────────

    [Fact]
    public void ThreeLeaf_Top_ShowLabelsTrue_EmitsLeafLabels()
    {
        string svg = RenderSvg(ThreeLeafTree, s => s.ShowLabels = true);
        Assert.Contains(">A<", svg);
        Assert.Contains(">B<", svg);
        Assert.Contains(">C<", svg);
    }

    [Fact]
    public void ThreeLeaf_Top_ShowLabelsFalse_NoLeafLabels()
    {
        string svg = RenderSvg(ThreeLeafTree, s => s.ShowLabels = false);
        Assert.DoesNotContain(">A<", svg);
        Assert.DoesNotContain(">B<", svg);
        Assert.DoesNotContain(">C<", svg);
    }

    [Fact]
    public void ThreeLeaf_Left_RotatesLeafLabels()
    {
        // Left/Right orientations rotate labels so they remain readable along the leaf axis.
        string svg = RenderSvg(ThreeLeafTree, s =>
        {
            s.Orientation = DendrogramOrientation.Left;
            s.ShowLabels = true;
        });
        Assert.Contains("transform=\"rotate(", svg);
    }

    // ── Cut height ──────────────────────────────────────────────────────────

    [Fact]
    public void ThreeLeaf_CutHeight_AddsExtraLine()
    {
        string none   = RenderSvg(ThreeLeafTree);
        string cut    = RenderSvg(ThreeLeafTree, s => s.CutHeight = 1.5);
        Assert.True(CountOccurrences(cut, "<line") > CountOccurrences(none, "<line"),
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
        var nonCutSegments = Regex.Matches(svg, @"<line[^>]*stroke=""(#[0-9a-fA-F]{6,8})""");
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in nonCutSegments)
        {
            string c = m.Groups[1].Value;
            if (!c.Equals(Colors.Magenta.ToHex(), StringComparison.OrdinalIgnoreCase))
                set.Add(c);
        }
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

        var matches = Regex.Matches(svg, @"<text[^>]*x=""([0-9.]+)""[^>]*>Only</text>");
        Assert.Single(matches);
        double labelX = double.Parse(matches[0].Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.InRange(labelX, 200.0, 400.0);   // central third of a 600-wide figure
    }

    [Fact]
    public void EmptyChildren_RendersWithoutError()
    {
        var tree = new TreeNode { Label = "Empty" };
        string svg = RenderSvg(tree);
        Assert.Contains("<svg", svg);
    }

    // ── Coverage gaps surfaced during code review ────────────────────────────

    [Fact]
    public void AllZeroMergeValues_RendersWithoutNaN()
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
        string svg = RenderSvg(tree);
        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("NaN", svg);
    }

    [Fact]
    public void CutAboveRoot_AllSegmentsShareSingletonClusterColor()
    {
        // Cut height greater than every node's Value collapses to a single cluster root
        // (the series root itself). AssignClusterColors' singletonT=0.0 path samples
        // the colormap at index 0, yielding Tab10's first colour (#1F77B4).
        string svg = RenderSvg(SevenLeafTree, s =>
        {
            s.CutHeight = 999;
            s.ColorByCluster = true;
            s.ColorMap = QualitativeColorMaps.Tab10;
        });
        Assert.Contains("#1F77B4", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PureLeafTree_LeafIsClusterRootViaChildrenCountArm()
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
        string svg = RenderSvg(tree, s =>
        {
            s.CutHeight = 3;
            s.ColorByCluster = true;
            s.ColorMap = QualitativeColorMaps.Tab10;
        });
        Assert.Contains("#1F77B4", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#17BECF", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bottom_ShowLabelsTrue_EmitsLeafLabels()
    {
        // The Bottom case of DrawLabels' switch was previously unhit; ensure leaf labels
        // appear when ShowLabels is enabled in this orientation.
        string svg = RenderSvg(ThreeLeafTree, s =>
        {
            s.Orientation = DendrogramOrientation.Bottom;
            s.ShowLabels = true;
        });
        Assert.Contains(">A<", svg);
        Assert.Contains(">B<", svg);
        Assert.Contains(">C<", svg);
    }
}
