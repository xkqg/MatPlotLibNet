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

    // ── Fractional values ─────────────────────────────────────────────────────

    [Fact]
    public void Format_FractionalValue_TruncatesToInt()
    {
        var f = new CategoryFormatter(Labels);
        Assert.Equal("BTC", f.Format(0.9));   // (int)0.9 = 0
        Assert.Equal("ETH", f.Format(1.9));   // (int)1.9 = 1
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
