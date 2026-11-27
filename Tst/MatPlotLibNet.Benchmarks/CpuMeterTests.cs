// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Benchmarks.Drag;

namespace MatPlotLibNet.Benchmarks.Tests;

/// <summary>
/// The rule that lets a drag run read its processor time. <c>Process.TotalProcessorTime</c> moves in steps of the scheduler's tick
/// (15.625 ms on Windows), so a run of 300 frames of 0.85 ms (255 ms in all) reads as sixteen steps or seventeen, a difference of
/// 6 %: it printed multiples of 0.052 ms per frame and a false 17 % regression of the SVG drag. The rule: the run goes on until
/// one step is at most a hundredth of the processor time it measured. The clock here is a stand-in that moves in whole steps, whose
/// every read costs a little processor time as a spin on the real one does, so the tests are exact and take no real time.
/// </summary>
public class CpuMeterTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromTicks(156250);

    /// <summary>A processor clock that is quantised to <c>tick</c>, advanced by the frames a test draws and by 10 µs for each read.</summary>
    private sealed class SteppedClock(TimeSpan tick)
    {
        private long _exact;

        public void Spend(TimeSpan cpu) => _exact += cpu.Ticks;

        public TimeSpan Read()
        {
            _exact += TimeSpan.FromMicroseconds(10).Ticks;
            return TimeSpan.FromTicks(_exact / tick.Ticks * tick.Ticks);
        }
    }

    [Fact]
    public void TheLeastProcessorTimeThatResolvesAFrameIsAHundredStepsOfTheClock()
    {
        var resolution = CpuResolution.Of(Tick);

        Assert.Equal(TimeSpan.FromTicks(15625000), resolution.MinimumCpu);
        Assert.False(resolution.Resolves(TimeSpan.FromMilliseconds(255)));
        Assert.True(resolution.Resolves(TimeSpan.FromMilliseconds(1563)));
    }

    [Fact]
    public void TheErrorOfAReadingIsOneStepOverTheProcessorTimeRead()
    {
        var resolution = CpuResolution.Of(Tick);

        Assert.Equal(0.0625, resolution.ErrorOf(TimeSpan.FromMilliseconds(250)), 6);
        Assert.Equal(1.0, resolution.ErrorOf(TimeSpan.Zero));
    }

    [Fact]
    public void ARunThatResolvesNeedsNoFurtherFrames()
    {
        Assert.Equal(0, CpuResolution.Of(Tick).ExtraFramesNeeded(TimeSpan.FromSeconds(2), 300));
    }

    [Fact]
    public void ARunThatDoesNotResolveIsGivenTheFramesItLacksAtItsOwnRateAndNotMoreThanAFifthOver()
    {
        var resolution = CpuResolution.Of(Tick);
        var cpu = TimeSpan.FromMilliseconds(255);
        var extra = resolution.ExtraFramesNeeded(cpu, 300);

        var total = cpu + cpu / 300 * extra;
        Assert.True(resolution.Resolves(total), $"{total} does not resolve");
        Assert.True(total < resolution.MinimumCpu * 1.2, $"{total} overshoots");
    }

    [Fact]
    public void ARunThatReadNoProcessorTimeAtAllDoublesItsFrames()
    {
        Assert.Equal(300, CpuResolution.Of(Tick).ExtraFramesNeeded(TimeSpan.Zero, 300));
    }

    [Fact]
    public void ARunOfNoFramesCannotBeExtended()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CpuResolution.Of(Tick).ExtraFramesNeeded(TimeSpan.Zero, 0));
    }

    [Fact]
    public void TheTickIsTheSmallestStepTheClockMakesWhileTheProcessSpins()
    {
        var clock = new SteppedClock(TimeSpan.FromMilliseconds(7));

        Assert.Equal(TimeSpan.FromMilliseconds(7), new ProcessClock(clock.Read).Tick());
    }

    [Fact]
    public void AClockThatNeverStepsIsRefusedAtTheDeadlineAndNotWaitedOnForever()
    {
        var frozen = new ProcessClock(() => TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(50));

        Assert.Throws<InvalidOperationException>(() => frozen.Tick());
        Assert.Throws<InvalidOperationException>(() => frozen.AlignToStep());
    }

    [Fact]
    public void ARunStartsOnAStepOfTheClockSoThatItCountsWholeStepsFromItsFirstFrame()
    {
        var clock = new SteppedClock(Tick);
        var before = clock.Read();

        var start = new ProcessClock(clock.Read).AlignToStep();

        Assert.True(start > before);
        Assert.Equal(TimeSpan.Zero, TimeSpan.FromTicks(start.Ticks % Tick.Ticks));
    }

    [Fact]
    public void AFrameTooShortForTheTickIsMeasuredOnMoreFramesThanAskedUntilTheTickIsAHundredthOfTheTotal()
    {
        var clock = new SteppedClock(Tick);
        var meter = new CpuMeter(new ProcessClock(clock.Read), CpuResolution.Of(Tick));

        var measured = meter.Run(300, () => clock.Spend(TimeSpan.FromMicroseconds(850)));

        Assert.True(measured.Frames > 300);
        Assert.True(measured.RelativeError <= 0.01, $"error {measured.RelativeError}");
        var perFrameMs = measured.Cpu.TotalMilliseconds / measured.Frames;
        // The clock reads the whole steps that are over, so the reading is never above the truth and at most one step under it.
        Assert.InRange(perFrameMs, 0.85 * 0.99, 0.85 + 0.001);
    }

    [Fact]
    public void AFrameLongEnoughForTheTickIsMeasuredOnTheFramesAsked()
    {
        var clock = new SteppedClock(Tick);
        var meter = new CpuMeter(new ProcessClock(clock.Read), CpuResolution.Of(Tick));

        var measured = meter.Run(300, () => clock.Spend(TimeSpan.FromMilliseconds(12)));

        Assert.Equal(300, measured.Frames);
        Assert.True(measured.RelativeError <= 0.01);
    }

    [Fact]
    public void AFrameThatSpendsNoProcessorTimeStopsAtTwentyTimesTheFramesAskedAndReportsItsErrorAsItIs()
    {
        var clock = new SteppedClock(Tick);
        var meter = new CpuMeter(new ProcessClock(clock.Read), CpuResolution.Of(Tick));

        var measured = meter.Run(100, () => { });

        Assert.Equal(2000, measured.Frames);
        Assert.True(measured.RelativeError > 0.01);
    }

    [Fact]
    public void EveryFrameIsDrawnOnceAndOnlyOnce()
    {
        var clock = new SteppedClock(Tick);
        var meter = new CpuMeter(new ProcessClock(clock.Read), CpuResolution.Of(Tick));
        var drawn = 0;

        var measured = meter.Run(300, () => { drawn++; clock.Spend(TimeSpan.FromMicroseconds(850)); });

        Assert.Equal(measured.Frames, drawn);
    }
}
