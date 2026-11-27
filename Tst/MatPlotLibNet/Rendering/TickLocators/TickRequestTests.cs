// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>The one question every kind of axis asks for its major ticks (<see cref="TickRequest"/>): a locator it was given, told how
/// many labels fit; else its spacing; else the theme's ladder; else the library's own placement for that kind of axis.</summary>
public class TickRequestTests
{
    /// <summary>A request over <paramref name="span"/> along <paramref name="lengthPx"/> with labels of 10 px, along an X axis (three
    /// label sizes a tick).</summary>
    private static TickRequest Along(Range1D span, double lengthPx, Theme theme, TickAxisKind? kind = null) =>
        new(span, lengthPx, 10, AlongX: true, theme, kind ?? TickAxisKind.Plot);

    [Fact]
    public void TheTickSpace_IsHowManyLabelsFitAlongTheLength() =>
        Assert.Equal(13, Along(new Range1D(0, 10), 400, Theme.Default).TickSpace);

    // ── a locator the axis was given ──

    [Fact]
    public void AGivenLocator_WinsOverTheSpacing_AndOverTheTheme()
    {
        var request = Along(new Range1D(0, 3), 400, Theme.MatplotlibV2) with { Given = new FixedLocator([0.25, 2.75]), Spacing = 1.5 };

        Assert.Equal([0.25, 2.75], request.Majors());
    }

    [Fact]
    public void AGivenLocator_IsAskedForTheIntervalWhicheverWayTheAxisRuns()
    {
        var request = Along(new Range1D(3, 0), 400, Theme.Default) with { Given = new MultipleLocator(1) };

        Assert.Equal([0.0, 1, 2, 3], request.Majors());
    }

    /// <summary>matplotlib 3.11.2's <c>LogLocator(numticks=4).tick_values(1, 1e12)</c> in view: 1, 1e4, 1e8, 1e12. 120 px over three
    /// label sizes of 10 px leave room for four.</summary>
    [Fact]
    public void AGivenLocatorWhoseTicksFollowTheRoom_IsToldTheTickSpace()
    {
        var request = Along(new Range1D(1, 1e12), 120, Theme.MatplotlibV2) with { Given = new LogLocator() };

        Assert.Equal([1.0, 1e4, 1e8, 1e12], request.Majors());
    }

    // ── a spacing ──

    [Fact]
    public void ASpacing_PlacesTheTicksAtItsMultiples_UnderAnyTheme()
    {
        Assert.Equal([0.0, 1.5, 3.0], (Along(new Range1D(0, 3), 400, Theme.Default) with { Spacing = 1.5 }).Majors());
        Assert.Equal([0.0, 1.5, 3.0], (Along(new Range1D(3, 0), 400, Theme.MatplotlibClassic, TickAxisKind.Cube) with { Spacing = 1.5 }).Majors());
    }

    // ── the theme's ladder ──

    /// <summary>matplotlib 3.11.2's <c>MaxNLocator(nbins=9, steps=[1, 2, 2.5, 5, 10]).tick_values(0, 10)</c> is 0, 2, ..., 10, and with
    /// two bins 0, 5, 10: the bins follow how many labels fit (400 px leave room for 13, held at 9; 60 px for 2).</summary>
    [Fact]
    public void TheDefaultStylesLadder_IsLaidWithTheBinsTheLengthHolds()
    {
        Assert.Equal([0.0, 2, 4, 6, 8, 10], Along(new Range1D(0, 10), 400, Theme.MatplotlibV2).Majors());
        Assert.Equal([0.0, 5, 10], Along(new Range1D(0, 10), 60, Theme.MatplotlibV2).Majors());
    }

    /// <summary>matplotlib 3.11.2's classic <c>MaxNLocator(nbins=9, steps=[1, 2, 5, 10]).tick_values(0.95, 9.95)</c> in view is 1 to 9
    /// under <c>axes.autolimit_mode = 'data'</c> and 2, 4, 6, 8 under <c>'round_numbers'</c>, which the classic style sets: the step must
    /// reach the end of the view from a multiple of itself.</summary>
    [Fact]
    public void TheClassicLadder_RoundsNumbers()
    {
        Assert.Equal([2.0, 4, 6, 8], Along(new Range1D(0.95, 9.95), 400, Theme.MatplotlibClassic).Majors());
        Assert.Equal([1.0, 2, 3, 4, 5, 6, 7, 8, 9], TickLadder.Classic.TicksIn(new Range1D(0.95, 9.95), roundNumbers: false));
    }

    /// <summary>A 3-D axis keeps the whole run of the ladder, a tick past each end included (axis3d formats every value its locator
    /// gives before it keeps the ones in view), where a 2-D axis keeps the ones in view.</summary>
    [Fact]
    public void TheCube_KeepsTheLaddersWholeRun_WhereAPlotKeepsTheTicksInView()
    {
        double[] cube = Along(new Range1D(0.05, 0.95), 400, Theme.MatplotlibV2, TickAxisKind.Cube).Majors();
        double[] plot = Along(new Range1D(0.05, 0.95), 400, Theme.MatplotlibV2).Majors();

        Assert.Equal(11, cube.Length);
        Assert.Equal(0, cube[0], 12);
        Assert.Equal(1, cube[^1], 12);
        Assert.Equal(9, plot.Length);
        Assert.All(plot, tick => Assert.InRange(tick, 0.05, 0.95));
    }

    // ── the library's own placement, a value of the kind of axis ──

    [Fact]
    public void APlot_IsPlacedByTheLibrarysNearestStep_EightIntervalsFrom240Pixels()
    {
        Assert.Equal([0.0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10], Along(new Range1D(0, 10), 400, Theme.Default).Majors());
        Assert.Equal([0.0, 5, 10], Along(new Range1D(0, 10), 60, Theme.Default).Majors());
    }

    /// <summary>A width of 2.98 gives a step of 0.5, 1.83 a step of 0.2 (eleven intervals accepted), 6.25 a step of 1; the values run
    /// from the first step at or above the low end to the high end, whatever the width of the axes.</summary>
    [Theory]
    [InlineData(-0.4896, 2.4896, new[] { 0.0, 0.5, 1.0, 1.5, 2.0 })]
    [InlineData(-0.4167, 1.4167, new[] { -0.4, -0.2, 0.0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.2, 1.4 })]
    [InlineData(-0.125, 6.125, new[] { 0.0, 1, 2, 3, 4, 5, 6 })]
    public void ACube_IsPlacedByTheMaxNLadderOverTenIntervals(double lo, double hi, double[] expected)
    {
        Assert.Equal(expected, Along(new Range1D(lo, hi), 400, Theme.Default, TickAxisKind.Cube).Majors());
        Assert.Equal(expected, Along(new Range1D(hi, lo), 40, Theme.Default, TickAxisKind.Cube).Majors());
    }

    /// <summary>A ruler (a colour bar, the rings of a polar axes) asks for as many ticks as fit, two to nine, and names only the ones
    /// inside its range: over 0 to 9.99 the step of 2 reaches 10, which a plot keeps and a ruler leaves out.</summary>
    [Fact]
    public void ARuler_AsksForTheTicksThatFit_AndKeepsTheOnesInsideItsRange()
    {
        var span = new Range1D(0, 9.99);

        Assert.Equal([0.0, 2, 4, 6, 8], new TickRequest(span, 340, 13, AlongX: false, Theme.Default, TickAxisKind.Ruler).Majors());
        Assert.Equal([0.0, 2, 4, 6, 8, 10], new MaxNLocator(9).Locate(0, 9.99));
    }

    /// <summary>The rings of a polar axes under the library's own themes are a ruler along the radius, while the room a given locator
    /// and the theme's ladder count is along the circle's height (<see cref="TickRequest.LibraryLengthPx"/>).</summary>
    [Fact]
    public void ARulerSizedToAShorterLength_AsksForTheTicksThatFitAlongIt()
    {
        var span = new Range1D(0, 9.99);
        var height = new TickRequest(span, 104, 13, AlongX: false, Theme.Default, TickAxisKind.Ruler);

        Assert.Equal([0.0, 5], height.Majors());
        Assert.Equal([0.0], (height with { LibraryLengthPx = 52 }).Majors());
        Assert.Equal(104, height.LibraryLengthPx);
    }

    [Fact]
    public void ARulerWithLabelsOfNoSize_AsksForTheMostTicks() =>
        // A label of no size takes no room (matplotlib's get_tick_space: 2**31 - 1), so the ruler asks for nine: MaxNLocator(9) over 0..10.
        Assert.Equal([0.0, 2, 4, 6, 8, 10], new TickRequest(new Range1D(0, 10), 100, 0, AlongX: false, Theme.Default, TickAxisKind.Ruler).Majors());

    // ── every tick list an axis of any length can be given ──

    [Fact]
    public void AtAnyLength_TheDefaultStylesLadder_IsLaidWithEveryCountOfBins()
    {
        var span = new Range1D(0, 10);
        var lists = Along(span, 400, Theme.MatplotlibV2).MajorsAtAnyLength().ToArray();

        Assert.Equal(9, lists.Length);
        for (int bins = 1; bins <= 9; bins++)
            Assert.Equal((TickLadder.DefaultStyle with { Bins = bins }).TicksIn(span, roundNumbers: false), lists[bins - 1]);
    }

    [Fact]
    public void AtAnyLength_ALadderWhoseBinsDoNotFollowTheLength_GivesOneList()
    {
        var request = Along(new Range1D(0.95, 9.95), 400, Theme.MatplotlibClassic);

        Assert.Equal([request.Majors()], request.MajorsAtAnyLength());
    }

    [Fact]
    public void AtAnyLength_ARuler_AsksForTwoToNineTicks()
    {
        var span = new Range1D(0, 9.99);
        var lists = new TickRequest(span, 340, 13, AlongX: false, Theme.Default, TickAxisKind.Ruler).MajorsAtAnyLength().ToArray();

        Assert.Equal(8, lists.Length);
        for (int most = 2; most <= 9; most++)
            Assert.Equal(new MaxNLocator(most).Locate(0, 9.99).Where(tick => tick <= 9.99), lists[most - 2]);
    }

    [Fact]
    public void AtAnyLength_AGivenLocatorWhoseTicksFollowTheRoom_IsToldTwoToNineLabels()
    {
        var span = new Range1D(1, 1e12);
        var given = new LogLocator();
        var lists = (Along(span, 120, Theme.MatplotlibV2) with { Given = given }).MajorsAtAnyLength().ToArray();

        Assert.Equal(8, lists.Length);
        for (int space = 2; space <= 9; space++)
            Assert.Equal(given.LocateAlong(1, 1e12, space, Theme.MatplotlibV2), lists[space - 2]);
    }

    [Fact]
    public void AtAnyLength_EveryOtherRequest_GivesItsOneList()
    {
        var span = new Range1D(0, 10);
        TickRequest[] requests =
        [
            Along(span, 400, Theme.Default) with { Given = new MultipleLocator(2) },
            Along(span, 400, Theme.MatplotlibV2) with { Spacing = 2.5 },
            Along(span, 400, Theme.Default),
            Along(span, 400, Theme.Default, TickAxisKind.Cube),
        ];

        foreach (var request in requests) Assert.Equal([request.Majors()], request.MajorsAtAnyLength());
    }

    /// <summary>The cube's placement as it was written with a list that grew and was copied into the array: the arithmetic the
    /// array-sized walk must keep bit for bit.</summary>
    private static double[] ListedCubeTicks(double lo, double hi)
    {
        double range = hi - lo;
        const int tickSpace = 10;
        ReadOnlySpan<double> stepLadder = [1.0, 2.0, 2.5, 5.0, 10.0];
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(range / tickSpace)));
        double chosenStep = 10 * magnitude;
        foreach (double s in stepLadder)
        {
            double step = s * magnitude;
            double niceLo = Math.Floor(lo / step + 1e-9) * step;
            double niceHi = Math.Ceiling(hi / step - 1e-9) * step;
            int count = (int)Math.Round((niceHi - niceLo) / step);
            if (count <= tickSpace + 1 && count >= 2)
            {
                chosenStep = step;
                break;
            }
        }

        double first = Math.Ceiling(lo / chosenStep - 1e-9) * chosenStep;
        var ticks = new List<double>();
        for (long i = 0; first + i * chosenStep <= hi + chosenStep * 0.01; i++)
            ticks.Add(Math.Round(first + i * chosenStep, 10));
        return [.. ticks];
    }

    /// <summary>Held bit for bit against a loop over the index that grows a list, over ranges below, on and above zero, from the
    /// thousandth of a unit to billions, and over the widths that pick each step of the ladder. (A walk that added its step stalled far
    /// from zero and started a range that rounds up to zero at -0.0, where the index starts at +0.0.)</summary>
    [Fact]
    public void ACube_GivesTheTicksOfTheIndexedWalk()
    {
        double[] starts = [-250, -12.5, -0.4896, -0.05, 0, 0.3, 1, 17.7, 1e6, 123456.789, -1e9];
        double[] widths = [0.001, 0.0123, 0.4, 1.83, 2.98, 3.7, 6.25, 10, 37, 100, 4321, 1e5, 1e9];
        int compared = 0;
        foreach (double lo in starts)
            foreach (double width in widths)
            {
                if (width < 1e-8 * Math.Abs(lo)) continue;   // a step below the spacing of doubles repeats ticks the walk leaves out (TickWalkStallTests)
                double hi = lo + width;
                double[] expected = ListedCubeTicks(lo, hi);
                double[] actual = Along(new Range1D(lo, hi), 400, Theme.Default, TickAxisKind.Cube).Majors();
                Assert.Equal(expected.Length, actual.Length);
                for (int i = 0; i < expected.Length; i++)
                    Assert.Equal(BitConverter.DoubleToInt64Bits(expected[i]), BitConverter.DoubleToInt64Bits(actual[i]));
                compared++;
            }

        Assert.True(compared > 100, $"{compared} ranges compared");
    }

    /// <summary>A 3-D axis asks for its ticks in several passes of every draw, so a call allocates the array it returns and nothing
    /// else: no list that grows, no copy of it.</summary>
    [Theory]
    [InlineData(0.0, 3.0)]
    [InlineData(-0.4896, 2.4896)]
    [InlineData(-0.125, 6.125)]
    [InlineData(0.0, 1e5)]
    public void ACube_AllocatesTheArrayItReturnsAndNothingElse(double lo, double hi)
    {
        var request = Along(new Range1D(lo, hi), 400, Theme.Default, TickAxisKind.Cube);
        request.Majors();   // the first call pays the compiler's tiering

        long before = GC.GetAllocatedBytesForCurrentThread();
        double[] ticks = request.Majors();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated <= 24 + 8L * ticks.Length, $"{allocated} bytes for {ticks.Length} ticks (an array of them is {24 + 8L * ticks.Length})");
    }
}
