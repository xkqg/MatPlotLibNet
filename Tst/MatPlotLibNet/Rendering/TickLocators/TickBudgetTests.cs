// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>How many ticks the room along an axis asks for (<see cref="TickBudget"/>): matplotlib's <c>np.clip(get_tick_space(), fewest,
/// most)</c>, one rule for every locator that reads the room. A room that is no number is no axis to ask, and a label of no size takes
/// no room: both ask for the most (<see cref="TickSpaceTheoryTests"/>).</summary>
public class TickBudgetTests
{
    [Theory]
    [InlineData(5.0, 5)]
    [InlineData(5.9, 5)]
    [InlineData(0.0, 2)]
    [InlineData(-3.0, 2)]
    [InlineData(9.0, 9)]
    [InlineData(40.0, 9)]
    [InlineData(double.NaN, 9)]
    [InlineData(double.PositiveInfinity, 9)]
    public void TheRoom_IsHeldBetweenTheFewestAndTheMost_AndNoNumberAsksForTheMost(double tickSpace, int expected)
    {
        Assert.Equal(expected, TickBudget.Standard.Clamp(tickSpace));
    }

    [Fact]
    public void TheStandardBudget_IsTwoToNine()
    {
        // matplotlib 3.11.2: LogLocator.tick_values' n_request = np.clip(self.axis.get_tick_space(), 2, 9), and 9 with no axis.
        Assert.Equal(new TickBudget(2, 9), TickBudget.Standard);
    }

    [Fact]
    public void ALogitAxis_AsksForTwoAtLeast_AndNoMost()
    {
        // LogitLocator.tick_values: nbins = self.axis.get_tick_space(); if nbins < 2: nbins = 2 (no clip above).
        Assert.Equal(2, TickBudget.Logit.Clamp(1));
        Assert.Equal(1000, TickBudget.Logit.Clamp(1000));
        Assert.Equal(int.MaxValue, TickBudget.Logit.Clamp(double.NaN));
    }
}
