// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>The one walk of a regular ladder of ticks (<see cref="ArithmeticTicks"/>): each tick worked out from its index, rounded to
/// ten decimals, a tick that rounds onto the one before it left out, the walk ended past its limit or at a value that is no number, and
/// nothing walked for a step that is not a positive number.</summary>
public class ArithmeticTicksTests
{
    [Fact]
    public void TheTicks_AreTheFirstAndItsMultiples_UpToTheLimit_RoundedToTenDecimals()
    {
        Assert.Equal([0.0, 0.1, 0.2, 0.3, 0.4], new ArithmeticTicks(0, 0.1, 0.4).ToArray());
    }

    [Fact]
    public void EachTick_IsWorkedOutFromItsIndex_NotAddedToTheOneBefore()
    {
        // A sum of steps drifts in its last bits (seven additions of 0.1 give 0.7999999999999999); every tick is first + i * step, rounded.
        double[] ticks = new ArithmeticTicks(0.1, 0.1, 1.0).ToArray();

        for (int i = 0; i < ticks.Length; i++) Assert.Equal(Math.Round(0.1 + i * 0.1, 10), ticks[i]);
    }

    [Fact]
    public void ATickThatRoundsOntoTheOneBefore_IsLeftOut()
    {
        // At 1e17 the doubles stand 16 apart: a step of 0.5 lands every tick on the one before until a whole 16 is passed.
        double[] ticks = new ArithmeticTicks(1e17, 0.5, 1e17 + 64).ToArray();

        Assert.Equal([1e17, 1e17 + 16, 1e17 + 32, 1e17 + 48, 1e17 + 64], ticks);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    public void AStepThatIsNotAPositiveNumber_WalksNothing(double step)
    {
        Assert.Empty(new ArithmeticTicks(0, step, 10).ToArray());
    }

    [Fact]
    public void AFirstTickThatIsNoNumber_WalksNothing()
    {
        Assert.Empty(new ArithmeticTicks(double.NaN, 1, 10).ToArray());
    }

    [Fact]
    public void AFirstTickPastTheLimit_WalksNothing()
    {
        Assert.Empty(new ArithmeticTicks(11, 1, 10).ToArray());
    }

    [Fact]
    public void Over_StartsAtTheFirstMultipleAtOrAboveTheLowEnd_AndReachesAHundredthOfAStepPastTheHighEnd()
    {
        Assert.Equal([0.5, 1.0, 1.5, 2.0], ArithmeticTicks.Over(0.3, 1.996, 0.5).ToArray());
        Assert.Equal([0.5, 1.0, 1.5], ArithmeticTicks.Over(0.3, 1.99, 0.5).ToArray());
    }

    [Fact]
    public void Over_KeepsATickAHairInsideTheLowEnd()
    {
        // 0.30000000000000004 is 0.3 to the last bit of a sum: the tick 0.3 stands at the low end, as matplotlib's _Edge_integer keeps it.
        Assert.Equal(0.3, ArithmeticTicks.Over(0.30000000000000004, 1, 0.1).ToArray()[0]);
    }

    [Fact]
    public void Multiples_AreShiftedByTheOffset_FromTheFirstAtOrAboveToTheLastAtOrBelow()
    {
        Assert.Equal([0.25, 0.75, 1.25], ArithmeticTicks.Multiples(0, 1.25, 0.5, 0.25).ToArray());
        Assert.Equal([0.25, 0.75], ArithmeticTicks.Multiples(0, 1.2499, 0.5, 0.25).ToArray());
    }

    [Fact]
    public void Holding_IsTheMultiplesAtOrBeyondTheEnds_RoundedAsATickIs()
    {
        Assert.Equal(new MinMaxRange(0.2, 1.2), ArithmeticTicks.Holding(0.25, 1.15, 0.2));
        Assert.Equal(new MinMaxRange(-1, 3), ArithmeticTicks.Holding(-0.5, 2.5, 1));
    }

    [Fact]
    public void WalkingAllocatesNothing()
    {
        var ladder = new ArithmeticTicks(0, 0.25, 20);
        double sum = 0;
        foreach (double tick in ladder) sum += tick; // warm the code

        long before = GC.GetAllocatedBytesForCurrentThread();
        foreach (double tick in ladder) sum += tick;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.True(sum > 0);
    }

    [Fact]
    public void TheArray_HoldsExactlyTheTicksTheWalkGives()
    {
        var ladder = new ArithmeticTicks(-1, 0.3, 2);
        var walked = new List<double>();
        foreach (double tick in ladder) walked.Add(tick);

        Assert.Equal(walked, ladder.ToArray());
    }
}
