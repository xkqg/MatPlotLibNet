// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MatPlotLibNet.Benchmarks.LiveWall;

/// <summary>What one part of the wall cost over a run: how often it was drawn, the time and the memory one draw took,
/// and the size of the SVG it produced.</summary>
public readonly record struct SectionCost(string Name, int Draws, double MeanMs, double MaxMs, double MeanKiBAllocated, double MeanSvgKiB)
{
    internal static SectionCost From(string name, List<DrawSample> samples)
    {
        if (samples.Count == 0)
        {
            return new SectionCost(name, 0, 0, 0, 0, 0);
        }
        double ms = 0, max = 0, bytes = 0, svg = 0;
        foreach (var s in samples)
        {
            ms += s.Ms;
            max = Math.Max(max, s.Ms);
            bytes += s.Allocated;
            svg += s.SvgChars;
        }
        var n = samples.Count;
        return new SectionCost(name, n, ms / n, max, bytes / n / 1024, svg / n / 1024);
    }
}

/// <summary>One draw, measured.</summary>
internal readonly record struct DrawSample(double Ms, long Allocated, int SvgChars);

/// <summary>The result of a run: what each part cost, and what the whole process spent while the wall was live.</summary>
public readonly record struct LiveWallReport(TimeSpan Wall, TimeSpan Cpu, IReadOnlyList<SectionCost> Sections)
{
    /// <summary>The process's CPU over the run as a share of ONE core.</summary>
    public double CorePercent => 100 * Cpu.TotalMilliseconds / Wall.TotalMilliseconds;

    /// <summary>The report as a table a reader can paste.</summary>
    public override string ToString()
    {
        var c = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine(string.Create(c, $"live wall: {Wall.TotalSeconds:0.0} s, process CPU {Cpu.TotalMilliseconds:0} ms = {CorePercent:0.0} % of one core"));
        sb.AppendLine("section      draws   mean ms    max ms   KiB alloc/draw   SVG KiB/draw");
        foreach (var s in Sections)
        {
            sb.AppendLine(string.Create(c, $"{s.Name,-11} {s.Draws,6} {s.MeanMs,9:0.00} {s.MaxMs,9:0.00} {s.MeanKiBAllocated,16:0.0} {s.MeanSvgKiB,14:0.0}"));
        }
        return sb.ToString();
    }
}

/// <summary>
/// Drives a <see cref="LiveWall"/> in real time on the cadences a live dashboard uses: the trend and the tile row on every
/// beat, the stacked panels, the log-scale panels and the 3-D surface every two seconds. Every draw renders its whole SVG, as a server does
/// before it pushes the document to its viewers. The thread runs below normal priority, so the run measures the library
/// without competing with the rest of the box.
/// </summary>
public sealed class LiveWallRun
{
    private static readonly TimeSpan SlowCadence = TimeSpan.FromSeconds(2);

    private readonly LiveWall _wall;

    /// <summary>A run over <paramref name="wall"/>.</summary>
    public LiveWallRun(LiveWall wall) => _wall = wall ?? throw new ArgumentNullException(nameof(wall));

    /// <summary>Runs the wall for <paramref name="duration"/> and reports what it cost.</summary>
    public LiveWallReport For(TimeSpan duration)
    {
        var previous = Thread.CurrentThread.Priority;
        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
        try
        {
            return Measure(duration);
        }
        finally
        {
            Thread.CurrentThread.Priority = previous;
        }
    }

    private LiveWallReport Measure(TimeSpan duration)
    {
        var trend = new List<DrawSample>();
        var tiles = new List<DrawSample>();
        var panels = new List<DrawSample>();
        var logPanels = new List<DrawSample>();
        var surface = new List<DrawSample>();
        var process = Process.GetCurrentProcess();
        var cpuBefore = process.TotalProcessorTime;
        var clock = Stopwatch.StartNew();
        var nextSlow = TimeSpan.Zero;
        var beat = 0L;
        while (clock.Elapsed < duration)
        {
            _wall.Advance();
            trend.Add(Draw(() => _wall.Trend.ToSvg()));
            tiles.Add(Draw(() => _wall.Tiles().ToSvg()));
            if (clock.Elapsed >= nextSlow)
            {
                panels.Add(Draw(() => _wall.Panels().ToSvg()));
                logPanels.Add(Draw(() => _wall.LogPanels().ToSvg()));
                surface.Add(Draw(() => _wall.Surface().ToSvg()));
                nextSlow += SlowCadence;
            }
            beat++;
            var due = LiveWall.Beat * beat;
            var wait = due - clock.Elapsed;
            if (wait > TimeSpan.Zero)
            {
                Thread.Sleep(wait);
            }
        }
        var wall = clock.Elapsed;
        process.Refresh();
        var cpu = process.TotalProcessorTime - cpuBefore;
        return new LiveWallReport(wall, cpu,
        [
            SectionCost.From("trend", trend),
            SectionCost.From("tiles", tiles),
            SectionCost.From("panels", panels),
            SectionCost.From("log panels", logPanels),
            SectionCost.From("surface", surface),
        ]);
    }

    // The total across threads, so work a renderer hands to other threads is counted with the draw.
    private static DrawSample Draw(Func<string> render)
    {
        var allocated = GC.GetTotalAllocatedBytes(precise: true);
        var started = Stopwatch.GetTimestamp();
        var svg = render();
        var ms = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        return new DrawSample(ms, GC.GetTotalAllocatedBytes(precise: true) - allocated, svg.Length);
    }
}
