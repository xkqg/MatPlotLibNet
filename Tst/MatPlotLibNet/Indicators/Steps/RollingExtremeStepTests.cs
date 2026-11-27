// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Tests.Indicators.Steps;

/// <summary>The highest and the lowest of the last <c>period</c> values, one value at a time: the one window-extreme walk the batch
/// <c>RollingMax</c> and <c>RollingMin</c> fold and the streaming stochastic and Williams %R take a value at a time.</summary>
public sealed class RollingExtremeStepTests
{
    private static double[] Walk(int count, int seed)
    {
        var random = new Random(seed);
        var values = new double[count];
        double previous = 50;
        for (int i = 0; i < count; i++) values[i] = previous = Math.Round(previous + (random.NextDouble() - 0.5) * 8, 2);
        return values;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(14)]
    public void TheHighest_IsTheHighestOfTheLastPeriodValues_AfterEveryValue(int period)
    {
        var values = Walk(200, seed: period);
        var step = new RollingExtremeStep<KeepHighest>(period);

        for (int i = 0; i < values.Length; i++)
        {
            double expected = values[Math.Max(0, i - period + 1)..(i + 1)].Max();
            Assert.Equal(expected, step.Next(values[i]));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(14)]
    public void TheLowest_IsTheLowestOfTheLastPeriodValues_AfterEveryValue(int period)
    {
        var values = Walk(200, seed: 100 + period);
        var step = new RollingExtremeStep<KeepLowest>(period);

        for (int i = 0; i < values.Length; i++)
        {
            double expected = values[Math.Max(0, i - period + 1)..(i + 1)].Min();
            Assert.Equal(expected, step.Next(values[i]));
        }
    }

    [Fact]
    public void AValueThatHasLeftTheWindow_IsNoLongerTheExtreme()
    {
        var step = new RollingExtremeStep<KeepHighest>(3);
        var highest = new List<double>();

        foreach (double value in new double[] { 5, 4, 3, 2, 1 }) highest.Add(step.Next(value));

        Assert.Equal([5.0, 5.0, 5.0, 4.0, 3.0], highest);
    }

    [Fact]
    public void ARunOfEqualValues_KeepsTheWindowFullWithoutGrowingIt()
    {
        var step = new RollingExtremeStep<KeepLowest>(2);
        var lowest = new List<double>();

        foreach (double value in new double[] { 2, 2, 2, 2, 1, 2, 2 }) lowest.Add(step.Next(value));

        Assert.Equal([2.0, 2.0, 2.0, 2.0, 1.0, 1.0, 2.0], lowest);
    }

    [Fact]
    public void BeforeAFullWindow_TheExtreme_IsOfTheValuesThatHaveCome()
    {
        var step = new RollingExtremeStep<KeepHighest>(4);

        Assert.Equal(3.0, step.Next(3));
        Assert.Equal(3.0, step.Next(1));
        Assert.Equal(7.0, step.Next(7));
    }
}
