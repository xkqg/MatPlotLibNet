// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="AxisBreakMap"/>: the breaks of one axis, put in order once, mapping a value from data space to
/// the compressed space the axis is drawn in.</summary>
public class AxisBreakMapTests
{
    // ---- IsInBreak ---------------------------------------------------------

    [Fact]
    public void IsInBreak_True_WhenInRange()
    {
        var map = new AxisBreakMap([new AxisBreak(80, 120)], 0, 200);
        Assert.True(map.IsInBreak(100));
    }

    [Fact]
    public void IsInBreak_False_WhenOutsideRange()
    {
        var map = new AxisBreakMap([new AxisBreak(80, 120)], 0, 200);
        Assert.False(map.IsInBreak(50));
    }

    [Theory]
    [InlineData(80)]
    [InlineData(120)]
    public void IsInBreak_False_AtBothBoundaries(double boundary)
    {
        var map = new AxisBreakMap([new AxisBreak(80, 120)], 0, 200);
        Assert.False(map.IsInBreak(boundary));
    }

    [Fact]
    public void IsInBreak_True_WhenInTheSecondBreak()
    {
        var map = new AxisBreakMap([new AxisBreak(10, 20), new AxisBreak(80, 120)], 0, 200);
        Assert.True(map.IsInBreak(100));
    }

    // ---- CompressedRange ---------------------------------------------------

    [Fact]
    public void CompressedRange_SingleBreak_ReducesRange()
    {
        var (cMin, cMax) = new AxisBreakMap([new AxisBreak(80, 120)], 0, 200).CompressedRange;
        Assert.Equal(0.0, cMin, 1e-9);
        Assert.Equal(160.0, cMax, 1e-9); // 200 - (120-80)
    }

    [Fact]
    public void CompressedRange_NoBreaks_Unchanged()
    {
        var (cMin, cMax) = new AxisBreakMap([], 0, 100).CompressedRange;
        Assert.Equal(0.0, cMin, 1e-9);
        Assert.Equal(100.0, cMax, 1e-9);
    }

    [Fact]
    public void CompressedRange_BreakPartlyOutsideTheRange_TakesOnlyTheGapInsideIt()
    {
        var (cMin, cMax) = new AxisBreakMap([new AxisBreak(-50, 20), new AxisBreak(90, 300)], 0, 100).CompressedRange;
        Assert.Equal(0.0, cMin, 1e-9);
        Assert.Equal(100.0 - 20.0 - 10.0, cMax, 1e-9);
    }

    [Fact]
    public void CompressedRange_BreakEntirelyOutsideTheRange_TakesNothing()
    {
        var (_, cMax) = new AxisBreakMap([new AxisBreak(500, 600)], 0, 100).CompressedRange;
        Assert.Equal(100.0, cMax, 1e-9);
    }

    // ---- Remap -------------------------------------------------------------

    [Fact]
    public void Remap_BeforeBreak_Unchanged()
    {
        Assert.Equal(30.0, new AxisBreakMap([new AxisBreak(80, 120)], 0, 200).Remap(30), 1e-9);
    }

    [Fact]
    public void Remap_AfterBreak_ShiftedByGap()
    {
        Assert.Equal(110.0, new AxisBreakMap([new AxisBreak(80, 120)], 0, 200).Remap(150), 1e-9); // 150 - 40
    }

    [Fact]
    public void Remap_InBreak_ReturnsNaN()
    {
        Assert.True(double.IsNaN(new AxisBreakMap([new AxisBreak(80, 120)], 0, 200).Remap(100)));
    }

    [Fact]
    public void Remap_BreaksHandedOverOutOfOrder_ShiftByEveryGapBelowTheValue()
    {
        var map = new AxisBreakMap([new AxisBreak(60, 70), new AxisBreak(10, 20), new AxisBreak(30, 45)], 0, 100);

        Assert.Equal(90 - (10.0 + 15.0 + 10.0), map.Remap(90), 1e-9);
        Assert.Equal(50 - (10.0 + 15.0), map.Remap(50), 1e-9);
    }

    [Fact]
    public void Remap_TheGapClippedToTheFullRange_IsTheGapTheValueIsShiftedBy()
    {
        // The break reaches below the range, so only [0, 20] of it counts.
        var map = new AxisBreakMap([new AxisBreak(-30, 20)], 0, 100);

        Assert.Equal(60 - 20.0, map.Remap(60), 1e-9);
    }

    [Fact]
    public void Remap_ABreakWhollyOutsideTheFullRange_ShiftsNothing()
    {
        var map = new AxisBreakMap([new AxisBreak(-40, -10)], 0, 100);

        Assert.Equal(60.0, map.Remap(60), 1e-9);
    }

    [Fact]
    public void Remap_NaNInput_StaysNaN()
    {
        Assert.True(double.IsNaN(new AxisBreakMap([new AxisBreak(80, 120)], 0, 200).Remap(double.NaN)));
    }

    [Fact]
    public void Remap_WithoutAllocating()
    {
        var map = new AxisBreakMap([new AxisBreak(80, 120), new AxisBreak(10, 20)], 0, 200);
        int placed = 0;
        for (int i = 0; i < 100; i++) placed += double.IsNaN(map.Remap(i * 2.0)) ? 0 : 1; // warm up the JIT

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) placed += double.IsNaN(map.Remap(i * 0.2)) ? 0 : 1;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.True(placed > 0);
    }

    [Fact]
    public void Remap_AgreesBitForBit_WithTheSortOnEveryCall_OnBreaksInAnyOrder()
    {
        var random = new Random(7);
        for (int trial = 0; trial < 50; trial++)
        {
            var breaks = new List<AxisBreak>();
            int count = random.Next(1, 6);
            for (int b = 0; b < count; b++)
            {
                double from = Math.Round(random.NextDouble() * 180 - 10, 1);
                breaks.Add(new AxisBreak(from, from + Math.Round(random.NextDouble() * 25 + 0.1, 1)));
            }
            var map = new AxisBreakMap(breaks, 0, 150);

            for (int v = -20; v <= 200; v++)
            {
                double value = v * 0.9 + 0.123;
                Assert.Equal(BitConverter.DoubleToInt64Bits(Reference(breaks, value, 0, 150)),
                             BitConverter.DoubleToInt64Bits(map.Remap(value)));
            }
        }
    }

    [Fact]
    public void CompressedRange_AgreesBitForBit_WithTheGapsOfTheUnionAddedInTheOrderTheyBegin()
    {
        var random = new Random(11);
        for (int trial = 0; trial < 50; trial++)
        {
            var breaks = new List<AxisBreak>();
            for (int b = 0; b < random.Next(1, 7); b++)
            {
                double from = random.NextDouble() * 140;
                breaks.Add(new AxisBreak(from, from + random.NextDouble() * 20));
            }

            double gap = 0;
            foreach (var b in Union(breaks))
            {
                double lo = Math.Max(b.From, 0), hi = Math.Min(b.To, 150);
                if (hi > lo) gap += hi - lo;
            }

            var (min, max) = new AxisBreakMap(breaks, 0, 150).CompressedRange;
            Assert.Equal(0, min);
            Assert.Equal(BitConverter.DoubleToInt64Bits(150 - gap), BitConverter.DoubleToInt64Bits(max));
        }
    }

    // ---- Breaks that overlap: cut out as their union, so the overlap is taken out once ---------------------------------

    [Fact]
    public void Remap_OverlappingBreaks_AreCutAsTheirUnion()
    {
        // (30, 80) and (50, 100) hide 30..100: 70 taken out, not 50 + 50.
        var map = new AxisBreakMap([new AxisBreak(30, 80), new AxisBreak(50, 100)], 0, 150);

        Assert.Equal(30.0, map.Remap(30), 1e-12);
        Assert.Equal(30.0, map.Remap(100), 1e-12);
        Assert.Equal(50.0, map.Remap(120), 1e-12);
        Assert.Equal(80.0, map.CompressedRange.Max, 1e-12);
        Assert.True(double.IsNaN(map.Remap(80)));
    }

    [Fact]
    public void Cuts_OfOverlappingBreaks_AreOneCutSpanningThemAll()
    {
        var map = new AxisBreakMap([new AxisBreak(50, 100), new AxisBreak(30, 80), new AxisBreak(60, 70)], 0, 150);

        Assert.Equal([new AxisBreakMap.Cut(30, 100, 70)], map.Cuts.ToArray());
        Assert.Equal([30.0], map.Edges.ToArray());
    }

    [Fact]
    public void Cuts_OfBreaksThatOnlyTouch_StayTwoCuts_AndTheValueTheyShareStaysOnTheAxis()
    {
        var map = new AxisBreakMap([new AxisBreak(30, 50), new AxisBreak(50, 80)], 0, 150);

        Assert.Equal(2, map.Cuts.Length);
        Assert.False(map.IsInBreak(50));
        Assert.Equal(30.0, map.Remap(50), 1e-12);
        Assert.Equal(100.0, map.CompressedRange.Max, 1e-12);
    }

    [Fact]
    public void Cuts_OfBreaksBeginningAtOnePlace_AreOneCut_HoweverTheyAreHandedOver()
    {
        var shortFirst = new AxisBreakMap([new AxisBreak(30, 50), new AxisBreak(30, 80), new AxisBreak(30, 30)], 0, 150);
        var longFirst = new AxisBreakMap([new AxisBreak(30, 30), new AxisBreak(30, 80), new AxisBreak(30, 50)], 0, 150);

        Assert.Equal([new AxisBreakMap.Cut(30, 80, 50)], shortFirst.Cuts.ToArray());
        Assert.Equal([new AxisBreakMap.Cut(30, 80, 50)], longFirst.Cuts.ToArray());
    }

    [Fact]
    public void Cuts_LeaveOutABreakThatHidesNothing()
    {
        var map = new AxisBreakMap([new AxisBreak(60, 60), new AxisBreak(90, 70), new AxisBreak(10, 20)], 0, 150);

        Assert.Equal([new AxisBreakMap.Cut(10, 20, 10)], map.Cuts.ToArray());
        Assert.Equal(140.0, map.CompressedRange.Max, 1e-12);
    }

    [Theory]
    [InlineData(AxisScale.Linear, 1.0)]
    [InlineData(AxisScale.Log, 1.0)]
    [InlineData(AxisScale.SymLog, 1.0)]
    [InlineData(AxisScale.SymLog, 10.0)]
    public void Remap_OverlappingBreaks_StaysInOrder_AndInverseReadsEveryPlacedValueBack(AxisScale scale, double linThresh)
    {
        // (10, 300), (100, 2000) and (1000, 1e5) overlap in a chain: one cut, 10..1e5.
        var map = new AxisBreakMap([new AxisBreak(1e3, 1e5), new AxisBreak(10, 300), new AxisBreak(100, 2e3)], 1, 1e6, scale.RuleAt(linThresh));

        double previous = double.NegativeInfinity;
        foreach (double v in new[] { 1, 2, 9.5, 10, 2e5, 1e6 })
        {
            double drawn = map.Remap(v);
            Assert.True(drawn >= previous, $"{v} is drawn at {drawn}, left of the value before it ({previous})");
            previous = drawn;
            Assert.Equal(v, map.Inverse(drawn), v * 1e-12);
        }
        Assert.True(map.IsInBreak(5e3));
        Assert.Equal(map.Remap(10), map.Remap(1e5), 1e-9);
    }

    [Fact]
    public void CompressedRange_OnALogAxis_TakesTheUnionOfOverlappingBreaksOut()
    {
        // (10, 1000) and (100, 1e4) hide 10..1e4: three decades, not two and two.
        var map = new AxisBreakMap([new AxisBreak(10, 1000), new AxisBreak(100, 1e4)], 1, 1e6, ScaleRule.Log);

        Assert.Equal(3.0, map.CompressedRange.Max, 1e-12);
        Assert.Equal(2.0, map.Remap(1e5), 1e-12);
    }

    [Fact]
    public void CompressedRange_OnASymLogAxis_TakesTheUnionOfOverlappingBreaksOut()
    {
        // linthresh 1, s = 1 / 0.9 (matplotlib's linear stretch): forward(-100) = -(s + 2), forward(1000) = s + 3; the union
        // (-100, 1000) removes 2s + 5; forward(-1e4) = -(s + 4), forward(1e4) = s + 4.
        const double s = 1 / 0.9;
        var map = new AxisBreakMap([new AxisBreak(-100, 10), new AxisBreak(1, 1000)], -1e4, 1e4, ScaleRule.SymLog);

        Assert.Equal(-(s + 4), map.CompressedRange.Min, 1e-9);
        Assert.Equal(-s - 1, map.CompressedRange.Max, 1e-9);
        Assert.Equal(-s - 1, map.Remap(1e4), 1e-9);
    }

    // ---- A range handed over high to low (a reversed axis): the same breaks cut from it ------------------------------------

    [Fact]
    public void CompressedRange_OnAReversedRange_TakesTheGapOut_AndStaysReversed()
    {
        var map = new AxisBreakMap([new AxisBreak(30, 80)], 150, 0);

        Assert.Equal(100.0, map.CompressedRange.Min, 1e-12);
        Assert.Equal(0.0, map.CompressedRange.Max, 1e-12);
        Assert.Equal(100.0, map.Remap(150), 1e-12);
        Assert.Equal(map.Remap(30), map.Remap(80), 1e-12);
        Assert.Equal([new AxisBreakMap.Cut(30, 80, 50)], map.Cuts.ToArray());
        Assert.Equal([30.0], map.Edges.ToArray());
        Assert.Equal(120.0, map.Inverse(map.Remap(120)), 1e-12);
    }

    [Fact]
    public void CompressedRange_OnAReversedRange_ClipsABreakToTheRangeByItsLowAndHighEnds()
    {
        // (-50, 20) and (90, 300) on 100..0: only [0, 20] and [90, 100] count.
        var (cMin, cMax) = new AxisBreakMap([new AxisBreak(-50, 20), new AxisBreak(90, 300)], 100, 0).CompressedRange;

        Assert.Equal(70.0, cMin, 1e-12);
        Assert.Equal(0.0, cMax, 1e-12);
    }

    [Fact]
    public void CompressedRange_OnAReversedLogAxis_TakesTheScaledGapOut()
    {
        var map = new AxisBreakMap([new AxisBreak(100, 1000)], 1e6, 1, ScaleRule.Log);

        Assert.Equal(5.0, map.CompressedRange.Min, 1e-12);
        Assert.Equal(0.0, map.CompressedRange.Max, 1e-12);
        Assert.Equal(2.0, map.Remap(1000), 1e-12);
    }

    [Fact]
    public void CompressedRange_OnAReversedSymLogAxis_TakesTheScaledGapOut()
    {
        // linthresh 10, s = 1 / 0.9: forward(100) = 10(s + 1), forward(1000) = 10(s + 2), forward(1e4) = 10(s + 3).
        const double s = 1 / 0.9;
        var map = new AxisBreakMap([new AxisBreak(100, 1000)], 1e4, -1e4, ScaleRule.SymLogAt(10));

        Assert.Equal(10 * (s + 2), map.CompressedRange.Min, 1e-9);
        Assert.Equal(-10 * (s + 3), map.CompressedRange.Max, 1e-9);
    }

    // ---- On a log or symlog axis: the break is cut out of the scaled space -----------------------------------------------

    [Fact]
    public void CompressedRange_OnALogAxis_IsTheScaledRangeLessTheScaledGap()
    {
        var (cMin, cMax) = new AxisBreakMap([new AxisBreak(100, 1000)], 1, 1e6, ScaleRule.Log).CompressedRange;
        Assert.Equal(0.0, cMin, 1e-12);
        Assert.Equal(5.0, cMax, 1e-12); // log10(1e6) - (log10(1000) - log10(100))
    }

    [Theory]
    [InlineData(10.0, 1.0)]
    [InlineData(100.0, 2.0)]
    [InlineData(1000.0, 2.0)]   // the break's end stands where its start stands
    [InlineData(1e4, 3.0)]
    public void Remap_OnALogAxis_ScalesThenTakesOffTheScaledGapBelow(double value, double expected) =>
        Assert.Equal(expected, new AxisBreakMap([new AxisBreak(100, 1000)], 1, 1e6, ScaleRule.Log).Remap(value), 1e-12);

    [Fact]
    public void Remap_OnALogAxis_AValueTheScaleHasNoPlaceFor_IsNaN() =>
        Assert.True(double.IsNaN(new AxisBreakMap([new AxisBreak(100, 1000)], 1, 1e6, ScaleRule.Log).Remap(0)));

    [Fact]
    public void CompressedRange_OnALogAxis_ABreakReachingBelowTheRange_TakesOnlyThePartInsideIt()
    {
        // (-5, 10) on 1..1000: only [1, 10] counts, one decade.
        var (cMin, cMax) = new AxisBreakMap([new AxisBreak(-5, 10)], 1, 1000, ScaleRule.Log).CompressedRange;
        Assert.Equal(0.0, cMin, 1e-12);
        Assert.Equal(2.0, cMax, 1e-12);
    }

    [Fact]
    public void Remap_OnASymLogAxis_UsesItsLinearThreshold()
    {
        // linthresh 10, s = 1 / 0.9: forward(100) = 10(s + 1), forward(1000) = 10(s + 2), forward(1e4) = 10(s + 3); the break
        // (100, 1000) removes 10.
        const double s = 1 / 0.9;
        var map = new AxisBreakMap([new AxisBreak(100, 1000)], -1e4, 1e4, ScaleRule.SymLogAt(10));
        Assert.Equal(10 * (s + 2), map.Remap(1e4), 1e-9);
        Assert.Equal(-10 * (s + 3), map.Remap(-1e4), 1e-9);
        Assert.Equal(10 * (s + 2), map.CompressedRange.Max, 1e-9);
    }

    // ---- Inverse and Cuts --------------------------------------------------

    [Fact]
    public void Inverse_BeforeABreak_IsTheValue_AndPastIt_AddsItsGap()
    {
        var map = new AxisBreakMap([new AxisBreak(80, 120)], 0, 200);

        Assert.Equal(30.0, map.Inverse(30), 1e-12);
        Assert.Equal(150.0, map.Inverse(110), 1e-12);
    }

    [Fact]
    public void Inverse_AtThePlaceABreakIsCut_ReadsWhereTheBreakBegins()
    {
        var map = new AxisBreakMap([new AxisBreak(80, 120)], 0, 200);

        Assert.Equal(map.Remap(80), map.Remap(120), 1e-12);
        Assert.Equal(80.0, map.Inverse(map.Remap(120)), 1e-12);
    }

    [Fact]
    public void Inverse_ABreakBeginningBelowTheRange_TheStartOfTheRangeReadsTheStartOfTheRange()
    {
        // (-30, 20) on 0..100: only [0, 20] is cut, at the start of the axis.
        var map = new AxisBreakMap([new AxisBreak(-30, 20)], 0, 100);

        Assert.Equal(0.0, map.Inverse(0), 1e-12);
        Assert.Equal(60.0, map.Inverse(40), 1e-12);
    }

    [Fact]
    public void Inverse_ABreakWhollyOutsideTheRange_AddsNothing()
    {
        var map = new AxisBreakMap([new AxisBreak(500, 600), new AxisBreak(-40, -10)], 0, 100);

        Assert.Equal(70.0, map.Inverse(70), 1e-12);
    }

    [Theory]
    [InlineData(AxisScale.Linear, 1.0)]
    [InlineData(AxisScale.Log, 1.0)]
    [InlineData(AxisScale.SymLog, 1.0)]
    [InlineData(AxisScale.SymLog, 10.0)]
    public void Inverse_OfEveryPlacedValue_IsTheValue_PastBreaksHandedOverInAnyOrder(AxisScale scale, double linThresh)
    {
        var map = new AxisBreakMap([new AxisBreak(1e4, 1e5), new AxisBreak(10, 100), new AxisBreak(300, 1000)], 1, 1e6, scale.RuleAt(linThresh));

        foreach (double v in new[] { 1, 2, 9.5, 10, 150, 299, 1001, 5000, 2e5, 1e6 })
            Assert.Equal(v, map.Inverse(map.Remap(v)), v * 1e-12);
    }

    [Fact]
    public void Cuts_ListTheBreaksInTheOrderTheyBegin_WithThePlaceEachIsCutAndItsGap()
    {
        var map = new AxisBreakMap([new AxisBreak(100, 120), new AxisBreak(30, 80)], 0, 150);

        Assert.Equal([new AxisBreakMap.Cut(30, 80, 50), new AxisBreakMap.Cut(100, 120, 20)], map.Cuts.ToArray());
        Assert.Equal([30.0, 50.0], map.Edges.ToArray());
    }

    [Fact]
    public void Cuts_OnALogAxis_StandInTheScaledSpace()
    {
        var map = new AxisBreakMap([new AxisBreak(100, 1000)], 1, 1e6, ScaleRule.Log);

        Assert.Equal(2.0, Assert.Single(map.Edges.ToArray()), 1e-12);
        Assert.Equal(1.0, Assert.Single(map.Cuts.ToArray()).Gap, 1e-12);
    }

    [Fact]
    public void Inverse_WithoutAllocating()
    {
        var map = new AxisBreakMap([new AxisBreak(80, 120), new AxisBreak(10, 20)], 0, 200);
        double sum = 0;
        for (int i = 0; i < 100; i++) sum += map.Inverse(i * 1.5); // warm up the JIT

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) sum += map.Inverse(i * 0.15);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.True(sum > 0);
    }

    // ---- Shifted over the whole axis: the move a pan makes -----------------------------------------------------------------

    [Theory]
    [InlineData(1.0, 2.0, 3.0)]
    [InlineData(3.0, 2.0, 5.0)]
    [InlineData(-4.0, -1.0, -5.0)]
    public void Shifted_WithoutBreaks_OnALinearAxis_MovesByTheDelta(double value, double delta, double expected) =>
        Assert.Equal(expected, new AxisMapping(ScaleRule.Linear, []).OverTheWholeAxis().Shifted(value, delta));

    [Theory]
    [InlineData(1.0, 10.0)]
    [InlineData(100.0, 1000.0)]
    public void Shifted_WithoutBreaks_OnALogAxis_MovesByDecades(double value, double expected) =>
        Assert.Equal(expected, new AxisMapping(ScaleRule.Log, []).OverTheWholeAxis().Shifted(value, 1), expected * 1e-12);

    [Fact]
    public void Shifted_OnALogAxis_PastABreakReachingBelowZero_TakesTheBreakOut()
    {
        // (-5, 10) on a log axis hides everything up to 10; 100 moved half a decade up stays 10^2.5.
        var whole = new AxisMapping(ScaleRule.Log, [new AxisBreak(-5, 10)]).OverTheWholeAxis();

        Assert.Equal(Math.Pow(10, 2.5), whole.Shifted(100, 0.5), 1e-9);
    }

    [Fact]
    public void Shifted_AValueInsideABreak_MovesFromWhereTheBreakIsCut()
    {
        var whole = new AxisMapping(ScaleRule.Linear, [new AxisBreak(30, 80)]).OverTheWholeAxis();

        Assert.Equal(90.0, whole.Shifted(40, 10), 1e-12);
        Assert.Equal(20.0, whole.Shifted(40, -10), 1e-12);
    }

    /// <summary>The rule as it stood when every call sorted the breaks again, with the breaks merged into their union first: the
    /// oracle the map is held to.</summary>
    private static double Reference(IReadOnlyList<AxisBreak> breaks, double value, double fullMin, double fullMax)
    {
        var union = Union(breaks);
        foreach (var b in union)
            if (value > b.From && value < b.To) return double.NaN;

        double shift = 0;
        foreach (var b in union)
        {
            if (b.To <= value)
            {
                double lo = Math.Max(b.From, fullMin);
                double hi = Math.Min(b.To, fullMax);
                if (hi > lo) shift += hi - lo;
            }
        }
        return value - shift;
    }

    /// <summary>The breaks that hide something, merged where they overlap, in the order they begin.</summary>
    private static List<AxisBreak> Union(IReadOnlyList<AxisBreak> breaks)
    {
        var union = new List<AxisBreak>();
        foreach (var b in breaks.Where(b => b.To > b.From).OrderBy(b => b.From))
        {
            if (union.Count > 0 && b.From < union[^1].To)
                union[^1] = union[^1] with { To = Math.Max(union[^1].To, b.To) };
            else
                union.Add(b);
        }
        return union;
    }
}
