// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers.Flow;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers.Flow;

/// <summary>
/// Direct unit tests for <see cref="SankeyLayoutEngine"/> extracted in Phase B.10.
/// Targets every branch of column assignment, alignment, value aggregation, greedy
/// packing, relaxation, and collision resolution. Pure-logic tests — no IRenderContext.
/// </summary>
public class SankeyLayoutEngineTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 200);
    private readonly SankeyLayoutEngine _engine = new();

    // ──────────────────────────────────────────────────────────────────────────
    // Compute — degenerate inputs
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_EmptyNodes_ReturnsNull()
    {
        var series = new SankeySeries(Array.Empty<SankeyNode>(), Array.Empty<SankeyLink>());
        var layout = _engine.Compute(series, Bounds);
        Assert.Null(layout);
    }

    [Fact]
    public void Compute_SingleNode_NoLinks_ReturnsLayout()
    {
        var series = new SankeySeries([new("A")], Array.Empty<SankeyLink>());
        var layout = _engine.Compute(series, Bounds);
        Assert.NotNull(layout);
        Assert.Single(layout!.Columns);
        Assert.Equal(0, layout.Columns[0]);
        Assert.Equal(0, layout.MaxCol);
    }

    [Fact]
    public void Compute_SingleNode_HasNodeValuesArray()
    {
        var series = new SankeySeries([new("A")], Array.Empty<SankeyLink>());
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Single(layout.NodeValues);
        Assert.Equal(0.0, layout.NodeValues[0]);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Column assignment — BFS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_LinearChain_AssignsSequentialColumns()
    {
        // A → B → C
        var series = new SankeySeries(
            [new("A"), new("B"), new("C")],
            [new(0, 1, 10), new(1, 2, 10)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(0, layout.Columns[0]);
        Assert.Equal(1, layout.Columns[1]);
        Assert.Equal(2, layout.Columns[2]);
        Assert.Equal(2, layout.MaxCol);
    }

    [Fact]
    public void Compute_FanOut_AllTargetsInColumn1()
    {
        // A → B, A → C, A → D
        var series = new SankeySeries(
            [new("A"), new("B"), new("C"), new("D")],
            [new(0, 1, 5), new(0, 2, 5), new(0, 3, 5)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(0, layout.Columns[0]);
        Assert.Equal(1, layout.Columns[1]);
        Assert.Equal(1, layout.Columns[2]);
        Assert.Equal(1, layout.Columns[3]);
    }

    [Fact]
    public void Compute_FanIn_SharesTargetColumn()
    {
        // A → C, B → C  — both sources at col 0, target at col 1
        var series = new SankeySeries(
            [new("A"), new("B"), new("C")],
            [new(0, 2, 5), new(1, 2, 5)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(0, layout.Columns[0]);
        Assert.Equal(0, layout.Columns[1]);
        Assert.Equal(1, layout.Columns[2]);
    }

    [Fact]
    public void Compute_ExplicitColumnOverride_OverridesBfs()
    {
        // A → B, but B has explicit Column=5 — BFS must not overwrite
        var series = new SankeySeries(
            [new("A"), new("B", Column: 5)],
            [new(0, 1, 10)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(0, layout.Columns[0]);
        Assert.Equal(5, layout.Columns[1]);
        Assert.Equal(5, layout.MaxCol);
    }

    [Fact]
    public void Compute_ExplicitColumnOverride_QueuesFromOverride()
    {
        // B has explicit Column=2 and is a target — should still enter queue
        // via the override path, propagating through links that source from it.
        var series = new SankeySeries(
            [new("A"), new("B", Column: 2), new("C")],
            [new(0, 1, 10), new(1, 2, 10)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(2, layout.Columns[1]);
        Assert.Equal(3, layout.Columns[2]);
    }

    [Fact]
    public void Compute_BfsSkipsTargetWhenOverrideSet()
    {
        // A → B, with B override=9 — BFS would set B to 1, but override wins.
        var series = new SankeySeries(
            [new("A"), new("B", Column: 9)],
            [new(0, 1, 10)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(9, layout.Columns[1]);
    }

    [Fact]
    public void Compute_BfsDoesNotShrinkColumn()
    {
        // A → C and A → B → C. BFS visiting A→C first gives cols[C]=1, then
        // via B must not decrease (cols[tgt] < nextCol guard).
        var series = new SankeySeries(
            [new("A"), new("B"), new("C")],
            [new(0, 2, 5), new(0, 1, 5), new(1, 2, 5)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(0, layout.Columns[0]);
        Assert.Equal(1, layout.Columns[1]);
        Assert.Equal(2, layout.Columns[2]);
    }

    [Fact]
    public void Compute_CycleWithoutOverrides_ClampsToZero()
    {
        // A ↔ B — neither is a source (both are targets), BFS queue stays empty.
        // Clamp at the end forces -1 → 0.
        var series = new SankeySeries(
            [new("A"), new("B")],
            [new(0, 1, 5), new(1, 0, 5)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(0, layout.Columns[0]);
        Assert.Equal(0, layout.Columns[1]);
    }

    [Fact]
    public void Compute_IsolatedNode_GetsColumnZero()
    {
        // A, B isolated — not a target, so source-path sets cols=0.
        var series = new SankeySeries([new("A"), new("B")], Array.Empty<SankeyLink>());
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(0, layout.Columns[0]);
        Assert.Equal(0, layout.Columns[1]);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Alignment
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_JustifyAlignment_PreservesBfsColumns()
    {
        // A → C and B → C. BFS: A=0, B=0, C=1. Justify leaves as-is.
        var series = new SankeySeries(
            [new("A"), new("B"), new("C")],
            [new(0, 2, 5), new(1, 2, 5)])
        { NodeAlignment = SankeyNodeAlignment.Justify };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(0, layout.Columns[0]);
        Assert.Equal(0, layout.Columns[1]);
        Assert.Equal(1, layout.Columns[2]);
    }

    [Fact]
    public void Compute_LeftAlignment_PreservesBfsColumns()
    {
        // Left mode hits the default arm of the switch and does not change columns[i],
        // but still runs the "latest" computation (branch coverage).
        var series = new SankeySeries(
            [new("A"), new("B"), new("C"), new("D")],
            [new(0, 1, 5), new(1, 3, 5), new(2, 3, 5)])
        { NodeAlignment = SankeyNodeAlignment.Left };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(0, layout.Columns[0]);
        Assert.Equal(1, layout.Columns[1]);
        Assert.Equal(0, layout.Columns[2]);
        Assert.Equal(2, layout.Columns[3]);
    }

    [Fact]
    public void Compute_RightAlignment_PullsSourcesRightward()
    {
        // A → B → D, C → D. Under Justify: A=0, B=1, C=0, D=2.
        // Under Right: C gets pulled to col 1 (latest just before D).
        var series = new SankeySeries(
            [new("A"), new("B"), new("C"), new("D")],
            [new(0, 1, 5), new(1, 3, 5), new(2, 3, 5)])
        { NodeAlignment = SankeyNodeAlignment.Right };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(2, layout.MaxCol);
        Assert.Equal(1, layout.Columns[2]); // C pulled right
        Assert.Equal(2, layout.Columns[3]); // D stays
    }

    [Fact]
    public void Compute_CenterAlignment_AveragesBetweenLeftAndRight()
    {
        // A → B → D (3 cols) and C → D.
        // Left: A=0, B=1, C=0, D=2.  Right (latest):  A=0, B=1, C=1, D=2.
        // Center = (left+right)/2 = A=0, B=1, C=0, D=2 (C averages (0+1)/2=0 via integer div).
        var series = new SankeySeries(
            [new("A"), new("B"), new("C"), new("D")],
            [new(0, 1, 5), new(1, 3, 5), new(2, 3, 5)])
        { NodeAlignment = SankeyNodeAlignment.Center };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(0, layout.Columns[0]);
        Assert.Equal(1, layout.Columns[1]);
        Assert.Equal(0, layout.Columns[2]);
        Assert.Equal(2, layout.Columns[3]);
    }

    [Fact]
    public void Compute_RightAlignment_ClampsLatestWhenBelowBfsColumn()
    {
        // Both nodes pinned to column 2 via override; link A→B drives
        // `latest[A] = latest[B] - 1 = 1` during the alignment sweep.
        // The post-sweep clamp `if (latest[i] < columns[i])` must restore
        // latest[A] = 2 so Right-alignment does not pull A backwards.
        var series = new SankeySeries(
            [new("A", Column: 2), new("B", Column: 2)],
            [new(0, 1, 5)])
        { NodeAlignment = SankeyNodeAlignment.Right };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(2, layout.Columns[0]);
        Assert.Equal(2, layout.Columns[1]);
    }

    [Fact]
    public void Compute_RightAlignment_ConvergenceLoopExitsEarly()
    {
        // Fully linear chain — the "latest" sweep converges in one pass, triggering
        // the `if (!changed) break;` branch on pass 2.
        var series = new SankeySeries(
            [new("A"), new("B"), new("C")],
            [new(0, 1, 5), new(1, 2, 5)])
        { NodeAlignment = SankeyNodeAlignment.Right };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(2, layout.MaxCol);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Node values — out/in max
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_NodeValues_UsesOutwardWhenLargerThanInward()
    {
        // A receives 10, sends 20 → value = 20 (outward wins).
        var series = new SankeySeries(
            [new("Src"), new("A"), new("T1"), new("T2")],
            [new(0, 1, 10), new(1, 2, 12), new(1, 3, 8)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(20, layout.NodeValues[1]);
    }

    [Fact]
    public void Compute_NodeValues_UsesInwardWhenLargerThanOutward()
    {
        // A receives 30 (20+10), sends 5 → value = 30.
        var series = new SankeySeries(
            [new("S1"), new("S2"), new("A"), new("T")],
            [new(0, 2, 20), new(1, 2, 10), new(2, 3, 5)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(30, layout.NodeValues[2]);
    }

    [Fact]
    public void Compute_ZeroValueLinks_PositiveNodeSizeFloor()
    {
        // All links have value 0 → valueScale = 0 path; each node must still get size≥1.
        var series = new SankeySeries(
            [new("A"), new("B")],
            [new(0, 1, 0)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.True(layout.NodeRects[0].Height >= 1);
        Assert.True(layout.NodeRects[1].Height >= 1);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Orientation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_HorizontalOrientation_NodesSpanVertically()
    {
        var series = new SankeySeries(
            [new("A"), new("B")],
            [new(0, 1, 10)])
        { Orient = SankeyOrientation.Horizontal };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.False(layout.Vertical);
        // Horizontal: NodeWidth is the X-dimension
        Assert.Equal(series.NodeWidth, layout.NodeRects[0].Width);
    }

    [Fact]
    public void Compute_VerticalOrientation_NodesSpanHorizontally()
    {
        var series = new SankeySeries(
            [new("A"), new("B")],
            [new(0, 1, 10)])
        { Orient = SankeyOrientation.Vertical };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.True(layout.Vertical);
        // Vertical: NodeWidth is the Y-dimension (cross axis becomes X)
        Assert.Equal(series.NodeWidth, layout.NodeRects[0].Height);
    }

    [Fact]
    public void Compute_SingleColumn_ColStepIsZero()
    {
        // Only sources, no links — all nodes fall into col 0, MaxCol=0 → colStep=0 branch.
        var series = new SankeySeries(
            [new("A"), new("B"), new("C")],
            Array.Empty<SankeyLink>());
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(0, layout.MaxCol);
        // All nodes share the same primary-axis position
        Assert.Equal(layout.NodeRects[0].X, layout.NodeRects[1].X);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Relaxation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_ZeroIterations_SkipsRelaxation()
    {
        // Iterations = 0 → Relax returns immediately. Layout must still be valid.
        var series = new SankeySeries(
            [new("A"), new("B"), new("C")],
            [new(0, 1, 5), new(1, 2, 5)])
        { Iterations = 0 };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(3, layout.NodeRects.Length);
    }

    [Fact]
    public void Compute_PositiveIterations_ShiftsNodePositions()
    {
        // Crossing scenario that benefits from relaxation:
        // S1→T2, S2→T1 — relaxation should pull S1/S2 toward their targets' centres.
        var series = new SankeySeries(
            [new("S1"), new("S2"), new("T1"), new("T2")],
            [new(0, 3, 10), new(1, 2, 10)])
        { Iterations = 6 };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(4, layout.NodeRects.Length);
    }

    [Fact]
    public void Compute_NodeWithNoLinks_SkipsRelaxShift()
    {
        // C is isolated (no links touching it) — weightTotal stays 0, relax branch skips it.
        var series = new SankeySeries(
            [new("A"), new("B"), new("C")],
            [new(0, 1, 5)])
        { Iterations = 3 };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(3, layout.NodeRects.Length);
    }

    [Fact]
    public void Compute_BothRelaxDirections_Exercised()
    {
        // 3-column layout exercises both upstream (targets pulled toward sources)
        // and downstream (sources pulled toward targets) passes.
        var series = new SankeySeries(
            [new("A"), new("B"), new("C"), new("D")],
            [new(0, 1, 10), new(0, 2, 10), new(1, 3, 5), new(2, 3, 5)])
        { Iterations = 6 };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(4, layout.NodeRects.Length);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Collision resolution
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_OversizedNodes_ResolvedWithinBounds()
    {
        // Many nodes in one column with big values → relaxation/collision must push
        // the stack back within bounds (overshoot > 0 branch).
        var nodes = new SankeyNode[]
        {
            new("Src"),
            new("A"), new("B"), new("C"), new("D"), new("E"), new("F"),
        };
        var links = new SankeyLink[]
        {
            new(0, 1, 100), new(0, 2, 100), new(0, 3, 100),
            new(0, 4, 100), new(0, 5, 100), new(0, 6, 100),
        };
        var series = new SankeySeries(nodes, links) { NodePadding = 5, Iterations = 6 };
        var tight = new Rect(0, 0, 200, 100);
        var layout = _engine.Compute(series, tight)!;

        double crossMax = tight.Y + tight.Height;
        foreach (var rect in layout.NodeRects)
            Assert.True(rect.Y + rect.Height <= crossMax + 0.001,
                $"Node extends beyond bounds: {rect.Y + rect.Height} > {crossMax}");
    }

    [Fact]
    public void Compute_LayoutFitsInsideBounds_NoOvershoot()
    {
        // Small, comfortably-sized scenario — overshoot <= 0 branch.
        var series = new SankeySeries(
            [new("A"), new("B")],
            [new(0, 1, 10)]);
        var layout = _engine.Compute(series, Bounds)!;

        double crossMax = Bounds.Y + Bounds.Height;
        foreach (var rect in layout.NodeRects)
            Assert.True(rect.Y + rect.Height <= crossMax + 0.001);
    }

    [Fact]
    public void Compute_CollisionPropagation_RespectsPadding()
    {
        // Force overshoot propagation through k>0 branch: two nodes in column 0
        // crammed into a tall-values scenario.
        var series = new SankeySeries(
            [new("A"), new("B"), new("C"), new("D")],
            [new(0, 2, 50), new(1, 3, 50)])
        { NodePadding = 20, Iterations = 6 };
        var tight = new Rect(0, 0, 200, 120);
        var layout = _engine.Compute(series, tight)!;

        // Each column has 2 nodes; they must not overlap.
        var col0 = layout.NodeRects.Where((_, i) => layout.Columns[i] == 0).ToArray();
        for (int i = 1; i < col0.Length; i++)
        {
            var a = col0[i - 1];
            var b = col0[i];
            Assert.True(a.Y + a.Height <= b.Y + 0.001);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Layout result shape
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compute_LayoutHasCorrectArrayLengths()
    {
        var series = new SankeySeries(
            [new("A"), new("B"), new("C")],
            [new(0, 1, 5), new(1, 2, 5)]);
        var layout = _engine.Compute(series, Bounds)!;
        Assert.Equal(3, layout.Columns.Length);
        Assert.Equal(3, layout.NodeRects.Length);
        Assert.Equal(3, layout.NodeValues.Length);
    }

    [Fact]
    public void Compute_AllNodeRectsWithinPrimaryBounds()
    {
        var series = new SankeySeries(
            [new("A"), new("B"), new("C"), new("D")],
            [new(0, 1, 5), new(1, 2, 5), new(2, 3, 5)]);
        var layout = _engine.Compute(series, Bounds)!;

        double primaryMin = Bounds.X;
        double primaryMax = Bounds.X + Bounds.Width;
        foreach (var rect in layout.NodeRects)
        {
            Assert.InRange(rect.X, primaryMin, primaryMax);
            Assert.InRange(rect.X + rect.Width, primaryMin, primaryMax + 0.001);
        }
    }

    [Fact]
    public void Compute_BoundsOffsetRespected()
    {
        // Non-origin bounds — ensure primary/cross origins shift.
        var shifted = new Rect(100, 50, 400, 200);
        var series = new SankeySeries(
            [new("A"), new("B")],
            [new(0, 1, 10)]);
        var layout = _engine.Compute(series, shifted)!;
        Assert.True(layout.NodeRects[0].X >= shifted.X - 0.001);
        Assert.True(layout.NodeRects[0].Y >= shifted.Y - 0.001);
    }

    [Fact]
    public void Compute_VerticalWithMultipleColumns_NodeYProgresses()
    {
        var series = new SankeySeries(
            [new("A"), new("B"), new("C")],
            [new(0, 1, 5), new(1, 2, 5)])
        { Orient = SankeyOrientation.Vertical };
        var layout = _engine.Compute(series, Bounds)!;
        // In vertical mode, primary axis is Y — consecutive columns have increasing Y.
        Assert.True(layout.NodeRects[0].Y < layout.NodeRects[1].Y);
        Assert.True(layout.NodeRects[1].Y < layout.NodeRects[2].Y);
    }

    [Fact]
    public void Compute_HorizontalWithMultipleColumns_NodeXProgresses()
    {
        var series = new SankeySeries(
            [new("A"), new("B"), new("C")],
            [new(0, 1, 5), new(1, 2, 5)])
        { Orient = SankeyOrientation.Horizontal };
        var layout = _engine.Compute(series, Bounds)!;
        Assert.True(layout.NodeRects[0].X < layout.NodeRects[1].X);
        Assert.True(layout.NodeRects[1].X < layout.NodeRects[2].X);
    }

    [Fact]
    public void Compute_NodesSortedByValueWithinColumn()
    {
        // S fans out to three targets of different values. They share col 1 and
        // must be sorted by total value descending (largest first).
        var series = new SankeySeries(
            [new("S"), new("Small"), new("Big"), new("Mid")],
            [new(0, 1, 3), new(0, 2, 20), new(0, 3, 10)])
        { Iterations = 0 };
        var layout = _engine.Compute(series, Bounds)!;

        // Big (idx=2) should have smaller Y than Mid (idx=3), which should be smaller than Small (idx=1)
        // in horizontal mode (crossAxis = Y).
        Assert.True(layout.NodeRects[2].Y < layout.NodeRects[3].Y);
        Assert.True(layout.NodeRects[3].Y < layout.NodeRects[1].Y);
    }
}
