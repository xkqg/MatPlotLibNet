// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Benchmarks.Drag;

namespace MatPlotLibNet.Benchmarks.Tests;

/// <summary>
/// How the drag mode of the benchmark program is read from its command line: the paint, the panels, and whether the Skia package
/// is loaded for an SVG paint. A name that is not a mode must be refused, because a mistyped mode that fell back to some other
/// paint would print a number for the wrong thing.
/// </summary>
public class DragModeTests
{
    [Theory]
    [InlineData("svg1", DragPaint.Svg, false, false)]
    [InlineData("svg4", DragPaint.Svg, true, false)]
    [InlineData("skia1", DragPaint.Skia, false, true)]
    [InlineData("skia4", DragPaint.Skia, true, true)]
    [InlineData("ctl1", DragPaint.Control, false, true)]
    [InlineData("ctl4", DragPaint.Control, true, true)]
    public void AModeNamesThePaintAndWhetherThePanelsAreThere(string text, DragPaint paint, bool withPanels, bool skiaLoaded)
    {
        Assert.True(DragMode.TryParse(text, out var mode));

        Assert.Equal(new DragMode(paint, withPanels, skiaLoaded), mode);
    }

    [Theory]
    [InlineData("svg1g", false)]
    [InlineData("svg4g", true)]
    public void ATrailingGLoadsSkiaForAnSvgPaint(string text, bool withPanels)
    {
        Assert.True(DragMode.TryParse(text, out var mode));

        Assert.Equal(new DragMode(DragPaint.Svg, withPanels, SkiaLoaded: true), mode);
    }

    [Theory]
    [InlineData("skia4g", DragPaint.Skia)]
    [InlineData("ctl1g", DragPaint.Control)]
    public void ATrailingGOnAPaintThatLoadsSkiaAnywayChangesNothingElse(string text, DragPaint paint)
    {
        Assert.True(DragMode.TryParse(text, out var mode));

        Assert.Equal(paint, mode.Paint);
        Assert.True(mode.SkiaLoaded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("g")]
    [InlineData("1")]
    [InlineData("4g")]
    public void ANameWithNoRoomForAPaintAndACountIsRefused(string text)
    {
        Assert.False(DragMode.TryParse(text, out var mode));

        Assert.Equal(default, mode);
    }

    [Theory]
    [InlineData("svg")]
    [InlineData("svg2")]
    [InlineData("skia0")]
    [InlineData("ctl41")]
    [InlineData("svgg")]
    public void ANameThatDoesNotEndInOneOrFourIsRefused(string text)
    {
        Assert.False(DragMode.TryParse(text, out var mode));

        Assert.Equal(default, mode);
    }

    [Theory]
    [InlineData("bogus1")]
    [InlineData("skiaa4")]
    [InlineData("SVG4")]
    [InlineData("native4g")]
    [InlineData("14")]
    public void ANameWhosePaintIsUnknownIsRefused(string text)
    {
        Assert.False(DragMode.TryParse(text, out var mode));

        Assert.Equal(default, mode);
    }
}
