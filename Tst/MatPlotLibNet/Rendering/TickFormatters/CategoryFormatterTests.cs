// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>v1.12.0 — Verifies <see cref="CategoryFormatter"/> label mapping,
/// bounds-checking, reversed mode, and constructor validation.</summary>
public class CategoryFormatterTests
{
    private static readonly string[] Labels = ["BTC", "ETH", "BNB", "SOL", "ADA"];

    // ── Happy path — normal ──────────────────────────────────────────────────

    [Fact]
    public void Format_IndexZero_ReturnsFirstLabel()
    {
        var f = new CategoryFormatter(Labels);
        Assert.Equal("BTC", f.Format(0));
    }

    [Fact]
    public void Format_IndexLast_ReturnsLastLabel()
    {
        var f = new CategoryFormatter(Labels);
        Assert.Equal("ADA", f.Format(Labels.Length - 1));
    }

    [Fact]
    public void Format_MiddleIndex_ReturnsCorrectLabel()
    {
        var f = new CategoryFormatter(Labels);
        Assert.Equal("BNB", f.Format(2));
    }

    // ── Bounds ───────────────────────────────────────────────────────────────

    [Fact]
    public void Format_BelowZero_ReturnsEmpty()
    {
        var f = new CategoryFormatter(Labels);
        Assert.Equal(string.Empty, f.Format(-1));
    }

    [Fact]
    public void Format_AboveLength_ReturnsEmpty()
    {
        var f = new CategoryFormatter(Labels);
        Assert.Equal(string.Empty, f.Format(Labels.Length));
    }

    // ── Reversed ─────────────────────────────────────────────────────────────

    [Fact]
    public void Format_Reversed_IndexZero_ReturnsLastLabel()
    {
        var f = new CategoryFormatter(Labels, reversed: true);
        Assert.Equal("ADA", f.Format(0));
    }

    [Fact]
    public void Format_Reversed_IndexLast_ReturnsFirstLabel()
    {
        var f = new CategoryFormatter(Labels, reversed: true);
        Assert.Equal("BTC", f.Format(Labels.Length - 1));
    }

    [Fact]
    public void Format_Reversed_OutOfBounds_ReturnsEmpty()
    {
        var f = new CategoryFormatter(Labels, reversed: true);
        Assert.Equal(string.Empty, f.Format(Labels.Length));
        Assert.Equal(string.Empty, f.Format(-1));
    }

    /// <summary>What the reversed formatter is for, pinned on a drawn heatmap: the heatmap draws its row 0 at the TOP while
    /// the Y axis counts upward, so the tick at the bottom (index 0) must read the last name. With <c>reversed: true</c> on
    /// the cell centres the names read from the top down in the order of the rows, each level with its row.</summary>
    [Fact]
    public void Reversed_NamesAHeatmapsRowsFromTheTopDown_WhereRowZeroIsDrawn()
    {
        string[] rows = ["first", "second", "third"];
        string svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 101 }, { 202 }, { 303 } }, s => { s.ShowLabels = true; s.LabelFormat = "F0"; })
                .SetYTickLocator(new MatPlotLibNet.Rendering.TickLocators.FixedLocator([0.5, 1.5, 2.5]))
                .SetYTickFormatter(new CategoryFormatter(rows, reversed: true)))
            .ToSvg();

        double Y(string text) => double.Parse(
            System.Text.RegularExpressions.Regex.Match(svg, $@"<text[^>]*?\sy=""([-0-9.]+)""[^>]*>{text}</text>").Groups[1].Value,
            System.Globalization.CultureInfo.InvariantCulture);
        double[] cells = [Y("101"), Y("202"), Y("303")];
        double[] names = [Y("first"), Y("second"), Y("third")];

        Assert.True(cells[0] < cells[1] && cells[1] < cells[2], "row 0 is drawn at the top");
        double rowHeight = cells[1] - cells[0];
        for (int r = 0; r < 3; r++)
        {
            Assert.InRange(names[r], cells[r] - rowHeight / 2, cells[r] + rowHeight / 2);
        }
    }

    // ── Fractional values ─────────────────────────────────────────────────────

    [Fact]
    public void Format_FractionalValue_TruncatesToInt()
    {
        var f = new CategoryFormatter(Labels);
        Assert.Equal("BTC", f.Format(0.9));   // (int)0.9 = 0
        Assert.Equal("ETH", f.Format(1.9));   // (int)1.9 = 1
    }

    // ── At positions of their own ────────────────────────────────────────────

    /// <summary>Names at positions of their own, for categories whose places along the axis are not slots of width one (the
    /// columns of a Marimekko chart, whose widths are shares): a tick at <c>positions[k]</c> reads label <c>k</c>, whatever
    /// slot the position falls in.</summary>
    [Fact]
    public void AtPositions_ATickOnAPosition_ReadsTheLabelOfThatPosition()
    {
        var f = new CategoryFormatter(["Retail", "Online", "Wholesale"], [20.0, 60.5, 90.25]);

        Assert.Equal("Retail", f.Format(20.0));
        Assert.Equal("Online", f.Format(60.5));
        Assert.Equal("Wholesale", f.Format(90.25));
    }

    /// <summary>A tick anywhere else reads nothing, also on the slots the slot rule would name (0, 1 and 2 hold three labels),
    /// a hair beside a position, and at a missing value.</summary>
    [Fact]
    public void AtPositions_ATickAnywhereElse_ReadsNothing()
    {
        var f = new CategoryFormatter(["Retail", "Online", "Wholesale"], [20.0, 60.5, 90.25]);

        Assert.Equal(string.Empty, f.Format(0));
        Assert.Equal(string.Empty, f.Format(1));
        Assert.Equal(string.Empty, f.Format(2));
        Assert.Equal(string.Empty, f.Format(20.000001));
        Assert.Equal(string.Empty, f.Format(double.NaN));
    }

    /// <summary>Two positions in one slot of width one (two narrow columns) each read their own label; the slot rule reads
    /// the first label for both.</summary>
    [Fact]
    public void AtPositions_TwoPositionsInOneSlot_ReadTheirOwnLabels()
    {
        var f = new CategoryFormatter(["narrow", "next"], [0.2, 0.7]);

        Assert.Equal("narrow", f.Format(0.2));
        Assert.Equal("next", f.Format(0.7));
    }

    /// <summary>The names and their positions are pairs: no names, no positions, or a count that does not pair up is refused
    /// with the parameter named.</summary>
    [Fact]
    public void AtPositions_NamesAndPositionsThatDoNotPairUp_AreRefused()
    {
        Assert.Throws<ArgumentNullException>("labels", () => new CategoryFormatter(null!, [1.0]));
        Assert.Throws<ArgumentNullException>("positions", () => new CategoryFormatter(["a"], (double[])null!));

        var refusal = Assert.Throws<ArgumentException>("positions", () => new CategoryFormatter(["a", "b"], [1.0]));
        Assert.StartsWith("1 positions were given for 2 labels; there must be one position per label.", refusal.Message);
    }

    // ── Constructor validation ────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullLabels_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CategoryFormatter(null!));
    }

    [Fact]
    public void Constructor_EmptyLabels_DoesNotThrow()
    {
        var f = new CategoryFormatter([]);
        Assert.Equal(string.Empty, f.Format(0));
    }

    // ── Interface contract ────────────────────────────────────────────────────

    [Fact]
    public void Implements_ITickFormatter()
    {
        Assert.IsAssignableFrom<ITickFormatter>(new CategoryFormatter(Labels));
    }
}
