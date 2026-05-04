// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>v1.10 Phase 4 — pure-function tests for <see cref="PairGridLayout.ComputeCellRects"/>.
/// No rendering, no SVG; the layout is a deterministic geometry function and is tested as such.</summary>
public class PairGridLayoutTests
{
    private static readonly Rect Bounds100 = new(0, 0, 100, 100);

    // ── N = 1 ────────────────────────────────────────────────────────────────

    [Fact]
    public void N1_AnySpacing_ReturnsSingleRectMatchingBounds()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 1, 0.0);
        Assert.Equal(1, cells.GetLength(0));
        Assert.Equal(1, cells.GetLength(1));
        Assert.Equal(Bounds100, cells[0, 0]);
    }

    [Fact]
    public void N1_NonZeroSpacing_StillReturnsBoundsRect()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 1, 0.1);
        Assert.Equal(Bounds100, cells[0, 0]);
    }

    // ── Dimensions ────────────────────────────────────────────────────────────

    [Fact]
    public void N3_ReturnsThreeByThreeArray()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 3, 0.0);
        Assert.Equal(3, cells.GetLength(0));
        Assert.Equal(3, cells.GetLength(1));
    }

    [Fact]
    public void N10_ReturnsTenByTenArray()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 10, 0.02);
        Assert.Equal(10, cells.GetLength(0));
        Assert.Equal(10, cells.GetLength(1));
    }

    // ── Spacing = 0 (cells tile exactly) ──────────────────────────────────────

    [Fact]
    public void N2_ZeroSpacing_CellsTileExactly()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 2, 0.0);
        // 2x2 grid of 50x50 cells with no gutter
        Assert.Equal(50.0, cells[0, 0].Width, 10);
        Assert.Equal(50.0, cells[0, 0].Height, 10);
        Assert.Equal(0.0,  cells[0, 0].X, 10);
        Assert.Equal(0.0,  cells[0, 0].Y, 10);
        Assert.Equal(50.0, cells[0, 1].X, 10);
        Assert.Equal(50.0, cells[1, 0].Y, 10);
    }

    [Fact]
    public void N4_ZeroSpacing_AllCellsHaveEqualSize()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 4, 0.0);
        double expectedSide = 25.0;
        for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
        {
            Assert.Equal(expectedSide, cells[r, c].Width,  10);
            Assert.Equal(expectedSide, cells[r, c].Height, 10);
        }
    }

    [Fact]
    public void N3_ZeroSpacing_RowsCoverFullHeight()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 3, 0.0);
        // bottom of last row should equal bounds.Bottom
        Assert.Equal(Bounds100.Bottom, cells[2, 0].Bottom, 10);
        Assert.Equal(Bounds100.Right,  cells[0, 2].Right,  10);
    }

    // ── Spacing > 0 (cells separated by gutter) ───────────────────────────────

    [Fact]
    public void N2_SpacingTenPct_CellWidthIsNinetyPctDividedByN()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 2, 0.10);
        // total gutter = 10, gutter count = 1, gutter width = 10
        // total cell width = 90, cell width = 45
        Assert.Equal(45.0, cells[0, 0].Width, 10);
        Assert.Equal(45.0, cells[0, 0].Height, 10);
    }

    [Fact]
    public void N2_SpacingTenPct_GutterBetweenCells()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 2, 0.10);
        double rightOfFirstCol = cells[0, 0].Right;
        double leftOfSecondCol = cells[0, 1].X;
        // Gap between columns must equal the gutter (10 px for spacing=0.10)
        Assert.Equal(10.0, leftOfSecondCol - rightOfFirstCol, 10);
    }

    [Fact]
    public void N3_SpacingTwoPct_TwoEqualGuttersBetweenCells()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 3, 0.02);
        // total gutter = 2, gutter count = 2, gutter width = 1
        double gap1 = cells[0, 1].X - cells[0, 0].Right;
        double gap2 = cells[0, 2].X - cells[0, 1].Right;
        Assert.Equal(1.0, gap1, 10);
        Assert.Equal(1.0, gap2, 10);
    }

    // ── Boundary spacing values ───────────────────────────────────────────────

    [Fact]
    public void N3_SpacingTwentyPct_CellsStillPositive()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 3, 0.20);
        // total gutter = 20, total cell = 80, each cell = 80/3
        for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
        {
            Assert.True(cells[r, c].Width  > 0);
            Assert.True(cells[r, c].Height > 0);
        }
    }

    // ── Bounds containment ───────────────────────────────────────────────────

    [Fact]
    public void AllCells_StayWithinPlotBounds()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 5, 0.05);
        for (int r = 0; r < 5; r++)
        for (int c = 0; c < 5; c++)
        {
            Assert.True(cells[r, c].X     >= Bounds100.X     - 1e-9);
            Assert.True(cells[r, c].Y     >= Bounds100.Y     - 1e-9);
            Assert.True(cells[r, c].Right  <= Bounds100.Right  + 1e-9);
            Assert.True(cells[r, c].Bottom <= Bounds100.Bottom + 1e-9);
        }
    }

    // ── Non-origin / non-square bounds ────────────────────────────────────────

    [Fact]
    public void NonOriginBounds_CellsAreOffsetByBoundsXY()
    {
        var bounds = new Rect(100, 200, 60, 60);
        var cells = PairGridLayout.ComputeCellRects(bounds, 2, 0.0);
        Assert.Equal(100.0, cells[0, 0].X, 10);
        Assert.Equal(200.0, cells[0, 0].Y, 10);
        Assert.Equal(130.0, cells[0, 1].X, 10);
        Assert.Equal(230.0, cells[1, 0].Y, 10);
    }

    [Fact]
    public void NonSquareBounds_CellsHaveMatchingAspect()
    {
        var bounds = new Rect(0, 0, 200, 100);
        var cells = PairGridLayout.ComputeCellRects(bounds, 2, 0.0);
        Assert.Equal(100.0, cells[0, 0].Width, 10);
        Assert.Equal(50.0,  cells[0, 0].Height, 10);
    }

    // ── Cell ordering ─────────────────────────────────────────────────────────

    [Fact]
    public void CellOrdering_FirstIndexIsRow_SecondIsColumn()
    {
        var cells = PairGridLayout.ComputeCellRects(Bounds100, 3, 0.0);
        // Row 0 is at the top (smallest Y), row 2 at the bottom.
        Assert.True(cells[0, 0].Y < cells[1, 0].Y);
        Assert.True(cells[1, 0].Y < cells[2, 0].Y);
        // Column 0 is at the left (smallest X), column 2 at the right.
        Assert.True(cells[0, 0].X < cells[0, 1].X);
        Assert.True(cells[0, 1].X < cells[0, 2].X);
    }

    // ── Defensive input ───────────────────────────────────────────────────────

    [Fact]
    public void ZeroN_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PairGridLayout.ComputeCellRects(Bounds100, 0, 0.0));
    }

    [Fact]
    public void NegativeN_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PairGridLayout.ComputeCellRects(Bounds100, -1, 0.0));
    }
}
