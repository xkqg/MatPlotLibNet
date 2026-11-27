// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A raster export covers a horizontal or vertical line by the share of each pixel it takes, as matplotlib's Agg rasterizer
/// does, under the themes that ask for it (<see cref="RasterLook.StrokesOnPixelGrid"/>, the matplotlib themes). A library theme draws
/// a PNG as it draws any canvas that does not snap: its lines are stroked, as they were before the Agg coverage was added, and the
/// picture of a figure is the same on a raster export and on a canvas that is not one.</summary>
public sealed class LibraryThemeRasterTests
{
    private const float Width = 1.3888889f;   // 1 pt at 100 dpi

    private static Figure Gridded(Theme theme) => Plt.Create().WithSize(320, 240).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 5.0, 10.0], [1.0, 8.0, 3.0]).Bar(["a", "b"], [2.0, 3.0]).WithGrid(g => g).SetXLim(0, 10).SetYLim(0, 10))
        .Build();

    private static SKBitmap Draw(Figure figure, bool rasterExport)
    {
        var bitmap = new SKBitmap(320, 240);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        new ChartRenderer().Render(figure, new SkiaRenderContext(canvas) { SnapsToPixelGrid = rasterExport });
        return bitmap;
    }

    private static bool Same(SKBitmap a, SKBitmap b) => a.Bytes.AsSpan().SequenceEqual(b.Bytes);

    /// <summary>A library theme draws the same picture on a raster export and on a canvas that does not snap; a matplotlib theme, the
    /// positive control, draws another, since the snap and the coverage are what it asks for.</summary>
    [Theory]
    [InlineData("default", true)]
    [InlineData("dark", true)]
    [InlineData("v2", false)]
    public void ATheme_DrawsTheSameOnARasterExport_OnlyWhenItDoesNotSnap(string theme, bool same)
    {
        using var raster = Draw(Gridded(theme.AsTheme()), rasterExport: true);
        using var plain = Draw(Gridded(theme.AsTheme()), rasterExport: false);

        Assert.Equal(same, Same(raster, plain));
    }

    [Theory]
    [InlineData(false, 189, 193)]   // a library theme: Skia covers the row beside a 1.39 px line in quarters, 255 * (1 - 0.25) = 191
    [InlineData(true, 204, 207)]    // a matplotlib theme: the share Agg covers, 255 * (1 - 0.194) = 205.5
    public void ALineDrawnUnderATheme_CoversTheRowBesideItAsThatThemeAsks(bool matplotlib, int low, int high)
    {
        using var bitmap = new SKBitmap(120, 120);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        var context = new SkiaRenderContext(canvas) { SnapsToPixelGrid = true };
        context.UseLooksOf(matplotlib ? Theme.MatplotlibV2 : Theme.Default);

        context.DrawLine(new Point(5, 20.5), new Point(55, 20.5), new StrokeStyle(Color.FromHex("#000000"), Width, LineStyle.Solid));

        Assert.InRange(bitmap.GetPixel(30, 19).Red, low, high);
    }
}
