// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="SubPlotSpacing"/> default values and record behavior.</summary>
public class SubPlotSpacingTests
{
    /// <summary>Verifies that default margins match the previous hardcoded values.</summary>
    [Fact]
    public void Default_HasExpectedMargins()
    {
        var spacing = new SubPlotSpacing();
        Assert.Equal(60, spacing.MarginLeft);
        Assert.Equal(20, spacing.MarginRight);
        Assert.Equal(40, spacing.MarginTop);
        Assert.Equal(50, spacing.MarginBottom);
        Assert.Equal(40, spacing.HorizontalGap);
        Assert.Equal(40, spacing.VerticalGap);
    }

    /// <summary>Verifies that TightLayout defaults to false.</summary>
    [Fact]
    public void TightLayout_DefaultsFalse()
    {
        var spacing = new SubPlotSpacing();
        Assert.False(spacing.TightLayout);
    }

    /// <summary>Verifies that the with expression creates a modified copy.</summary>
    [Fact]
    public void WithExpression_CreatesModifiedCopy()
    {
        var spacing = new SubPlotSpacing() with { MarginLeft = 30, TightLayout = true };
        Assert.Equal(30, spacing.MarginLeft);
        Assert.True(spacing.TightLayout);
        Assert.Equal(20, spacing.MarginRight); // unchanged
    }

    /// <summary>Verifies that Figure.Spacing defaults to a new SubPlotSpacing.</summary>
    [Fact]
    public void Figure_Spacing_DefaultIsNotNull()
    {
        var figure = new Figure();
        Assert.NotNull(figure.Spacing);
        Assert.Equal(60, figure.Spacing.MarginLeft);
    }

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

    /// <summary>Verifies that FromFractions stores the supplied fraction values.</summary>
    [Fact]
    public void FromFractions_StoresFractions()
    {
        var sp = SubPlotSpacing.FromFractions(0.125, 0.10, 0.12, 0.12, horizontalGap: 30, verticalGap: 25);
        Assert.Equal(0.125, sp.FractLeft);
        Assert.Equal(0.10,  sp.FractRight);
        Assert.Equal(0.12,  sp.FractTop);
        Assert.Equal(0.12,  sp.FractBottom);
        Assert.Equal(30,    sp.HorizontalGap);
        Assert.Equal(25,    sp.VerticalGap);
    }

    /// <summary>Verifies that Resolve converts fractional margins to absolute pixel values.</summary>
    [Fact]
    public void Resolve_FractionalSpacing_ComputesAbsoluteValues()
    {
        var sp = SubPlotSpacing.FromFractions(0.125, 0.10, 0.12, 0.12);
        var resolved = sp.Resolve(800, 600);

        Assert.False(resolved.IsFractional);
        Assert.Equal(Math.Round(800 * 0.125), resolved.MarginLeft);
        Assert.Equal(Math.Round(800 * 0.10),  resolved.MarginRight);
        Assert.Equal(Math.Round(600 * 0.12),  resolved.MarginTop);
        Assert.Equal(Math.Round(600 * 0.12),  resolved.MarginBottom);
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
}
