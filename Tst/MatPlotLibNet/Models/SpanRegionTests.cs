// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="SpanRegion"/> behavior.</summary>
public class SpanRegionTests
{
    /// <summary>Verifies that a span region stores its min, max, and orientation.</summary>
    [Fact]
    public void SpanRegion_StoresMinMaxOrientation()
    {
        var span = new SpanRegion(2.0, 5.0, Orientation.Horizontal);
        Assert.Equal(2.0, span.Min);
        Assert.Equal(5.0, span.Max);
        Assert.Equal(Orientation.Horizontal, span.Orientation);
    }

    /// <summary>Verifies that the default alpha is 0.2.</summary>
    [Fact]
    public void DefaultAlpha_Is0Point2()
    {
        var span = new SpanRegion(1.0, 3.0, Orientation.Vertical);
        Assert.Equal(0.2, span.Alpha);
    }

    /// <summary>Verifies that AxHSpan adds a horizontal span region.</summary>
    [Fact]
    public void Axes_AxHSpan_AddsSpan()
    {
        var axes = new Axes();
        var span = axes.AxHSpan(1.0, 3.0);
        Assert.Single(axes.Spans);
        Assert.Equal(Orientation.Horizontal, span.Orientation);
    }

    /// <summary>Verifies that AxVSpan adds a vertical span region.</summary>
    [Fact]
    public void Axes_AxVSpan_AddsSpan()
    {
        var axes = new Axes();
        var span = axes.AxVSpan(2.0, 4.0);
        Assert.Single(axes.Spans);
        Assert.Equal(Orientation.Vertical, span.Orientation);
    }

    /// <summary>Verifies that LineStyle defaults to None.</summary>
    [Fact]
    public void SpanRegion_LineStyle_DefaultsToNone()
    {
        var span = new SpanRegion(1.0, 3.0, Orientation.Horizontal);
        Assert.Equal(LineStyle.None, span.LineStyle);
    }

    /// <summary>Verifies that LineWidth defaults to 1.</summary>
    [Fact]
    public void SpanRegion_LineWidth_DefaultsTo1()
    {
        var span = new SpanRegion(1.0, 3.0, Orientation.Horizontal);
        Assert.Equal(1.0, span.LineWidth);
    }

    /// <summary>Verifies that EdgeColor defaults to null.</summary>
    [Fact]
    public void SpanRegion_EdgeColor_DefaultsToNull()
    {
        var span = new SpanRegion(1.0, 3.0, Orientation.Horizontal);
        Assert.Null(span.EdgeColor);
    }

    /// <summary>Verifies that Label defaults to null.</summary>
    [Fact]
    public void SpanRegion_Label_DefaultsToNull()
    {
        var span = new SpanRegion(1.0, 3.0, Orientation.Horizontal);
        Assert.Null(span.Label);
    }

    /// <summary>Verifies that Label can be set.</summary>
    [Fact]
    public void SpanRegion_Label_CanBeSet()
    {
        var span = new SpanRegion(1.0, 3.0, Orientation.Horizontal) { Label = "zone" };
        Assert.Equal("zone", span.Label);
    }
}
