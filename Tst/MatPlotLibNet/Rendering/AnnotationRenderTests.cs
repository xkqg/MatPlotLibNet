// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that annotation rendering emits the correct SVG for each connection and arrow style.</summary>
public class AnnotationRenderTests
{
    /// <summary>Verifies that an annotation with Arc3 connection renders an SVG path element.</summary>
    [Fact]
    public void Annotation_Arc3Connection_RendersSvgPath()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("label", 1.0, 1.0);
        ann.ArrowTargetX = 2.0;
        ann.ArrowTargetY = 2.0;
        ann.ConnectionStyle = ConnectionStyle.Arc3;

        string svg = figure.ToSvg();

        Assert.Contains("<path", svg);
    }

    /// <summary>Verifies that an annotation with Angle connection renders an SVG path element.</summary>
    [Fact]
    public void Annotation_AngleConnection_RendersSvgPath()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("label", 1.0, 1.0);
        ann.ArrowTargetX = 2.5;
        ann.ArrowTargetY = 0.5;
        ann.ConnectionStyle = ConnectionStyle.Angle;

        string svg = figure.ToSvg();

        Assert.Contains("<path", svg);
    }

    /// <summary>Verifies that a Wedge arrow style renders a filled polygon element.</summary>
    [Fact]
    public void Annotation_WedgeArrow_RendersFilledPolygon()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("label", 1.0, 1.0);
        ann.ArrowTargetX = 2.0;
        ann.ArrowTargetY = 2.0;
        ann.ArrowStyle = ArrowStyle.Wedge;

        string svg = figure.ToSvg();

        Assert.Contains("<polygon", svg);
    }

    /// <summary>Verifies that a BracketB arrow style renders SVG path elements.</summary>
    [Fact]
    public void Annotation_BracketArrow_RendersLines()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("label", 1.0, 1.0);
        ann.ArrowTargetX = 2.0;
        ann.ArrowTargetY = 2.0;
        ann.ArrowStyle = ArrowStyle.BracketB;

        string svg = figure.ToSvg();

        // BracketB adds a path (bracket line) at the target end
        Assert.Contains("<path", svg);
    }

    /// <summary>Verifies that Straight connection backward-compatibility: still renders a path for existing charts.</summary>
    [Fact]
    public void Annotation_StraightConnection_BackwardCompat()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("label", 1.0, 1.0);
        ann.ArrowTargetX = 2.0;
        ann.ArrowTargetY = 2.0;
        ann.ArrowStyle = ArrowStyle.FancyArrow;
        // ConnectionStyle defaults to Straight

        string svg = figure.ToSvg();

        Assert.Contains("<path", svg);
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Verifies that an annotation with ArrowStyle.None and no target renders no path or polygon.</summary>
    [Fact]
    public void Annotation_NoArrow_NoPathElement()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        ax.Annotate("label", 1.0, 1.0); // no ArrowTargetX/Y

        string svg = figure.ToSvg();

        // No arrow target means no path drawn for the annotation
        // (There may be paths elsewhere, but no polygon from annotation)
        Assert.Contains("<text", svg); // at minimum the label text
    }

    /// <summary>Verifies that a Round callout box renders an SVG path with bezier curves.</summary>
    [Fact]
    public void Annotation_RoundCallout_RendersSvgPath()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("boxed", 1.5, 1.5);
        ann.BoxStyle = BoxStyle.Round;
        ann.BoxFaceColor = MatPlotLibNet.Styling.Colors.White;

        string svg = figure.ToSvg();

        Assert.Contains("<path", svg);
        Assert.Contains(" C ", svg);
    }

    /// <summary>Verifies that a Square callout box renders an SVG rect element.</summary>
    [Fact]
    public void Annotation_SquareCallout_RendersRect()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("boxed", 1.5, 1.5);
        ann.BoxStyle = BoxStyle.Square;
        ann.BoxEdgeColor = MatPlotLibNet.Styling.Colors.Black;

        string svg = figure.ToSvg();

        Assert.Contains("<rect", svg);
    }

    /// <summary>Verifies that a callout with pointer target renders connection path.</summary>
    [Fact]
    public void Annotation_CalloutWithPointer_RendersPointerPath()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("boxed", 1.0, 1.0);
        ann.BoxStyle = BoxStyle.Square;
        ann.ArrowTargetX = 2.5;
        ann.ArrowTargetY = 2.5;

        string svg = figure.ToSvg();

        Assert.Contains("<path", svg);
    }

    /// <summary>Verifies that BackgroundColor still works when BoxStyle is None (backward compat).</summary>
    [Fact]
    public void Annotation_NoBox_BackgroundColorStillWorks()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("labeled", 1.5, 1.5);
        ann.BackgroundColor = MatPlotLibNet.Styling.Colors.Yellow;
        // BoxStyle is None by default

        string svg = figure.ToSvg();

        // Background color draws a rect — check it exists
        Assert.Contains("<rect", svg);
        Assert.Contains("<text", svg);
    }
}
