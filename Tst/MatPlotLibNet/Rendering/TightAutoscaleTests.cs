// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A hexbin asks its axes for a tight autoscale. matplotlib 3.11.2, <c>Axes.hexbin</c>: <c>self.update_datalim(corners)</c>
/// then <c>self._request_autoscale_view(tight=True)</c>, and <c>_AxesBase.autoscale_view</c> skips <c>locator.view_limits</c>
/// (the rounding out to the ticks) when <c>self._tight</c> is set: "If True, only expand the axis limits using the margins".
/// The flag is the axes', so a line on the same axes is ranged tight too, whichever was drawn first. Measured with matplotlib
/// 3.11.2: <c>ax.hexbin([0.3, 2.0, 4.1, 9.7], [1.1, 5.3, 2.2, 7.9], gridsize=5)</c> under the classic style ends at 0.3 .. 9.7 and
/// 1.1 .. 7.9 (a line over the same points: 0 .. 10 and 1 .. 8); with <c>ax.plot([0.5, 11.3], [0.5, 11.3])</c> beside it,
/// 0.3 .. 11.3 and 0.5 .. 11.3; under the default style -0.17 .. 10.17 and 0.76 .. 8.24.</summary>
public sealed class TightAutoscaleTests
{
    private static readonly double[] X = [0.3, 2.0, 4.1, 9.7];
    private static readonly double[] Y = [1.1, 5.3, 2.2, 7.9];

    private static Axes With(params ISeries[] series)
    {
        var axes = new Axes();
        foreach (var s in series) axes.AddSeries(s);
        return axes;
    }

    /// <summary>The classic style ends a hexbin at its data, not at the round numbers; the default style ends it at its data and the margin.</summary>
    [Theory]
    [InlineData("classic", 0.3, 9.7, 1.1, 7.9)]
    [InlineData("v2", -0.17, 10.17, 0.76, 8.24)]
    public void AHexbin_EndsAtItsData_NotAtTheRoundNumbers(string theme, double xMin, double xMax, double yMin, double yMax)
    {
        var range = With(new HexbinSeries(X, Y)).ComputeDataRanges(theme.AsTheme());

        new Range1D(xMin, xMax).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-6);
        new Range1D(yMin, yMax).AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-6);
    }

    [Fact]
    public void Classic_ALineBesideAHexbin_IsRangedTightToo_InEitherOrder()
    {
        var after = With(new HexbinSeries(X, Y), new LineSeries([0.5, 11.3], [0.5, 11.3])).ComputeDataRanges(Theme.MatplotlibClassic);
        var before = With(new LineSeries([0.5, 11.3], [0.5, 11.3]), new HexbinSeries(X, Y)).ComputeDataRanges(Theme.MatplotlibClassic);

        new Range1D(0.3, 11.3).AssertCloseTo(new Range1D(after.XMin, after.XMax), 1e-6);
        new Range1D(0.5, 11.3).AssertCloseTo(new Range1D(after.YMin, after.YMax), 1e-6);
        new Range1D(0.3, 11.3).AssertCloseTo(new Range1D(before.XMin, before.XMax), 1e-6);
        new Range1D(0.5, 11.3).AssertCloseTo(new Range1D(before.YMin, before.YMax), 1e-6);
    }

    /// <summary>The same line alone still rounds out under the classic theme: the tight range is the hexbin's request, not the
    /// theme's.</summary>
    [Fact]
    public void Classic_TheLineAlone_StillRoundsOut()
    {
        var range = With(new LineSeries(X, Y)).ComputeDataRanges(Theme.MatplotlibClassic);

        new Range1D(0, 10).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-6);
        new Range1D(1, 8).AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-6);
    }

    /// <summary>The library's own themes widen a range to the nice bounds of its data (here X 0.3 .. 9.0 to 0 .. 10 past the
    /// margin's 9.435); a hexbin's request for a tight range holds there too: the data and the 5 % margin.</summary>
    [Fact]
    public void DefaultTheme_AHexbin_KeepsTheMargin_NotTheNiceBounds()
    {
        var hexbin = With(new HexbinSeries([0.3, 2.0, 4.1, 9.0], Y)).ComputeDataRanges(Theme.Default);
        var line = With(new LineSeries([0.3, 2.0, 4.1, 9.0], Y)).ComputeDataRanges(Theme.Default);

        new Range1D(-0.135, 9.435).AssertCloseTo(new Range1D(hexbin.XMin, hexbin.XMax), 1e-6);
        Assert.Equal(10, line.XMax, 6);
    }

    // A contour of any kind asks for a tight range too. matplotlib 3.11.2, contour.py ContourSet._process_args:
    // "self.axes.autoscale_view(tight=True)" after update_datalim, for contour, contourf and tricontour alike (one ContourSet).
    // Measured: ax.tricontour(x, y, z) over points reaching 2.95 ends at the data under the classic style; the library rounded
    // that out to -3 .. 3. Under the library's own themes (no autolimit mode named) a contour keeps the bounds it was always drawn in.
    private static readonly double[] CX = [-2.95, -1.0, 0.4, 2.95];
    private static readonly double[] CY = [-2.4, -0.3, 1.1, 2.7];

    private static Axes ContourKind(int kind)
    {
        double[,] z = new double[4, 4];
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++) z[r, c] = r + c;
        ISeries series = kind switch
        {
            0 => new ContourSeries(CX, CY, z),
            1 => new ContourfSeries(CX, CY, z),
            _ => new TricontourSeries(new Vec(CX), new Vec(CY), new Vec([1, 2, 3, 4])),
        };
        return With(series);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Classic_AContour_EndsAtItsData_NotAtTheRoundNumbers(int kind)
    {
        var range = ContourKind(kind).ComputeDataRanges(Theme.MatplotlibClassic);

        new Range1D(-2.95, 2.95).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-6);
        new Range1D(-2.4, 2.7).AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void V2_AContour_EndsAtItsData(int kind)
    {
        var range = ContourKind(kind).ComputeDataRanges(Theme.MatplotlibV2);

        new Range1D(-2.95, 2.95).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-6);
        new Range1D(-2.4, 2.7).AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-6);
    }

    /// <summary>Beside a contour a line is ranged tight too (the flag is the axes'), as with a hexbin.</summary>
    [Fact]
    public void Classic_ALineBesideAContour_IsRangedTightToo()
    {
        var axes = ContourKind(0);
        axes.AddSeries(new LineSeries([-2.0, 2.2], [-1.3, 1.7]));

        var range = axes.ComputeDataRanges(Theme.MatplotlibClassic);

        new Range1D(-2.95, 2.95).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-6);
        new Range1D(-2.4, 2.7).AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-6);
    }

    /// <summary>The library's own themes name no autolimit mode: a contour there keeps the bounds it was drawn in.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void DefaultTheme_AContour_KeepsItsBounds(int kind)
    {
        var tight = ContourKind(kind).ComputeDataRanges(Theme.Default);
        var withoutRequest = With(new LineSeries(CX, CY)).ComputeDataRanges(Theme.Default);

        // the contour's sticky edges end it at the data under the library's themes, as they always did
        new Range1D(-2.95, 2.95).AssertCloseTo(new Range1D(tight.XMin, tight.XMax), 1e-6);
        Assert.True(withoutRequest.XMin <= -2.95);
    }
}
