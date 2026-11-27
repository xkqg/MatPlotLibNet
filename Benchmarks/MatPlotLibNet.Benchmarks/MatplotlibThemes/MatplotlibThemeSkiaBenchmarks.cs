// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Benchmarks.MatplotlibThemes;

/// <summary>What the Skia package costs on the nine-panel figures of the matplotlib themes (see
/// <see cref="MatplotlibThemeFigures"/>): a raster export, which puts spines, ticks, grid lines and bar edges on the pixel grid,
/// and a PDF page, which does not, and then the ask the renderers make of the render context to put a mark on a pixel grid, with
/// and without a context that snaps. The class loads the Skia package, which changes how SVG text is written, so it holds no SVG
/// row and is measured in a run of its own.</summary>
[MemoryDiagnoser]
public class MatplotlibThemeSkiaBenchmarks
{
    private const int Asks = 1_000;

    private MatplotlibThemeFigures _figures = default!;
    private SKBitmap _bitmap = default!;
    private SKCanvas _canvas = default!;
    private IPixelGridRenderContext _notSnapping = default!;
    private IPixelGridRenderContext _snapping = default!;
    private int _tick;

    [GlobalSetup]
    public void Setup()
    {
        _figures = new MatplotlibThemeFigures();
        _bitmap = new SKBitmap(100, 100);
        _canvas = new SKCanvas(_bitmap);
        _notSnapping = new SkiaRenderContext(_canvas);
        var snapping = new SkiaRenderContext(_canvas) { SnapsToPixelGrid = true };
        snapping.UseLooksOf(Theme.MatplotlibV2);
        _snapping = snapping;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private byte[] Png(string row) => _figures.Of(row, MatplotlibThemeOutput.Png).ChangedFor(_tick++).ToPng();

    private byte[] Pdf(string row) => _figures.Of(row, MatplotlibThemeOutput.Pdf).ChangedFor(_tick++).ToPdf();

    /// <summary>Nine panels under the matplotlib v2 theme, each with minor ticks and a grid on both tick sets, as a picture.</summary>
    [Benchmark(Baseline = true)]
    public byte[] V2Grid3x3() => Png(nameof(V2Grid3x3));

    /// <summary>The same nine panels under the classic theme, as a picture.</summary>
    [Benchmark]
    public byte[] ClassicGrid3x3() => Png(nameof(ClassicGrid3x3));

    /// <summary>The v2 nine panels as a PDF page: the renderers ask the context to snap, and it does not.</summary>
    [Benchmark]
    public byte[] V2Grid3x3Pdf() => Pdf(nameof(V2Grid3x3Pdf));

    /// <summary>The classic nine panels as a PDF page.</summary>
    [Benchmark]
    public byte[] ClassicGrid3x3Pdf() => Pdf(nameof(ClassicGrid3x3Pdf));

    /// <summary>A thousand horizontal lines asked of a context that does not snap: each is handed back as it stands.</summary>
    [Benchmark(OperationsPerInvoke = Asks)]
    public double PixelGridAskNotSnapping() => Ask(_notSnapping);

    /// <summary>A thousand horizontal lines asked of a context that snaps: each is read off the matrix of the canvas and moved.</summary>
    [Benchmark(OperationsPerInvoke = Asks)]
    public double PixelGridAskSnapping() => Ask(_snapping);

    private static double Ask(IPixelGridRenderContext context)
    {
        double sum = 0;
        for (var i = 0; i < Asks; i++)
        {
            var y = 20.3 + (i * 0.01);
            sum += context.OnPixelGrid(new PixelSegment(new Point(10.3, y), new Point(50.3, y)), 1.0, projectingEnds: false).From.Y;
        }
        return sum;
    }
}
