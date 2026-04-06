// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

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
}
