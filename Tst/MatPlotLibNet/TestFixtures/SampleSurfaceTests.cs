// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>The surfaces the tests draw, each at the points a hand calculation gives.</summary>
public sealed class SampleSurfaceTests
{
    [Fact]
    public void ThePeaks_RunFromMinusThreeToThree_AndHaveMatplotlibsHeightAtTheOrigin()
    {
        var peaks = SampleSurface.Peaks(101);

        Assert.Equal(101, peaks.X.Length);
        Assert.Equal(-3.0, peaks.X[0]);
        Assert.Equal(3.0, peaks.X[^1], 12);
        Assert.Equal(0.0, peaks.X[50], 12);
        Assert.Equal(1.0, peaks.Z[50, 50], 12);                                  // (1 - 0 + 0 + 0) * exp(0)
        Assert.Same(peaks.X, peaks.Y);
    }

    [Fact]
    public void ThePeaksOverAxes_AreTheFormulaAtEachCrossing_YDownTheRows()
    {
        var peaks = SampleSurface.PeaksOver([1.0, 2.0], [0.0, 1.0, 2.0]);

        Assert.Equal(3, peaks.Z.GetLength(0));
        Assert.Equal(2, peaks.Z.GetLength(1));
        Assert.Equal((1 - 0.5 + 1 + 0) * Math.Exp(-1), peaks.Z[0, 0], 12);        // x = 1, y = 0
        Assert.Equal((1 - 1.0 + 32 + 1) * Math.Exp(-5), peaks.Z[1, 1], 12);       // x = 2, y = 1
    }

    [Fact]
    public void TheSinc_IsOneAtTheOrigin_AndSinROverRElsewhere()
    {
        var sinc = SampleSurface.Sinc(21);

        Assert.Equal(1.0, sinc.Z[10, 10], 12);
        double r = Math.Sqrt(2) * 3;
        Assert.Equal(Math.Sin(r) / r, sinc.Z[0, 0], 12);
        Assert.Equal(20, SampleSurface.Sinc().X.Length);
    }

    [Fact]
    public void TheSinR_IsTheSineOfTheDistanceFromTheOrigin()
    {
        var surface = SampleSurface.SinR([0.0, 3.0], [0.0, 4.0]);

        Assert.Equal(0.0, surface.Z[0, 0], 12);
        Assert.Equal(Math.Sin(5), surface.Z[1, 1], 12);
    }

    [Fact]
    public void TheRamp_AddsTheTwoAxes_AtEachCrossing()
    {
        Assert.Equal(5.0, SampleSurface.Ramp([1.0, 2.0], [3.0, 4.0]).Z[1, 0]);   // y = 4, x = 1
    }

    [Fact]
    public void TheRings_MeasureTheDistanceFromTheMiddleOfTheGrid()
    {
        var rings = SampleSurface.Rings();

        Assert.Equal(11, rings.X.Length);
        Assert.Equal(Math.Sqrt(2) * 5.5, rings.Z[0, 0], 12);
        Assert.Equal(rings.Z[5, 5], rings.Z[6, 6], 12);
    }

    [Fact]
    public void TheThreeByThree_CountsUpRowByRow()
    {
        Assert.Equal(8.0, SampleSurface.ThreeByThree().Z[2, 2]);
        Assert.Equal(3.0, SampleSurface.ThreeByThree().Z[1, 0]);
    }
}
