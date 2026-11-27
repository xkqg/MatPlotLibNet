// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="SubPlotSpacing"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class SubPlotSpacingTests
{
    /// <summary>Verifies that TightLayout fluent method sets the flag on Figure.</summary>
    [Fact]
    public void FigureBuilder_TightLayout_SetsFlag()
    {
        var figure = Plt.Create().TightLayout().Plot([1.0], [2.0]).Build();
        Assert.True(figure.Spacing.TightLayout);
    }

    /// <summary>Verifies that WithSubPlotSpacing configures custom margins.</summary>
    [Fact]
    public void FigureBuilder_WithSubPlotSpacing_ConfiguresMargins()
    {
        var figure = Plt.Create()
            .WithSubPlotSpacing(s => s with { MarginLeft = 100, HorizontalGap = 20 })
            .Plot([1.0], [2.0])
            .Build();

        Assert.Equal(100, figure.Spacing.MarginLeft);
        Assert.Equal(20, figure.Spacing.HorizontalGap);
    }

    // --- Fractional support (v1.1.2) ---

    /// <summary>Verifies that FromFractions creates a fractional sentinel.</summary>
    [Fact]
    public void FromFractions_SetsFractionalFlag()
    {
        var sp = SubPlotSpacing.FromFractions(0.125, 0.10, 0.12, 0.12);
        Assert.True(sp.IsFractional);
    }

    /// <summary>Resolve turns each fraction FromFractions was given into the margin of its own side, in pixels.</summary>
    [Fact]
    public void Resolve_FractionalSpacing_ComputesAbsoluteValues()
    {
        var sp = SubPlotSpacing.FromFractions(0.125, 0.10, 0.12, 0.11);
        var resolved = sp.Resolve(800, 600);

        Assert.False(resolved.IsFractional);
        Assert.Equal(Math.Round(800 * 0.125), resolved.MarginLeft);
        Assert.Equal(Math.Round(800 * 0.10),  resolved.MarginRight);
        Assert.Equal(Math.Round(600 * 0.12),  resolved.MarginTop);
        Assert.Equal(Math.Round(600 * 0.11),  resolved.MarginBottom);
    }

    /// <summary>Verifies that Resolve on a non-fractional spacing returns itself unchanged.</summary>
    [Fact]
    public void Resolve_AbsoluteSpacing_ReturnsSelf()
    {
        var sp = new SubPlotSpacing { MarginLeft = 60 };
        var resolved = sp.Resolve(800, 600);
        Assert.Same(sp, resolved);
    }

    /// <summary>Verifies that Resolve preserves gap values from the fractional sentinel.</summary>
    [Fact]
    public void Resolve_PreservesGapValues()
    {
        var sp = SubPlotSpacing.FromFractions(0.1, 0.1, 0.1, 0.1, horizontalGap: 30, verticalGap: 25);
        var resolved = sp.Resolve(800, 600);
        Assert.Equal(30, resolved.HorizontalGap);
        Assert.Equal(25, resolved.VerticalGap);
    }
    // --- Gaps as a share of the average axes (matplotlib's figure.subplot.wspace / hspace) ---

    private static readonly SubPlotSpacing MatplotlibLike =
        SubPlotSpacing.FromFractions(0.125, 0.10, 0.12, 0.11) with { HorizontalGapFraction = 0.2, VerticalGapFraction = 0.2 };

    /// <summary>matplotlib's <c>GridSpecBase.get_grid_positions</c>: <c>cell_w = tot_width / (ncols + wspace * (ncols - 1))</c> and
    /// <c>sep_w = wspace * cell_w</c>, rows the same with <c>hspace</c>. At 800 x 600 with the default style's margins the room is 620
    /// by 462 pixels: two columns leave a gap of 0.2 * 620 / 2.2 and two rows one of 0.2 * 462 / 2.2.</summary>
    [Fact]
    public void Resolve_WithAGrid_TakesTheGapsAsAShareOfTheAverageCell()
    {
        var resolved = MatplotlibLike.Resolve(800, 600, rows: 2, columns: 2);

        Assert.Equal(0.2 * 620 / 2.2, resolved.HorizontalGap, 9);
        Assert.Equal(0.2 * 462 / 2.2, resolved.VerticalGap, 9);
        Assert.Equal(100.0, resolved.MarginLeft);
    }

    /// <summary>Three columns share the room with two gaps: <c>0.2 * 620 / (3 + 0.2 * 2)</c>.</summary>
    [Fact]
    public void Resolve_WithThreeColumns_CountsTwoGaps()
    {
        var resolved = MatplotlibLike.Resolve(800, 600, rows: 1, columns: 3);

        Assert.Equal(0.2 * 620 / 3.4, resolved.HorizontalGap, 9);
    }

    /// <summary>A resolved spacing holds pixels only: neither a margin fraction nor a gap fraction is left on it, so resolving it
    /// again changes nothing.</summary>
    [Fact]
    public void Resolve_LeavesNoFractionBehind()
    {
        var resolved = MatplotlibLike.Resolve(800, 600, rows: 2, columns: 2);

        Assert.False(resolved.IsFractional);
        Assert.Null(resolved.HorizontalGapFraction);
        Assert.Null(resolved.VerticalGapFraction);
        Assert.Same(resolved, resolved.Resolve(800, 600, rows: 2, columns: 2));
    }

    /// <summary>A gap is a share of the average cell, which the grid decides: without a grid the gaps keep their pixel value.</summary>
    [Fact]
    public void Resolve_WithoutAGrid_KeepsThePixelGaps()
    {
        var resolved = MatplotlibLike.Resolve(800, 600);

        Assert.Equal(40, resolved.HorizontalGap);
        Assert.Equal(40, resolved.VerticalGap);
        Assert.Null(resolved.HorizontalGapFraction);
    }

    /// <summary>A spacing that names no gap fraction keeps its pixel gaps whatever the grid.</summary>
    [Fact]
    public void Resolve_WithAGrid_KeepsPixelGapsWhenNoFractionIsNamed()
    {
        var sp = SubPlotSpacing.FromFractions(0.1, 0.1, 0.1, 0.1, horizontalGap: 30, verticalGap: 25);

        var resolved = sp.Resolve(800, 600, rows: 3, columns: 4);

        Assert.Equal(30, resolved.HorizontalGap);
        Assert.Equal(25, resolved.VerticalGap);
    }

    /// <summary>A pixel spacing is already resolved: it comes back as it is, with a grid or without.</summary>
    [Fact]
    public void Resolve_AbsoluteSpacingWithAGrid_ReturnsSelf()
    {
        var sp = new SubPlotSpacing { MarginLeft = 60 };

        Assert.Same(sp, sp.Resolve(800, 600, rows: 2, columns: 2));
    }


    /// <summary>A spacing in pixels that names a share for its gaps keeps its margins and works the gaps out from them: 800 x 600 with the
    /// margins of a new spacing (60, 20, 40, 50) leaves 720 by 510 pixels, and a share of 0.5 between three columns and two rows.</summary>
    [Fact]
    public void Resolve_PixelMarginsWithAGapShare_KeepsTheMargins_AndWorksTheGapsOut()
    {
        var sp = new SubPlotSpacing { HorizontalGapFraction = 0.5, VerticalGapFraction = 0.25 };

        var resolved = sp.Resolve(800, 600, rows: 2, columns: 3);

        Assert.Equal(60, resolved.MarginLeft);
        Assert.Equal(50, resolved.MarginBottom);
        Assert.Equal(0.5 * 720 / (3 + 0.5 * 2), resolved.HorizontalGap, 9);
        Assert.Equal(0.25 * 510 / (2 + 0.25), resolved.VerticalGap, 9);
    }
}
