// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The levels a contour lays over a scale (<see cref="ScaleRule.LevelsOver"/>): matplotlib 3.11.2's
/// <c>ContourSet._ensure_locator_exists</c> takes a <c>LogLocator(numticks=N)</c> for a log-scaled contour and a
/// <c>MaxNLocator(N + 1, min_n_ticks=1)</c> otherwise, so the run of levels is a member of the scale, not a flag of the caller.</summary>
public class ScaleRuleLevelsTests
{
    [Fact]
    public void ALogScale_LaysTheDecadesOfTheLogLocator()
    {
        Assert.Equal(LogLocator.DecadesLaid(1, 1000, 3, classic: false).Powers(), ScaleRule.Log.LevelsOver(1, 1000, 3, roundNumbers: false));
    }

    [Fact]
    public void EveryOtherScale_LaysTheRunOfTheLevelsLadder()
    {
        var run = SteppedTicks.Over(-0.6849773668989229, 1.0560418679341015, TickLadder.ForLevels(10), roundNumbers: true);
        double[] expected = [.. Enumerable.Range(0, run.Count).Select(i => run[i])];

        var range = new Range1D(-0.6849773668989229, 1.0560418679341015).NonsingularBy(expander: 1e-13, tiny: 1e-14);
        Assert.Equal(expected, ScaleRule.Linear.LevelsOver(range.Lo, range.Hi, 10, roundNumbers: true));
        Assert.Equal(expected, ScaleRule.SymLog.LevelsOver(range.Lo, range.Hi, 10, roundNumbers: true));
    }
}
