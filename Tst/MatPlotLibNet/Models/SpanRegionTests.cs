// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

public class SpanRegionTests
{
    [Fact]
    public void SpanRegion_StoresMinMaxOrientation()
    {
        var span = new SpanRegion(2.0, 5.0, Orientation.Horizontal);
        Assert.Equal(2.0, span.Min);
        Assert.Equal(5.0, span.Max);
        Assert.Equal(Orientation.Horizontal, span.Orientation);
    }

    [Fact]
    public void DefaultAlpha_Is0Point2()
    {
        var span = new SpanRegion(1.0, 3.0, Orientation.Vertical);
        Assert.Equal(0.2, span.Alpha);
    }

    [Fact]
    public void Axes_AxHSpan_AddsSpan()
    {
        var axes = new Axes();
        var span = axes.AxHSpan(1.0, 3.0);
        Assert.Single(axes.Spans);
        Assert.Equal(Orientation.Horizontal, span.Orientation);
    }

    [Fact]
    public void Axes_AxVSpan_AddsSpan()
    {
        var axes = new Axes();
        var span = axes.AxVSpan(2.0, 4.0);
        Assert.Single(axes.Spans);
        Assert.Equal(Orientation.Vertical, span.Orientation);
    }
}
