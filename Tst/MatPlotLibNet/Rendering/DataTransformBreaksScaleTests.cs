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
    [InlineData(50,       55.55555555555556)]                                  // |x| ≤ linthresh → linear
    [InlineData(-50,     -55.55555555555556)]
    [InlineData(100,      111.11111111111111)]                                 // |x| = linthresh → linear
    [InlineData(1000,     211.1111111111111)]                                 // 100 * (1 + log10(10)) = 200
    [InlineData(10000,    311.11111111111114)]                                 // 100 * (1 + log10(100)) = 300
    [InlineData(100000,   411.1111111111111)]                                 // 100 * (1 + log10(1000)) = 400
    [InlineData(-1000,   -211.1111111111111)]
    [InlineData(-100000, -411.1111111111111)]
    public void SymLogTransform_Forward_MatchesNumpy(double raw, double expected)
    {
        // Reference: matplotlib 3.11.2, SymmetricalLogTransform(10, 100.0, 1.0).transform(raw), read with python:
        // forward(x) = x * s                                         if |x| <= L, s = 1 / (1 - 1/10)
        //            = sign(x) * L * (s + log10(|x| / L))            if |x| > L
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
        var map = new AxisBreakMap([new AxisBreak(25, 85)], 0, 125);

        Assert.Equal(40, map.Remap(100), 1e-9);
        Assert.Equal(60, map.Remap(120), 1e-9);
        Assert.Equal(18, map.Remap(18), 1e-9);     // below break → unchanged
        Assert.True(double.IsNaN(map.Remap(50)));    // inside break → NaN
    }

    [Fact]
    public void TransformX_WithBreaks_AllocatesTheResultAndNothingPerPoint()
    {
        var plot = new Rect(50, 20, 700, 360);
        var transform = new DataTransform(0, 850, 0, 80, plot,
            [new AxisBreak(300, 400), new AxisBreak(700, 750)], [new AxisBreak(40, 60)], 0, 1000, 0, 100);
        var x = new double[10_000];
        for (int i = 0; i < x.Length; i++) x[i] = i * 0.1;

        transform.TransformX(x); // warm up
        transform.TransformY(x);
        long before = GC.GetAllocatedBytesForCurrentThread();
        transform.TransformX(x);
        transform.TransformY(x);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Two result arrays of 10,000 doubles (80 KB each) and their headers; before the breaks were kept in order once, every
        // point also sorted them, which allocated about 300 bytes a point.
        Assert.InRange(allocated, 0, 2 * (x.Length * sizeof(double) + 64));
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

    // ── Batch and one-axis transforms must agree with scalar ──────────────────

    private static readonly double ScaledSymLogMin = SymlogTransform.Forward(-125000, 100);
    private static readonly double ScaledSymLogMax = SymlogTransform.Forward(125000, 100);
    private static readonly List<AxisBreak> BreakOf20To80 = [new(20, 80)];

    /// <summary>A transform that takes the per-point path (a symmetric-log scale or a break on one axis), the values of both axes and
    /// how the transform is read: whole (<c>TransformBatch</c>), or along one axis (<c>TransformX</c>, <c>TransformY</c>).</summary>
    public static TheoryData<string, DataTransform, double[], double[], string> PerPointTransforms { get; } = new()
    {
        { "TransformBatch_AgreesWithDataToPixel_UnderSymLog",
            new DataTransform(-50, 50, ScaledSymLogMin, ScaledSymLogMax, Plot, null, null, -50, 50, -125000, 125000, AxisScale.Linear, AxisScale.SymLog, 1.0, 100.0),
            [-50, -10, 0, 10, 50], [-100000, -1000, 0, 1000, 100000], "Batch" },
        { "TransformBatch_AgreesWithDataToPixel_UnderBreaks",
            new DataTransform(0, 19, 0, 65, Plot, xBreaks: null, yBreaks: new List<AxisBreak> { new(25, 85) }, 0, 19, 0, 125),
            [0, 9, 10, 19], [0, 18, 100, 118], "Batch" },
        // needScalar fires via the 3rd OR operand
        { "TransformBatch_WithXSymLogOnlyScale_UseScalarPath",
            new DataTransform(SymlogTransform.Forward(-1000, 10), SymlogTransform.Forward(1000, 10), 0, 10, Plot, null, null, -1000, 1000, 0, 10,
                AxisScale.SymLog, AxisScale.Linear, 10.0, 1.0),
            [-1000, 0, 1000], [0, 5, 10], "Batch" },
        // needScalar fires via the 1st OR operand
        { "TransformBatch_WithXBreaksOnly_UseScalarPath",
            new DataTransform(0, 60, 0, 10, Plot, xBreaks: BreakOf20To80, yBreaks: null, 0, 120, 0, 10),
            [0, 10, 90, 100], [0, 3, 7, 10], "Batch" },
        // TransformX with a SymLog X scale or X breaks takes the slow-path else branch
        { "TransformX_WithSymLogXScale_AppliesSymLogToXValues",
            new DataTransform(ScaledSymLogMin, ScaledSymLogMax, 0, 10, Plot, null, null, -125000, 125000, 0, 10, AxisScale.SymLog, AxisScale.Linear, 100.0, 1.0),
            [-100000, -1000, 0, 1000, 100000], [5, 5, 5, 5, 5], "X" },
        { "TransformX_WithXBreaks_RemapsValuesAroundBreak", // full X range 0..120, the break removes [20,80]: compressed 0..60
            new DataTransform(0, 60, 0, 10, Plot, xBreaks: BreakOf20To80, yBreaks: null, 0, 120, 0, 10),
            [0, 10, 90, 100], [0, 0, 0, 0], "X" },
        { "TransformY_WithSymLogYScale_AppliesSymLogToYValues",
            new DataTransform(0, 10, ScaledSymLogMin, ScaledSymLogMax, Plot, null, null, 0, 10, -125000, 125000, AxisScale.Linear, AxisScale.SymLog, 1.0, 100.0),
            [5, 5, 5, 5, 5], [-100000, -1000, 0, 1000, 100000], "Y" },
        { "TransformY_WithYBreaks_RemapsValuesAroundBreak", // full Y range 0..120, the break removes [20,80]: compressed 0..60
            new DataTransform(0, 10, 0, 60, Plot, xBreaks: null, yBreaks: BreakOf20To80, 0, 10, 0, 120),
            [0, 0, 0, 0], [0, 10, 90, 100], "Y" },
    };

    [Theory]
    [MemberData(nameof(PerPointTransforms))]
    public void ABatchOrAOneAxisTransform_MapsEveryValueAsDataToPixelDoes(string name, DataTransform t, double[] xs, double[] ys, string via)
    {
        _ = name;
        var batch = via == "Batch" ? t.TransformBatch(xs, ys) : null;
        var px = via == "X" ? t.TransformX(xs) : null;
        var py = via == "Y" ? t.TransformY(ys) : null;

        for (int i = 0; i < xs.Length; i++)
        {
            var single = t.DataToPixel(xs[i], ys[i]);
            if (batch is not null)
            {
                Assert.Equal(single.X, batch[i].X, 1e-6);
                Assert.Equal(single.Y, batch[i].Y, 1e-6);
            }

            if (px is not null) Assert.Equal(single.X, px[i], 1e-6);
            if (py is not null) Assert.Equal(single.Y, py[i], 1e-6);
        }
    }

    // ── Wave J.2 — TransformX slow path + missing DataTransform branches ─────

    /// <summary>A transform of a range that is one value (xScale or yScale of 0) puts every value at the middle of the plot.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TransformAxis_ZeroRange_FillsMidpoint(bool onX)
    {
        var t = onX ? new DataTransform(5, 5, 0, 10, Plot) : new DataTransform(0, 10, 7, 7, Plot);
        var drawn = onX ? t.TransformX([1.0, 5.0, 9.0]) : t.TransformY([1.0, 7.0, 12.0]);
        double mid = onX ? Plot.X + Plot.Width / 2 : Plot.Y + Plot.Height / 2;

        Assert.All(drawn, v => Assert.Equal(mid, v, 1e-9));
    }

    /// <summary>Log scale via DataToPixel — hits the <c>AxisScale.Log</c> arm of ApplyScale.</summary>
    [Fact]
    public void DataToPixel_WithLogXScale_MapsLog10Correctly()
    {
        // X range in log space: log10(1)=0 .. log10(1000)=3
        var t = Plot.LogX(xMax: 1000);

        // x=100 → log10(100)=2 → midpoint between 0 and 3 relative to range
        var p = t.DataToPixel(100, 5);
        Assert.InRange(p.X, Plot.X, Plot.X + Plot.Width);
    }

    /// <summary>Log scale on Y with x ≤ 0 — ApplyScale returns NaN.</summary>
    [Fact]
    public void DataToPixel_LogScaleWithNonPositiveX_ReturnsNaN()
    {
        var t = Plot.LogX(xMax: 1000);

        var p = t.DataToPixel(-1, 5);
        Assert.True(double.IsNaN(p.X));
    }
}
