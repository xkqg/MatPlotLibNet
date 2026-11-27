// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="PolarBarSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class PolarBarSeriesTests
{
    /// <summary>The angle one drawn wedge spans, read off its outline: the outer arc first, then the inner one at the centre (a bar on
    /// the centre).</summary>
    private static double DrawnWidth(Theme theme, Action<PolarBarSeries>? configure = null)
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        var bar = axes.PolarBar([2.0], [1.0]);
        configure?.Invoke(bar);
        var ctx = new RecordingRenderContext();
        new PolarAxesRenderer(axes, new Rect(100, 60, 400, 400), ctx, theme).Render();
        var points = ctx.Of<DrawPolygonCall>().Single().Points;
        var centre = points[^1];
        double Angle(Point p) => Math.Atan2(-(p.Y - centre.Y), p.X - centre.X);
        return Angle(points[points.Length / 2 - 1]) - Angle(points[0]);
    }

    /// <summary>matplotlib's <c>bar</c> on a polar axes rests at <c>width=0.8</c>, as a Cartesian bar does: printed with 3.11.2 under both
    /// styles, <c>ax.bar([0, 1, 2], [1, 2, 3])</c> on a polar axes makes wedges <c>get_width()</c> 0.8 radians wide. The matplotlib themes draw
    /// a polar bar given no width that wide; the library's own themes keep the 0.3 it has always had; a width given is drawn as given.</summary>
    [Fact]
    public void ABarGivenNoWidth_IsDrawnAtItsThemesWidth_AndAGivenOneAsGiven()
    {
        Assert.Equal(0.8, DrawnWidth(Theme.MatplotlibV2), 9);
        Assert.Equal(0.8, DrawnWidth(Theme.MatplotlibClassic), 9);
        Assert.Equal(0.3, DrawnWidth(Theme.Default), 9);
        Assert.Equal(0.3, DrawnWidth(Theme.MatplotlibV2, b => b.BarWidth = 0.3), 9);
        Assert.Equal(0.5, DrawnWidth(Theme.Default, b => b.BarWidth = 0.5), 9);
    }
}
