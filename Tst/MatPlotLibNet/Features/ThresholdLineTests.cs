// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Features;

/// <summary>TDD tests for the ThresholdLine convenience feature on <see cref="AxesBuilder"/> and <see cref="FigureBuilder"/>.</summary>
public class ThresholdLineTests
{
    // ── Axes model tests ────────────────────────────────────────────────────

    /// <summary>Threshold adds one ReferenceLine to the axes at the given value.</summary>
    [Fact]
    public void Threshold_AddsReferenceLine_AtValue()
    {
        var axes = new Axes();
        axes.AddThreshold(70.0, Orientation.Horizontal, ThresholdBreach.Above);

        Assert.Single(axes.ReferenceLines);
        Assert.Equal(70.0, axes.ReferenceLines[0].Value);
    }

    /// <summary>Threshold adds one SpanRegion to the axes.</summary>
    [Fact]
    public void Threshold_AddsSpanRegion()
    {
        var axes = new Axes();
        axes.AddThreshold(70.0, Orientation.Horizontal, ThresholdBreach.Above);

        Assert.Single(axes.Spans);
    }

    /// <summary>For a horizontal threshold with breach=Above the span min equals the threshold value.</summary>
    [Fact]
    public void Threshold_Horizontal_Above_SpanMinEqualsValue()
    {
        var axes = new Axes();
        axes.AddThreshold(70.0, Orientation.Horizontal, ThresholdBreach.Above);

        Assert.Equal(70.0, axes.Spans[0].Min);
    }

    /// <summary>For a horizontal threshold with breach=Above the span max is double.MaxValue (extend to +∞ side).</summary>
    [Fact]
    public void Threshold_Horizontal_Above_SpanMaxIsPositiveInfinity()
    {
        var axes = new Axes();
        axes.AddThreshold(70.0, Orientation.Horizontal, ThresholdBreach.Above);

        Assert.Equal(double.MaxValue, axes.Spans[0].Max);
    }

    /// <summary>For a horizontal threshold with breach=Below the span max equals the threshold value.</summary>
    [Fact]
    public void Threshold_Horizontal_Below_SpanMaxEqualsValue()
    {
        var axes = new Axes();
        axes.AddThreshold(30.0, Orientation.Horizontal, ThresholdBreach.Below);

        Assert.Equal(30.0, axes.Spans[0].Max);
    }

    /// <summary>For a horizontal threshold with breach=Below the span min is double.MinValue (extend to -∞ side).</summary>
    [Fact]
    public void Threshold_Horizontal_Below_SpanMinIsNegativeInfinity()
    {
        var axes = new Axes();
        axes.AddThreshold(30.0, Orientation.Horizontal, ThresholdBreach.Below);

        Assert.Equal(double.MinValue, axes.Spans[0].Min);
    }

    /// <summary>The ReferenceLine is dashed by default.</summary>
    [Fact]
    public void Threshold_ReferenceLine_IsDashedByDefault()
    {
        var axes = new Axes();
        axes.AddThreshold(50.0, Orientation.Horizontal, ThresholdBreach.Above);

        Assert.Equal(LineStyle.Dashed, axes.ReferenceLines[0].LineStyle);
    }

    /// <summary>The SpanRegion orientation matches the threshold orientation.</summary>
    [Fact]
    public void Threshold_SpanRegion_OrientationMatchesThreshold()
    {
        var axes = new Axes();
        axes.AddThreshold(50.0, Orientation.Horizontal, ThresholdBreach.Above);

        Assert.Equal(Orientation.Horizontal, axes.Spans[0].Orientation);
    }

    /// <summary>A configurable color is applied to the ReferenceLine.</summary>
    [Fact]
    public void Threshold_WithColor_SetsReferenceLineColor()
    {
        var axes = new Axes();
        axes.AddThreshold(50.0, Orientation.Horizontal, ThresholdBreach.Above, color: Color.FromHex("#FF0000"));

        Assert.Equal(Color.FromHex("#FF0000"), axes.ReferenceLines[0].Color);
    }

    /// <summary>An optional label text annotation is added when label is provided.</summary>
    [Fact]
    public void Threshold_WithLabel_AddsAnnotation()
    {
        var axes = new Axes();
        axes.AddThreshold(50.0, Orientation.Horizontal, ThresholdBreach.Above, label: "Alarm");

        Assert.Single(axes.Annotations);
        Assert.Equal("Alarm", axes.Annotations[0].Text);
    }

    /// <summary>No annotation is added when label is null.</summary>
    [Fact]
    public void Threshold_WithoutLabel_NoAnnotation()
    {
        var axes = new Axes();
        axes.AddThreshold(50.0, Orientation.Horizontal, ThresholdBreach.Above);

        Assert.Empty(axes.Annotations);
    }

    // ── AxesBuilder fluent API tests ────────────────────────────────────────

    /// <summary>AxesBuilder.Threshold returns the builder for chaining.</summary>
    [Fact]
    public void AxesBuilder_Threshold_IsChainable()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .Threshold(70.0, Orientation.Horizontal, ThresholdBreach.Above))
            .Build();

        Assert.Single(figure.SubPlots[0].ReferenceLines);
        Assert.Single(figure.SubPlots[0].Spans);
    }

    /// <summary>AxesBuilder.Threshold with both breach directions adds correct spans.</summary>
    [Fact]
    public void AxesBuilder_Threshold_BelowAndAbove_BothWork()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .Threshold(20.0, Orientation.Horizontal, ThresholdBreach.Below)
                .Threshold(80.0, Orientation.Horizontal, ThresholdBreach.Above))
            .Build();

        Assert.Equal(2, figure.SubPlots[0].ReferenceLines.Count);
        Assert.Equal(2, figure.SubPlots[0].Spans.Count);
    }

    // ── FigureBuilder convenience tests ────────────────────────────────────

    /// <summary>FigureBuilder.Threshold on the default axes adds a ReferenceLine and SpanRegion.</summary>
    [Fact]
    public void FigureBuilder_Threshold_AddsReferenceLineAndSpan()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Threshold(70.0, Orientation.Horizontal, ThresholdBreach.Above)
            .Build();

        Assert.Single(figure.SubPlots[0].ReferenceLines);
        Assert.Single(figure.SubPlots[0].Spans);
    }

    // ── SVG rendering tests ─────────────────────────────────────────────────

    /// <summary>SVG output contains a line element for the threshold ReferenceLine.</summary>
    [Fact]
    public void Threshold_SvgOutput_ContainsLine()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0])
            .Threshold(2.0, Orientation.Horizontal, ThresholdBreach.Above)
            .ToSvg();

        Assert.Contains("<line", svg);
    }

    /// <summary>SVG output contains a rect element for the breach SpanRegion.</summary>
    [Fact]
    public void Threshold_SvgOutput_ContainsRect()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0])
            .Threshold(2.0, Orientation.Horizontal, ThresholdBreach.Above)
            .ToSvg();

        Assert.Contains("<rect", svg);
    }

    /// <summary>Vertical threshold (AxVLine direction) adds a vertical ReferenceLine.</summary>
    [Fact]
    public void Threshold_Vertical_AddsVerticalReferenceLine()
    {
        var axes = new Axes();
        axes.AddThreshold(5.0, Orientation.Vertical, ThresholdBreach.Above);

        Assert.Equal(Orientation.Vertical, axes.ReferenceLines[0].Orientation);
    }

    /// <summary>Vertical threshold with breach=Above: span min equals value, max is MaxValue.</summary>
    [Fact]
    public void Threshold_Vertical_Above_SpanRange()
    {
        var axes = new Axes();
        axes.AddThreshold(5.0, Orientation.Vertical, ThresholdBreach.Above);

        Assert.Equal(5.0, axes.Spans[0].Min);
        Assert.Equal(double.MaxValue, axes.Spans[0].Max);
    }
}
