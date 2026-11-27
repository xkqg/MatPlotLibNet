// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>A walk of ticks that adds its step to the tick before never reaches the end of a range far from zero, where the step is
/// smaller than the distance between two doubles, and drifts by a rounding at every step where it rounds as it goes. Every ladder of
/// ticks is walked from its index (<see cref="ArithmeticTicks"/>): the ticks of a 3-D axis, of a <see cref="MultipleLocator"/> and the
/// minor values between the major ticks end, and a step that is not a positive number walks nothing.</summary>
public class TickWalkStallTests
{
    /// <summary>Runs <paramref name="walk"/> and fails when it has not ended within a few seconds.</summary>
    private static T EndsWithin<T>(Func<T> walk)
    {
        var task = Task.Run(walk);
        Assert.True(task.Wait(TimeSpan.FromSeconds(3)), "the walk never reached the end of its range");
        return task.Result;
    }

    [Fact]
    public void TheTicksOfA3DAxisFarFromZero_End()
    {
        // At 1e16 the doubles stand 2 apart; the cube's step over a width of 4 is 0.5.
        double[] ticks = EndsWithin(() =>
            new TickRequest(new Range1D(1e16, 1e16 + 4), 400, 10, AlongX: true, Theme.Default, TickAxisKind.Cube).Majors());

        Assert.Equal([1e16, 1e16 + 2, 1e16 + 4], ticks);
    }

    [Fact]
    public void TheTicksOfAMultipleLocatorFarFromZero_End()
    {
        double[] ticks = EndsWithin(() => new MultipleLocator(0.5).Locate(1e17, 1e17 + 64));

        Assert.Equal([1e17, 1e17 + 16, 1e17 + 32, 1e17 + 48, 1e17 + 64], ticks);
    }

    [Fact]
    public void AMultipleLocatorWithABaseBelowZero_PlacesNoTick()
    {
        Assert.Empty(EndsWithin(() => new MultipleLocator(-1).Locate(0, 3)));
    }

    [Fact]
    public void TheMinorValuesFarFromZero_End()
    {
        var values = EndsWithin(() =>
        {
            var walked = new List<double>();
            foreach (double value in new MinorWalk([1e17, 1e17 + 64], 0.5)) walked.Add(value);
            return walked;
        });

        Assert.Equal([1e17 - 64, 1e17 - 48, 1e17 - 32, 1e17 - 16, 1e17 + 16, 1e17 + 32, 1e17 + 48, 1e17 + 80, 1e17 + 96, 1e17 + 112, 1e17 + 128], values);
    }

    [Fact]
    public void TheMinorValues_DoNotDriftByARoundingAtEveryStep()
    {
        // Ten-second minor ticks over a day of dates: a walk that rounds each value to ten decimals and adds the step to the rounded
        // value moves every value by up to half of 1e-10 at each step, which adds up over thousands of steps.
        double[] majors = [19000.0, 19000.5, 19001.0];
        double step = 10.0 / 86400.0;
        double first = majors[0] - (majors[1] - majors[0]);

        var walked = new List<double>();
        foreach (double value in new MinorWalk(majors, step)) walked.Add(value);

        var expected = new List<double>();
        for (long i = 0; ; i++)
        {
            double value = Math.Round(first + i * step, 10);
            if (!(first + i * step <= majors[^1] + (majors[1] - majors[0]) + step * 0.01)) break;
            if (Array.Exists(majors, major => Math.Abs(major - value) < step * 0.01)) continue;
            expected.Add(value);
        }

        Assert.Equal(expected, walked);
    }
}
