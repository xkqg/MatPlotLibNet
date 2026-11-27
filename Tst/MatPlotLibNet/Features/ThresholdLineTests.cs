// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet;

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

    /// <summary>The span runs from the threshold to the far end of the breach: above it up to the largest value, below it down to the
    /// smallest, in either orientation.</summary>
    [Theory]
    [InlineData(Orientation.Horizontal, ThresholdBreach.Above, 70.0, double.MaxValue)]
    [InlineData(Orientation.Horizontal, ThresholdBreach.Below, double.MinValue, 70.0)]
    [InlineData(Orientation.Vertical, ThresholdBreach.Above, 70.0, double.MaxValue)]
    public void TheSpan_RunsFromTheThreshold_ToTheFarEndOfTheBreach(Orientation orientation, ThresholdBreach breach, double min, double max)
    {
        var axes = new Axes();
        axes.AddThreshold(70.0, orientation, breach);

        Assert.Equal(min, axes.Spans[0].Min);
        Assert.Equal(max, axes.Spans[0].Max);
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

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  SVG output contains a line element for the threshold ReferenceLine.
        new DrawnToken("Threshold_SvgOutput_ContainsLine", () => Plt.Create()
            .Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0])
            .Threshold(2.0, Orientation.Horizontal, ThresholdBreach.Above)
            .ToSvg(), "<line"),
        //  SVG output contains a rect element for the breach SpanRegion.
        new DrawnToken("Threshold_SvgOutput_ContainsRect", () => Plt.Create()
            .Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0])
            .Threshold(2.0, Orientation.Horizontal, ThresholdBreach.Above)
            .ToSvg(), "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Vertical threshold (AxVLine direction) adds a vertical ReferenceLine.</summary>
    [Fact]
    public void Threshold_Vertical_AddsVerticalReferenceLine()
    {
        var axes = new Axes();
        axes.AddThreshold(5.0, Orientation.Vertical, ThresholdBreach.Above);

        Assert.Equal(Orientation.Vertical, axes.ReferenceLines[0].Orientation);
    }

    // ── Axes.AddThreshold — VERTICAL line + span + (labelled) annotation arms (the Horizontal arms are covered elsewhere) ──
    [Fact]
    public void AddThreshold_Vertical_WithColourAndLabel_AddsVerticalLineSpanAnnotation()
    {
        var ax = new Axes();
        ax.AddThreshold(50.0, Orientation.Vertical, ThresholdBreach.Above, Colors.Red, "Limit");
        Assert.Single(ax.ReferenceLines);   // AxVLine path
        Assert.Single(ax.Spans);            // AxVSpan path
        Assert.Single(ax.Annotations);      // label != null -> vertical annotation (annotX=value, annotY=0)
        Assert.Equal("Limit", ax.Annotations[0].Text);
    }

    // ── Axes.AddThreshold — VERTICAL + Below, no colour, no label (the skip arms on the vertical path) ──
    [Fact]
    public void AddThreshold_Vertical_Below_NoColourNoLabel_AddsLineSpan_NoAnnotation()
    {
        var ax = new Axes();
        ax.AddThreshold(50.0, Orientation.Vertical, ThresholdBreach.Below);
        Assert.Single(ax.ReferenceLines);
        Assert.Single(ax.Spans);
        Assert.Empty(ax.Annotations);       // label null -> annotation skipped
    }
}
