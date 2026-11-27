// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Benchmarks.Drag;

/// <summary>Draws frames and says what they cost in processor time, going on past the frames asked for until the processor clock's
/// step is small beside what was measured (<see cref="CpuResolution"/>), so a short frame is never read as a multiple of a step.</summary>
/// <param name="clock">The processor clock of the process.</param>
/// <param name="resolution">When the time read is enough.</param>
public sealed class CpuMeter(ProcessClock clock, CpuResolution resolution)
{
    /// <summary>The most a run grows past the frames asked for: a frame that costs no processor time must end the run, not
    /// extend it for ever.</summary>
    private const int MaxGrowth = 20;

    /// <summary>Draws <paramref name="frames"/> frames, then as many more as the rule asks for.</summary>
    /// <param name="frames">The frames asked for.</param>
    /// <param name="frame">Draws one frame.</param>
    /// <returns>How many frames were drawn, what they cost and how far that can be off.</returns>
    public CpuMeasurement Run(int frames, Action frame)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(frames, 1);
        var start = clock.AlignToStep();
        var done = 0;
        var toDraw = frames;
        var cap = frames * MaxGrowth;
        TimeSpan cpu;
        while (true)
        {
            for (var i = 0; i < toDraw; i++) frame();
            done += toDraw;
            cpu = clock.Read() - start;
            toDraw = Math.Min(resolution.ExtraFramesNeeded(cpu, done), cap - done);
            if (toDraw <= 0) break;
        }
        return new CpuMeasurement(done, cpu, resolution.ErrorOf(cpu));
    }
}
