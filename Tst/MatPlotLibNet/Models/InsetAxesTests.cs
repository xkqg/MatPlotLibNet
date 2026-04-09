// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies inset axes behavior on <see cref="Axes"/>.</summary>
public class InsetAxesTests
{
    [Fact]
    public void InsetBounds_StoresValues()
    {
        var bounds = new InsetBounds(0.6, 0.6, 0.35, 0.35);
        Assert.Equal(0.6, bounds.X);
        Assert.Equal(0.6, bounds.Y);
        Assert.Equal(0.35, bounds.Width);
        Assert.Equal(0.35, bounds.Height);
    }

    [Fact]
    public void InsetBounds_RecordEquality_Works()
    {
        var a = new InsetBounds(0.6, 0.6, 0.35, 0.35);
        var b = new InsetBounds(0.6, 0.6, 0.35, 0.35);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Axes_Insets_DefaultEmpty()
    {
        var axes = new Axes();
        Assert.Empty(axes.Insets);
    }

    [Fact]
    public void AddInset_ReturnsNewAxes()
    {
        var axes = new Axes();
        var inset = axes.AddInset(0.6, 0.6, 0.3, 0.3);
        Assert.NotNull(inset);
    }

    [Fact]
    public void AddInset_AddsToInsetsList()
    {
        var axes = new Axes();
        axes.AddInset(0.6, 0.6, 0.3, 0.3);
        Assert.Single(axes.Insets);
    }

    [Fact]
    public void AddInset_SetsInsetBounds()
    {
        var axes = new Axes();
        var inset = axes.AddInset(0.6, 0.6, 0.3, 0.3);
        Assert.NotNull(inset.InsetBounds);
        Assert.Equal(0.6, inset.InsetBounds.Value.X);
        Assert.Equal(0.3, inset.InsetBounds.Value.Width);
    }

    [Fact]
    public void AddInset_InsetCanHaveSeries()
    {
        var axes = new Axes();
        var inset = axes.AddInset(0.6, 0.6, 0.3, 0.3);
        inset.Plot([1.0, 2.0], [3.0, 4.0]);
        Assert.Single(inset.Series);
    }

    [Fact]
    public void MultipleInsets_AllStored()
    {
        var axes = new Axes();
        axes.AddInset(0.0, 0.0, 0.3, 0.3);
        axes.AddInset(0.7, 0.7, 0.3, 0.3);
        Assert.Equal(2, axes.Insets.Count);
    }
}
