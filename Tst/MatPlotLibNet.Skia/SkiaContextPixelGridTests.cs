// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A context that does not snap (a PDF page, a control that paints on a canvas of its own) holds no pixel grid: it answers every
/// ask to snap with the mark as it stands and never reads the matrix of the canvas, which only the grid does. One that snaps asks its
/// grid (<see cref="Skia.SkiaRenderContext.AggPixelGrid"/>, which has its own tests).</summary>
public class SkiaContextPixelGridTests
{
    [Fact]
    public void AContextThatDoesNotSnap_HandsEveryMarkBackAsItStands()
    {
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        IPixelGridRenderContext context = new SkiaRenderContext(canvas);
        var line = new PixelSegment(new Point(10.3, 20.7), new Point(50.3, 20.7));
        var area = new Rect(10.3, 20.7, 40.2, 30.9);

        Assert.Equal(line, context.OnPixelGrid(line, 1.0, projectingEnds: false));
        Assert.Equal(area, context.OnPixelGrid(area, 1.0));
        Assert.Null(context.MarkerOnPixelGrid(new Point(10.3, 20.7), line, 1.0));
    }

    [Fact]
    public void AContextThatSnaps_PutsAHorizontalLineOnTheCentreOfAPixelRow()
    {
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        IPixelGridRenderContext context = canvas.RasterExportUnder(Theme.MatplotlibV2);

        var snapped = context.OnPixelGrid(new PixelSegment(new Point(10.3, 20.7), new Point(50.3, 20.7)), 1.0, projectingEnds: false);

        Assert.Equal(new Point(10.5, 21.5), snapped.From);
        Assert.Equal(new Point(50.5, 21.5), snapped.To);
    }

    [Fact]
    public void AContextThatSnaps_KeepsOneGridForItsCanvas_SoAScaleSetBetweenTwoAsksIsRead()
    {
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        IPixelGridRenderContext context = canvas.RasterExportUnder(Theme.MatplotlibV2);
        var line = new PixelSegment(new Point(10.3, 20.7), new Point(50.3, 20.7));

        var natural = context.OnPixelGrid(line, 1.0, projectingEnds: false);
        canvas.Scale(2f);
        var doubled = context.OnPixelGrid(line, 1.0, projectingEnds: false);

        Assert.NotEqual(natural, doubled);
    }

    [Fact]
    public void ARasterExportThatIsNotToldATheme_DrawsAsALibraryThemeDoes_AndSnapsOnceTheThemeAsksForIt()
    {
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var raster = new SkiaRenderContext(canvas) { SnapsToPixelGrid = true };
        var line = new PixelSegment(new Point(10.3, 20.7), new Point(50.3, 20.7));

        Assert.Equal(line, ((IPixelGridRenderContext)raster).OnPixelGrid(line, 1.0, projectingEnds: false));

        raster.UseLooksOf(Theme.Dark);
        Assert.Equal(line, ((IPixelGridRenderContext)raster).OnPixelGrid(line, 1.0, projectingEnds: false));

        raster.UseLooksOf(Theme.MatplotlibClassic);
        Assert.NotEqual(line, ((IPixelGridRenderContext)raster).OnPixelGrid(line, 1.0, projectingEnds: false));

        raster.UseLooksOf(Theme.Default);
        Assert.Equal(line, ((IPixelGridRenderContext)raster).OnPixelGrid(line, 1.0, projectingEnds: false));
    }

    [Fact]
    public void AContextThatSnaps_PutsALineOfHorizontalAndVerticalSegmentsOnItsGrid_AndOneThatDoesNot_LeavesItAsItStands()
    {
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        Point[] steps = [new(10.3, 20.3), new(10.3, 60.7), new(50.6, 60.7)];
        IPixelGridRenderContext snapping = canvas.RasterExportUnder(Theme.MatplotlibV2);
        IPixelGridRenderContext plain = new SkiaRenderContext(canvas);

        Assert.Equal([new Point(10.5, 20.5), new Point(10.5, 61.5), new Point(51.5, 61.5)], snapping.OnPixelGrid(steps, 1.0));
        Assert.Same(steps, plain.OnPixelGrid(steps, 1.0));
    }
}