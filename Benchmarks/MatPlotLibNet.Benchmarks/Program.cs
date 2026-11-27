// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using BenchmarkDotNet.Running;
using MatPlotLibNet;
using MatPlotLibNet.Benchmarks.Drag;
using MatPlotLibNet.Benchmarks.Idle;
using MatPlotLibNet.Benchmarks.LiveWall;
using MatPlotLibNet.Benchmarks.MatplotlibThemes;

// `--live-wall [seconds]` drives a live dashboard in real time and prints what each part cost, so a profiler can be attached
// to a process that behaves like a server pushing charts to its viewers. Anything else goes to BenchmarkDotNet.
if (args.Length > 0 && args[0] == "--live-wall")
{
    var seconds = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 60;
    var wall = new LiveWall(LiveWallShape.Operations, DateTime.UtcNow);
    Console.WriteLine(new LiveWallRun(wall).For(TimeSpan.FromSeconds(seconds)));
    return;
}

// `--live-wall-burst <cycles>` draws the wall's mix without pausing — eight beats of trend and tiles to one round of the
// slow parts, which is the ratio of the live cadences — so a sampling profiler sees drawing and nothing else.
if (args.Length > 1 && args[0] == "--live-wall-burst")
{
    var wall = new LiveWall(LiveWallShape.Operations, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    var cycles = int.Parse(args[1], CultureInfo.InvariantCulture);
    var chars = 0L;
    for (var c = 0; c < cycles; c++)
    {
        for (var b = 0; b < 8; b++)
        {
            wall.Advance();
            chars += wall.Trend.ToSvg().Length + wall.Tiles().ToSvg().Length;
        }
        chars += wall.Panels().ToSvg().Length + wall.LogPanels().ToSvg().Length + wall.Surface().ToSvg().Length;
    }
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{cycles} cycles, {chars / 1024} KiB of SVG"));
    return;
}

// `--live-wall-dump <directory>` writes one SVG per part of the wall, so the documents themselves can be read and compared.
if (args.Length > 1 && args[0] == "--live-wall-dump")
{
    var wall = new LiveWall(LiveWallShape.Operations, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    Directory.CreateDirectory(args[1]);
    File.WriteAllText(Path.Combine(args[1], "trend.svg"), wall.Trend.ToSvg());
    File.WriteAllText(Path.Combine(args[1], "tiles.svg"), wall.Tiles().ToSvg());
    File.WriteAllText(Path.Combine(args[1], "panels.svg"), wall.Panels().ToSvg());
    File.WriteAllText(Path.Combine(args[1], "log-panels.svg"), wall.LogPanels().ToSvg());
    File.WriteAllText(Path.Combine(args[1], "surface.svg"), wall.Surface().ToSvg());
    return;
}

// `--drag <mode> <frames>` turns a 3-D surface a degree a frame and prints what a frame costs. Modes: svg1 svg4 svg4g skia1 skia4
// ctl1 ctl4 (the paint, then 1 = the surface alone, 4 = the surface and three line panels; g = Skia loaded for SVG text).
if (args.Length > 1 && args[0] == "--drag")
{
    if (!DragMode.TryParse(args[1], out var dragMode))
    {
        Console.Error.WriteLine($"unknown drag mode '{args[1]}'");
        return;
    }
    var dragFrames = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 300;
    var drag = new DragRun(dragMode, dragFrames);
    Console.WriteLine(drag.Execute());
    if (dragMode.Paint == DragPaint.Svg) Console.WriteLine(drag.TextPath(new DragFigures().Surface()));
    return;
}

// `--timer-idle <seconds> [figures]` holds streaming figures that get no data and prints what their timers cost; with 0 figures
// it measures the floor of an idle process the same way.
if (args.Length > 1 && args[0] == "--timer-idle")
{
    var idleSeconds = int.Parse(args[1], CultureInfo.InvariantCulture);
    var idleFigures = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 1;
    Console.WriteLine(new TimerIdleRun(idleFigures, TimeSpan.FromSeconds(idleSeconds)).Execute());
    return;
}

// `--theme-rows-bytes` prints the length in bytes of the SVG of every SVG row of the matplotlib-theme benchmarks, the number a
// step that makes a document shorter is judged by. It does not load the Skia package, whose presence changes the SVG text.
if (args.Length > 0 && args[0] == "--theme-rows-bytes")
{
    foreach (var row in new MatplotlibThemeFigures().Rows.Where(r => r.Output == MatplotlibThemeOutput.Svg))
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{row.Name} {System.Text.Encoding.UTF8.GetByteCount(row.Figure.ToSvg())}"));
    }
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
