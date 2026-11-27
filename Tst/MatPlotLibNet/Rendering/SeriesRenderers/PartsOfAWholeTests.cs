// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The slices of a whole (a pie's, a donut's) have one guard: no total above nothing draws no slice. matplotlib 3.11.2 refuses
/// a pie of negative sizes (<c>ValueError: Wedge sizes 'x' must be non negative values</c>) and of all zeros (<c>All wedge sizes are
/// zero</c>); the library draws nothing for either, as its donut already did and its pie did for zeros.</summary>
public class PartsOfAWholeTests
{
    private static readonly Rect Plot = new(80, 40, 400, 300);

    // ── The guard of a whole made of slices ──────────────────────────────────────────

    [Theory]
    [InlineData(-3.0, -1.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(-4.0, 1.0)]
    public void APieWhoseSlicesTotalNothingOrLess_DrawsNoSlice(double first, double second)
    {
        var axes = new Axes();
        axes.Pie([first, second]);

        Assert.Equal(0, axes.Drawn(Plot).Count<DrawPathCall>());
    }

    [Theory]
    [InlineData(-3.0, -1.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(-4.0, 1.0)]
    public void ADonutWhoseSlicesTotalNothingOrLess_DrawsNoWedge(double first, double second)
    {
        var axes = new Axes();
        axes.Donut([first, second]);

        Assert.Equal(0, axes.Drawn(Plot).Count<DrawPathCall>());
    }

    [Fact]
    public void APieAndADonutOfSlicesThatTotalSomething_DrawOneShapeForEachSlice()
    {
        var pie = new Axes();
        pie.Pie([1.0, 2.0, 3.0]);
        var donut = new Axes();
        donut.Donut([1.0, 2.0, 3.0]);

        Assert.Equal(3, pie.Drawn(Plot).Count<DrawPathCall>());
        Assert.Equal(3, donut.Drawn(Plot).Count<DrawPathCall>());
    }

    [Theory]
    [InlineData(new double[] { 1, 2, 3 }, 6.0)]
    [InlineData(new double[] { 1, double.NaN, 3 }, 4.0)]
    [InlineData(new double[] { 1, double.PositiveInfinity, 3 }, 4.0)]
    public void TheTotalOfTheSlices_LeavesOutWhatCannotBeDrawn(double[] sizes, double total)
    {
        Assert.Equal(total, sizes.DrawableTotal());
    }

    [Theory]
    [InlineData(new double[0])]
    [InlineData(new double[] { 0, 0 })]
    [InlineData(new double[] { -1, -2 })]
    [InlineData(new double[] { double.NaN, double.NaN })]
    [InlineData(new double[] { -3, 1 })]
    public void ASetOfSlicesThatTotalNothingOrLess_HasNoTotal(double[] sizes)
    {
        Assert.Null(sizes.DrawableTotal());
    }
}
