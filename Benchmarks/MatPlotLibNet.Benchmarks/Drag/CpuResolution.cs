// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Benchmarks.Drag;

/// <summary>When the processor time of a process, which its clock reports in whole steps of the scheduler's tick (15.625 ms on
/// Windows), is large enough to say what a frame costs: when one step is no more than <see cref="MaxRelativeError"/> of it.
/// A run of 300 frames of 0.85 ms is sixteen steps or seventeen, a difference of 6 %, and printed as a regression that was not
/// one.</summary>
/// <param name="Tick">The step of the processor clock.</param>
/// <param name="MaxRelativeError">The share of the measured time that one step may be.</param>
public readonly record struct CpuResolution(TimeSpan Tick, double MaxRelativeError)
{
    /// <summary>The rule at a hundredth: a reading is good to one step in a hundred.</summary>
    /// <param name="tick">The step of the processor clock.</param>
    /// <returns>The rule.</returns>
    public static CpuResolution Of(TimeSpan tick) => new(tick, 0.01);

    /// <summary>The least processor time that resolves a frame.</summary>
    public TimeSpan MinimumCpu => TimeSpan.FromTicks((long)Math.Ceiling(Tick.Ticks / MaxRelativeError));

    /// <summary>Whether <paramref name="cpu"/> is enough.</summary>
    /// <param name="cpu">The processor time a run measured.</param>
    /// <returns>True when one step is within the error this rule allows.</returns>
    public bool Resolves(TimeSpan cpu) => cpu >= MinimumCpu;

    /// <summary>How far a reading can be off, as a share of itself: one step over the time read.</summary>
    /// <param name="cpu">The processor time a run measured.</param>
    /// <returns>The share, 1 when nothing was read.</returns>
    public double ErrorOf(TimeSpan cpu) => cpu <= TimeSpan.Zero ? 1.0 : Math.Min(1.0, Tick.TotalMilliseconds / cpu.TotalMilliseconds);

    /// <summary>How many frames more a run needs, at the rate it has measured so far, to reach <see cref="MinimumCpu"/>, with a
    /// twentieth to spare so that the next reading does not fall just short.</summary>
    /// <param name="cpu">The processor time the frames so far cost.</param>
    /// <param name="framesDone">How many frames they were.</param>
    /// <returns>0 when the run resolves already; as many frames as were drawn when no time was read, because there is no rate yet.</returns>
    public int ExtraFramesNeeded(TimeSpan cpu, int framesDone)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(framesDone, 1);
        if (Resolves(cpu)) return 0;
        if (cpu <= TimeSpan.Zero) return framesDone;

        var perFrame = cpu.TotalMilliseconds / framesDone;
        var missing = (MinimumCpu - cpu).TotalMilliseconds;
        return Math.Max(1, (int)Math.Ceiling(missing / perFrame * 1.05));
    }
}

/// <summary>What a metered run measured.</summary>
/// <param name="Frames">How many frames were drawn, which can be more than were asked for.</param>
/// <param name="Cpu">The processor time they cost.</param>
/// <param name="RelativeError">How far <paramref name="Cpu"/> can be off, as a share of itself.</param>
public readonly record struct CpuMeasurement(int Frames, TimeSpan Cpu, double RelativeError);
