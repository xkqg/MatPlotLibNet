// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="DataTransform"/> applies axis scales (Log / SymLog) and
/// break compression correctly. Includes parity assertions against the formulas from
/// matplotlib / numpy so behaviour is provably equivalent.
///
/// SymLog reference (numpy): for linthresh L,
///     forward(x) = sign(x) * L * (1 + log10(|x| / L))   when |x| > L
///                = x                                    when |x| ≤ L
/// Log10 reference (numpy): forward(x) = log10(x)        when x > 0
/// </summary>
public class DataTransformBreaksScaleTests
{
    private static readonly Rect Plot = new(100, 100, 400, 300);

    // ── Linear baseline (no breaks, no scale) ────────────────────────────────

    [Fact]
    public void Linear_DataToPixel_MatchesScalarFormula()
    {
        var t = new DataTransform(0, 100, 0, 50, Plot);
        // Y is inverted: y=50 → top (pixel 100), y=0 → bottom (pixel 400)
        var pTop    = t.DataToPixel(50, 50);
        var pBottom = t.DataToPixel(0, 0);
        var pMid    = t.DataToPixel(50, 25);

        Assert.Equal(300, pTop.X, 1e-9);     // x=50 → pixel 100 + 0.5*400 = 300
        Assert.Equal(100, pTop.Y, 1e-9);     // y=50 → top
        Assert.Equal(100, pBottom.X, 1e-9);  // x=0  → pixel 100
        Assert.Equal(400, pBottom.Y, 1e-9);  // y=0  → bottom
        Assert.Equal(250, pMid.Y, 1e-9);     // y=25 → middle
    }

    // ── SymLog scale parity with numpy ───────────────────────────────────────

    [Theory]
    [InlineData(0,        0)]
    [InlineData(50,       50)]                                  // |x| ≤ linthresh → linear
    [InlineData(-50,     -50)]
    [InlineData(100,      100)]                                 // |x| = linthresh → linear
    [InlineData(1000,     200)]                                 // 100 * (1 + log10(10)) = 200
    [InlineData(10000,    300)]                                 // 100 * (1 + log10(100)) = 300
    [InlineData(100000,   400)]                                 // 100 * (1 + log10(1000)) = 400
    [InlineData(-1000,   -200)]
    [InlineData(-100000, -400)]
    public void SymLogTransform_Forward_MatchesNumpy(double raw, double expected)
    {
        // Reference: numpy / matplotlib symlog with linthresh=100
        // forward(x) = x                                             if |x| ≤ L
        //            = sign(x) * L * (1 + log10(|x| / L))           if |x| > L
        double actual = SymlogTransform.Forward(raw, linthresh: 100);
        Assert.Equal(expected, actual, 1e-9);
    }

    [Fact]
    public void SymLog_DataToPixel_PutsTicksAtEvenlySpacedPositions()
    {
        // For symlog with linthresh=100, the data values 100, 1000, 10000, 100000
        // map to scaled values 100, 200, 300, 400 — these should be evenly spaced
        // in pixel space (each gap = 1/4 of the positive half).
        // Construct transform with FULL data range -125000..125000 mapped through symlog.
        double scaledMin = SymlogTransform.Forward(-125000, 100); // ≈ -410.7
        double scaledMax = SymlogTransform.Forward(125000, 100);  // ≈ +410.7

        var t = new DataTransform(
            -50, 50, scaledMin, scaledMax, Plot,
            xBreaks: null, yBreaks: null,
            -50, 50, -125000, 125000,
            AxisScale.Linear, AxisScale.SymLog, 1.0, 100.0);

        // Map decade values and check spacing
        double y100   = t.DataToPixel(0, 100).Y;
        double y1000  = t.DataToPixel(0, 1000).Y;
        double y10000 = t.DataToPixel(0, 10000).Y;

        // Each successive decade should be 100 scaled units higher → equal pixel gap
        double gap1 = y100 - y1000;
        double gap2 = y1000 - y10000;
        Assert.Equal(gap1, gap2, 1.0);  // within 1 pixel
        Assert.True(gap1 > 30,
            $"Tick spacing for adjacent decades is only {gap1}px — symlog scale not applied");
    }

    // ── Axis breaks ──────────────────────────────────────────────────────────

    [Fact]
    public void YBreak_Remap_CompressesValuesAcrossGap()
    {
        // Break removes [25, 85] (gap of 60). Full range 0..125 → compressed 0..65.
        // Value 100 (above break) should map to 100 - 60 = 40 in compressed space.
        var breaks = new List<AxisBreak> { new(25, 85) };

        Assert.Equal(40, AxisBreakMapper.Remap(breaks, 100, 0, 125), 1e-9);
        Assert.Equal(60, AxisBreakMapper.Remap(breaks, 120, 0, 125), 1e-9);
        Assert.Equal(18, AxisBreakMapper.Remap(breaks, 18, 0, 125), 1e-9);     // below break → unchanged
        Assert.True(double.IsNaN(AxisBreakMapper.Remap(breaks, 50, 0, 125)));    // inside break → NaN
    }

    [Fact]
    public void YBreak_DataToPixel_KeepsValuesInsidePlotArea()
    {
        // Compressed range 0..65 (full 0..125 minus break gap [25, 85] of width 60).
        var breaks = new List<AxisBreak> { new(25, 85) };
        var t = new DataTransform(
            0, 19, 0, 65, Plot,
            xBreaks: null, yBreaks: breaks,
            0, 19, 0, 125);

        // Both endpoints of the data range should be inside the plot area
        var pLow  = t.DataToPixel(0,  18);    // below break
        var pHigh = t.DataToPixel(19, 118);   // above break

        Assert.InRange(pLow.Y,  Plot.Y, Plot.Y + Plot.Height);
        Assert.InRange(pHigh.Y, Plot.Y, Plot.Y + Plot.Height);
    }

    // ── Batch transforms must agree with scalar ──────────────────────────────

    [Fact]
    public void TransformBatch_AgreesWithDataToPixel_UnderSymLog()
    {
        double scaledMin = SymlogTransform.Forward(-125000, 100);
        double scaledMax = SymlogTransform.Forward(125000, 100);

        var t = new DataTransform(
            -50, 50, scaledMin, scaledMax, Plot,
            null, null,
            -50, 50, -125000, 125000,
            AxisScale.Linear, AxisScale.SymLog, 1.0, 100.0);

        double[] xs = [-50, -10, 0, 10, 50];
        double[] ys = [-100000, -1000, 0, 1000, 100000];

        var batch = t.TransformBatch(xs, ys);
        for (int i = 0; i < xs.Length; i++)
        {
            var single = t.DataToPixel(xs[i], ys[i]);
            Assert.Equal(single.X, batch[i].X, 1e-6);
            Assert.Equal(single.Y, batch[i].Y, 1e-6);
        }
    }

    [Fact]
    public void TransformBatch_AgreesWithDataToPixel_UnderBreaks()
    {
        var breaks = new List<AxisBreak> { new(25, 85) };
        var t = new DataTransform(
            0, 19, 0, 65, Plot,
            xBreaks: null, yBreaks: breaks,
            0, 19, 0, 125);

        double[] xs = [0, 9, 10, 19];
        double[] ys = [0, 18, 100, 118];

        var batch = t.TransformBatch(xs, ys);
        for (int i = 0; i < xs.Length; i++)
        {
            var single = t.DataToPixel(xs[i], ys[i]);
            Assert.Equal(single.X, batch[i].X, 1e-6);
            Assert.Equal(single.Y, batch[i].Y, 1e-6);
        }
    }
}
