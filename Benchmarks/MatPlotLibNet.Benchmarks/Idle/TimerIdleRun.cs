// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Benchmarks.Idle;

/// <summary>Holds streaming figures that never get data and measures what their render timers cost a process that does nothing
/// else. A run with no figures is the floor: the process asleep, measured the same way.</summary>
public sealed class TimerIdleRun(int figures, TimeSpan duration)
{
    private static readonly TimeSpan Settle = TimeSpan.FromSeconds(2);

    /// <summary>Builds the figures, lets them settle, then sleeps for the duration and reports what the process used.</summary>
    public TimerIdleResult Execute()
    {
        using var process = Process.GetCurrentProcess();
        process.PriorityClass = ProcessPriorityClass.BelowNormal;

        var held = new List<StreamingFigure>(figures);
        for (var i = 0; i < figures; i++)
        {
            held.Add(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StreamingPlot(720)).BuildStreaming());
        }

        Thread.Sleep(Settle);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        process.Refresh();
        var cpuBefore = process.TotalProcessorTime;
        var cyclesBefore = CycleTime(process);
        var itemsBefore = ThreadPool.CompletedWorkItemCount;

        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < duration) Thread.Sleep(250);
        clock.Stop();

        process.Refresh();
        var result = new TimerIdleResult(
            figures,
            clock.Elapsed.TotalSeconds,
            (process.TotalProcessorTime - cpuBefore).TotalMilliseconds,
            (CycleTime(process) - cyclesBefore) / 1e6,
            ThreadPool.CompletedWorkItemCount - itemsBefore);

        foreach (var figure in held) figure.Dispose();
        return result;
    }

    private static ulong CycleTime(Process process)
    {
        if (!OperatingSystem.IsWindows()) return 0;
        return QueryProcessCycleTime(process.Handle, out var cycles) ? cycles : 0;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryProcessCycleTime(IntPtr process, out ulong cycles);
}
