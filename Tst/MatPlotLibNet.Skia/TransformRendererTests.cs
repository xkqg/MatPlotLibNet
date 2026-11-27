// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Transforms;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A PNG or a PDF is drawn with the <see cref="IChartRenderer"/> its transform was made with, whichever one that is: one
/// of the caller's draws what it draws, and the library's renderer is not asked.</summary>
public class TransformRendererTests
{
    private static Figure Small() => Plt.Create().WithSize(40, 30).Plot([0.0, 1], [0.0, 1]).Build();

    /// <summary>A renderer of the caller's: fills the whole figure in one colour.</summary>
    private static RecordingChartRenderer CallersRenderer() =>
        new((figure, ctx) => ctx.DrawRectangle(new Rect(0, 0, figure.Width, figure.Height), new ShapeStyle(Color.FromHex("#123456"), null, 0)));

    [Fact]
    public void APng_IsDrawnWithARendererOfTheCallers()
    {
        var callers = CallersRenderer();
        using var stream = new MemoryStream();

        new PngTransform(callers).Transform(Small(), stream);

        Assert.Equal(1, callers.Renders);
        using var bitmap = SKBitmap.Decode(stream.ToArray());
        Assert.Equal(new SKColor(0x12, 0x34, 0x56), bitmap.GetPixel(20, 15));
    }

    [Fact]
    public void APdf_IsDrawnWithARendererOfTheCallers()
    {
        var callers = CallersRenderer();
        using var stream = new MemoryStream();

        new PdfTransform(callers).Transform(Small(), stream);

        Assert.Equal(1, callers.Renders);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void AMultiPagePdf_IsDrawnWithARendererOfTheCallers_OncePerPage()
    {
        var callers = CallersRenderer();
        using var stream = new MemoryStream();

        new PdfTransform(callers).TransformMultiPage([Small(), Small()], stream);

        Assert.Equal(2, callers.Renders);
    }
}
