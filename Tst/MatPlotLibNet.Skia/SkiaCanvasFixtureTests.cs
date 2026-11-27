// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The canvas the Skia context tests draw onto.</summary>
public class SkiaCanvasFixtureTests
{
    [Fact]
    public void TheFixture_IsATransparentSquareOfTheSizeAsked_WithAContextOntoItsCanvas()
    {
        using var fixture = new SkiaCanvasFixture(12);

        Assert.Equal((12, 12), (fixture.Bitmap.Width, fixture.Bitmap.Height));
        Assert.Equal(0, fixture.Bitmap.GetPixel(5, 5).Alpha);
        Assert.NotNull(fixture.Context);
        Assert.True(fixture.Canvas.TotalMatrix.IsIdentity);
    }
}
