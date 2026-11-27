// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Every count a budget asks for, from its fewest to its most (<see cref="TickBudget"/>'s enumerator): the one walk a layout
/// that keeps room before it knows the length (a colour bar's widest label, a log locator's labels at any length) takes.</summary>
public class TickBudgetCountsTests
{
    [Fact]
    public void TheStandardBudget_CountsFromTwoToNine()
    {
        var counts = new List<int>();
        foreach (int count in TickBudget.Standard) counts.Add(count);

        Assert.Equal([2, 3, 4, 5, 6, 7, 8, 9], counts);
    }

    [Fact]
    public void ALaddersBudget_CountsItsBinsFromTheFewestALengthLeaves()
    {
        var counts = new List<int>();
        foreach (int bins in TickLadder.DefaultStyle.Budget) counts.Add(bins);

        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], counts);
    }
}
