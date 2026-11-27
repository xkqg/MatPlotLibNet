// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>What each kind of axis does where neither the axis nor the theme places its ticks (<see cref="TickAxisKind"/>): one leaf per
/// kind, so a request asks its kind and never compares it.</summary>
public class TickAxisKindTests
{
    private static TickRequest Over(Range1D span, TickAxisKind kind, bool alongX = true, double lengthPx = 400, double labelPx = 10) =>
        new(span, lengthPx, labelPx, alongX, Theme.Default, kind);

    [Fact]
    public void APlotAxis_IsPlacedByTheLibrarysLocatorForItsLength()
    {
        var request = Over(new Range1D(0, 10), TickAxisKind.Plot, lengthPx: 120);

        Assert.Equal(AutoLocator.ForLength(120).Locate(0, 10), TickAxisKind.Plot.LibraryMajors(request));
    }

    /// <summary>A width of 1.83 over at most ten intervals (eleven accepted, as matplotlib's <c>MaxNLocator</c> accepts) takes the step
    /// 0.2 of the default style's steps 1, 2, 2.5, 5 and 10.</summary>
    [Fact]
    public void ACubeAxis_StepsByTheStepsOfTheDefaultStyle()
    {
        double[] ticks = TickAxisKind.Cube.LibraryMajors(Over(new Range1D(0, 1.83), TickAxisKind.Cube));

        Assert.Equal([0.0, 0.2, 0.4, 0.6, 0.8, 1.0, 1.2, 1.4, 1.6, 1.8], ticks);
    }

    [Fact]
    public void ARuler_NamesAsManyTicksAsFit_InsideItsRange()
    {
        var request = Over(new Range1D(0, 10), TickAxisKind.Ruler, alongX: false, lengthPx: 340, labelPx: 13);

        Assert.Equal([0.0, 2, 4, 6, 8, 10], TickAxisKind.Ruler.LibraryMajors(request));
    }

    [Fact]
    public void ACubeAxis_KeepsTheWholeRunOfALadder_EveryOtherKindTheTicksInView()
    {
        var span = new Range1D(0.05, 0.95);
        var ladder = TickLadder.DefaultStyle;

        Assert.Equal(ladder.TicksOver(span, onThreeDAxis: true, roundNumbers: false), TickAxisKind.Cube.Laid(ladder, span, roundNumbers: false));
        Assert.Equal(ladder.TicksIn(span, roundNumbers: false), TickAxisKind.Plot.Laid(ladder, span, roundNumbers: false));
        Assert.Equal(ladder.TicksIn(span, roundNumbers: true), TickAxisKind.Ruler.Laid(ladder, span, roundNumbers: true));
    }

    [Fact]
    public void ARuler_NamesEveryCountFromTwoToNine_AtAnyLength_EveryOtherKindItsOneList()
    {
        var span = new Range1D(0, 10);
        var ruler = Over(span, TickAxisKind.Ruler, alongX: false);
        var plot = Over(span, TickAxisKind.Plot);

        double[][] lists = [.. TickAxisKind.Ruler.LibraryMajorsAtAnyLength(ruler)];
        Assert.Equal(8, lists.Length);
        Assert.Equal(new MaxNLocator(2).Locate(0, 10), lists[0]);
        Assert.Equal([TickAxisKind.Plot.LibraryMajors(plot)], TickAxisKind.Plot.LibraryMajorsAtAnyLength(plot));
    }
}
