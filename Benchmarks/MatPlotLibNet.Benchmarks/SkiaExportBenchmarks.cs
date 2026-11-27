// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Benchmarks;

/// <summary>What the Skia package costs: pictures and PDF pages, the SVG it writes with glyph outlines, and shaping text. The setup
/// loads the package, so the SVG row is outlines even when it is the only row a run asks for; loading it changes how every later SVG
/// of the process is written, so the class is measured in a run of its own.</summary>
[MemoryDiagnoser]
public class SkiaExportBenchmarks
{
    private Figure _simpleFigure = default!;
    private Figure _complexFigure = default!;
    private Figure _labelledFigure = default!;

    [GlobalSetup]
    public void Setup()
    {
        FigureSkiaExtensions.Initialize();
        var x = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
        var y = x.Select(v => Math.Sin(v * 0.1) * 50 + 50).ToArray();

        _simpleFigure = Plt.Create()
            .WithTitle("Simple")
            .Plot(x, y)
            .Build();

        _complexFigure = Plt.Create()
            .WithTitle("Complex")
            .WithTheme(Theme.Dark)
            .Plot(x, y, l => { l.Color = Colors.Blue; })
            .Scatter(x.Take(20).ToArray(), y.Take(20).ToArray())
            .Bar(["A", "B", "C"], [10.0, 25, 15])
            .Build();

        // A grid of nine labelled panels: every title, axis label and tick label is a glyph outline in the SVG once this package is
        // loaded, and the outlines are kept after the first draw, so the row is the cost of writing them, not of making them.
        var grid = Plt.Create().WithSize(900, 700).WithTitle("Nine panels with labels");
        for (int i = 0; i < 9; i++)
            grid = grid.AddSubPlot(3, 3, i + 1, ax => ax.Plot(x, y).WithTitle("Panel").SetXLabel("time (s)").SetYLabel("level (mV)"));
        _labelledFigure = grid.Build();

        // A 256 x 256 image under the library's own theme (its cells drawn as they are, nearest) and under the default matplotlib style.
        var image = SvgRenderingBenchmarks.Image256();
        _image256Library = Plt.Create().WithTheme(Theme.Default).AddSubPlot(1, 1, 1, ax => ax.Image(image)).Build();
        _image256 = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Image(image)).Build();
    }

    private Figure _image256Library = default!;
    private Figure _image256 = default!;

    /// <summary>The PNG of a 256 x 256 image under the library's own theme: 65,536 cells.</summary>
    [Benchmark]
    public byte[] ToPng_Image256Library() => _image256Library.ToPng();

    /// <summary>The PNG of a 256 x 256 image under the default matplotlib style.</summary>
    [Benchmark]
    public byte[] ToPng_Image256() => _image256.ToPng();

    [Benchmark]
    public byte[] ToPng_Simple() => _simpleFigure.ToPng();

    [Benchmark]
    public byte[] ToPng_Complex() => _complexFigure.ToPng();

    /// <summary>The SVG of nine labelled panels, whose every text is a kept glyph outline.</summary>
    [Benchmark]
    public string ToSvg_Labelled() => _labelledFigure.ToSvg();

    [Benchmark]
    public byte[] ToPdf_Simple() => _simpleFigure.ToPdf();

    [Benchmark]
    public byte[] ToPdf_Complex() => _complexFigure.ToPdf();

    /// <summary>Tick labels of the kinds an axis writes: numbers, signed numbers, thousands, a time and a date.</summary>
    private static readonly string[] TickLabels =
        ["0", "0.25", "0.5", "0.75", "1.0", "2.5", "10", "100", "1,000", "-2.5", "1e-3", "12:30", "2026-01-15", "Q3"];

    private static readonly Font TickFont = new() { Size = 10 };

    /// <summary>Shapes every tick label afresh, the way a text that is not yet kept is shaped (with its bidi resolution), and
    /// sums the widths.</summary>
    [Benchmark]
    public double SkiaMeasure_TickLabels()
    {
        double sum = 0;
        foreach (var label in TickLabels) sum += SkiaGlyphPathProvider.MeasureAdvance(label, TickFont);
        return sum;
    }

    /// <summary>Shapes every tick label afresh and writes its glyph outline as SVG path data, the work a label costs the first
    /// time an SVG draws it (a later draw reads the kept outline).</summary>
    [Benchmark]
    public int SkiaOutline_TickLabels()
    {
        int length = 0;
        foreach (var label in TickLabels) length += new ShapedTextStore().Of(label, TickFont).Outline.Length;
        return length;
    }
}
