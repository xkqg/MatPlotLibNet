// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace MatPlotLibNet.Benchmarks.Drag;

/// <summary>The processor clock of a process, which moves in whole steps: finds the size of its step by spinning, and lets a
/// run begin on a step so that it counts whole steps from its first frame. Every wait has a deadline.</summary>
/// <param name="cpuTime">Reads the processor time of the process.</param>
/// <param name="deadline">How long to spin for one step before the clock is taken to be stopped; ten seconds when not given.</param>
public sealed class ProcessClock(Func<TimeSpan> cpuTime, TimeSpan? deadline = null)
{
    private const int StepsToObserve = 4;
    private readonly TimeSpan _deadline = deadline ?? TimeSpan.FromSeconds(10);

    /// <summary>The processor time of the process now.</summary>
    /// <returns>The reading, in whole steps of the clock.</returns>
    public TimeSpan Read() => cpuTime();

    /// <summary>The size of one step of the clock, the smallest of the steps seen while the process spun over four of them.</summary>
    /// <returns>The tick.</returns>
    /// <exception cref="InvalidOperationException">The clock did not step within the deadline.</exception>
    public TimeSpan Tick()
    {
        var previous = AlignToStep();
        var smallest = TimeSpan.MaxValue;
        for (var i = 1; i < StepsToObserve; i++)
        {
            var next = AlignToStep();
            smallest = TimeSpan.FromTicks(Math.Min(smallest.Ticks, (next - previous).Ticks));
            previous = next;
        }
        return smallest;
    }

    /// <summary>Spins until the clock has just stepped.</summary>
    /// <returns>The reading on that step, so that what is measured from it holds no part of a step that began earlier.</returns>
    /// <exception cref="InvalidOperationException">The clock did not step within the deadline.</exception>
    public TimeSpan AlignToStep()
    {
        var first = cpuTime();
        var started = Stopwatch.GetTimestamp();
        while (true)
        {
            var now = cpuTime();
            if (now != first) return now;
            if (Stopwatch.GetElapsedTime(started) > _deadline)
                throw new InvalidOperationException($"The processor clock did not step in {_deadline.TotalSeconds:F1} s of spinning.");
        }
    }
}
