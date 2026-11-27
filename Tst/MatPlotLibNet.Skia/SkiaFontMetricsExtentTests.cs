// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The Skia metrics measure a line of a text's shaped advance, at the library's line spacing; the PNG surface measures
/// by the installed metrics.</summary>
public sealed class SkiaFontMetricsExtentTests
{
    private static readonly Font Font = new() { Size = 12 };

    [Fact]
    public void TheBox_IsALineOfTheShapedAdvance()
    {
        var metrics = new SkiaFontMetrics();

        Assert.Equal(TextExtent.OfLine(metrics.Shaped.Of("Peak", Font).Width, Font).Box, metrics.Measure("Peak", Font));
    }

    [Fact]
    public void AnEmptyText_HasNoWidth_ButALinesHeight()
    {
        Assert.Equal(TextExtent.OfLine(0, Font).Box, new SkiaFontMetrics().Measure("", Font));
    }

    [Fact]
    public void ThePngSurface_MeasuresByTheInstalledMetrics()
    {
        using var bitmap = new SkiaSharp.SKBitmap(10, 10);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        IRenderContext ctx = new SkiaRenderContext(canvas);

        Assert.Equal(FontMetricsDefault.Current.ExtentOf("Peak", Font), ctx.ExtentOf(TextLabel.Plain("Peak"), Font));
    }
}
