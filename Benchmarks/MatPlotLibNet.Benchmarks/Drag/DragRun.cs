// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Benchmarks.Drag;

/// <summary>Turns the 3-D surface of a figure a degree a frame and draws every frame the way <see cref="DragMode"/> says, which is
/// the cost of a rotation drag: a browser viewer is sent the SVG, a native control paints. The turn is the Azimuth of the surface's
/// axes, the property the interaction controller writes.</summary>
public sealed class DragRun(DragMode mode, int frames)
{
    private const int WarmFrames = 600;

    /// <summary>Warms up, then draws <c>frames</c> frames and measures them; a frame too short for the processor clock's step is
    /// measured on more frames than that, until one step is a hundredth of the time read.</summary>
    public DragResult Execute()
    {
        using var process = Process.GetCurrentProcess();
        process.PriorityClass = ProcessPriorityClass.BelowNormal;

        var figures = new DragFigures();
        var figure = mode.WithPanels ? figures.SurfaceAndPanels() : figures.Surface();
        var surface = figure.SubPlots.First(a => a.CoordinateSystem == CoordinateSystem.ThreeD);

        if (mode.SkiaLoaded) SkiaDragPaints.Load();
        if (mode.Paint == DragPaint.Svg) return Measure(figure, surface, f => f.ToSvg());
        return MeasureOnSkia(figure, surface);
    }

    /// <summary>Whether the SVG text of the first frame is written as <c>&lt;text&gt;</c> (no glyph provider) and whether the Skia
    /// package is loaded, so a reader of the numbers knows which text path they measure.</summary>
    public string TextPath(Figure figure) =>
        $"text as <text>: {figure.ToSvg().Contains("<text", StringComparison.Ordinal)}  skia loaded: {AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "MatPlotLibNet.Skia")}";

    // Kept out of the caller so that an SVG run never compiles a reference to the Skia types, which would load the package.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private DragResult MeasureOnSkia(Figure figure, Axes surface)
    {
        using var paints = new SkiaDragPaints();
        return Measure(figure, surface, mode.Paint == DragPaint.Skia ? paints.Render : paints.Control);
    }

    private DragResult Measure(Figure figure, Axes surface, Action<Figure> draw)
    {
        for (var i = 0; i < WarmFrames; i++)
        {
            surface.Azimuth += 1;
            draw(figure);
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // The processor clock moves in whole steps of the scheduler's tick, so the run goes on past the frames asked for until a
        // step is a hundredth of what it measured (CpuMeter): 300 frames of 0.85 ms read as multiples of 0.052 ms per frame.
        using var process = Process.GetCurrentProcess();
        var clock = new ProcessClock(() =>
        {
            process.Refresh();
            return process.TotalProcessorTime;
        });
        var meter = new CpuMeter(clock, CpuResolution.Of(clock.Tick()));

        var each = new List<double>(frames);
        long allocBefore = 0;
        var measured = meter.Run(frames, () =>
        {
            if (each.Count == 0) allocBefore = GC.GetTotalAllocatedBytes(true);
            var started = Stopwatch.GetTimestamp();
            surface.Azimuth += 1;
            draw(figure);
            each.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        });
        var allocated = GC.GetTotalAllocatedBytes(true) - allocBefore;

        var sorted = each.ToArray();
        Array.Sort(sorted);
        var count = measured.Frames;
        return new DragResult(
            Name: $"{mode.Paint} {(mode.WithPanels ? "surface+3 panels" : "surface")}",
            Frames: count,
            WallMs: each.Sum() / count,
            MedianMs: sorted[count / 2],
            P95Ms: sorted[(int)(count * 0.95)],
            CpuMs: measured.Cpu.TotalMilliseconds / count,
            CpuError: measured.RelativeError,
            AllocKiB: allocated / (double)count / 1024.0);
    }
}
