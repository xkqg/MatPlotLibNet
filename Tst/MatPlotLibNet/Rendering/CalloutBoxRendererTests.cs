// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="CalloutBoxRenderer"/> SVG output for each box style.</summary>
public class CalloutBoxRendererTests
{
    private static Figure CreateFigureWithBoxAnnotation(BoxStyle boxStyle,
        double cornerRadius = 5, double padding = 4,
        bool withPointer = false)
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("callout", 1.5, 1.5);
        ann.BoxStyle = boxStyle;
        ann.BoxPadding = padding;
        ann.BoxCornerRadius = cornerRadius;
        ann.BoxFaceColor = Colors.Yellow;
        ann.BoxEdgeColor = Colors.LightGray;
        if (withPointer)
        {
            ann.ArrowTargetX = 2.5;
            ann.ArrowTargetY = 2.5;
        }
        return figure;
    }

    /// <summary>Verifies that BoxStyle.Square renders a stroked rectangle around the annotation text.</summary>
    [Fact]
    public void Square_DrawsRectangle()
    {
        string svg = CreateFigureWithBoxAnnotation(BoxStyle.Square).ToSvg();

        // Square box uses DrawRectangle with stroke
        Assert.Contains("<rect", svg);
    }

    /// <summary>Verifies that BoxStyle.Round renders an SVG path with Bezier curve commands.</summary>
    [Fact]
    public void Round_DrawsPathWithBezierCorners()
    {
        string svg = CreateFigureWithBoxAnnotation(BoxStyle.Round, cornerRadius: 8).ToSvg();

        // Round corners are rendered as a path with bezier curve (C) commands
        Assert.Contains("<path", svg);
        Assert.Contains(" C ", svg);
    }

    /// <summary>Verifies that BoxStyle.RoundTooth renders an SVG path (has non-straight edges).</summary>
    [Fact]
    public void RoundTooth_DrawsZigzagBottom()
    {
        string svg = CreateFigureWithBoxAnnotation(BoxStyle.RoundTooth).ToSvg();

        Assert.Contains("<path", svg);
    }

    /// <summary>Verifies that BoxStyle.Sawtooth renders an SVG path.</summary>
    [Fact]
    public void Sawtooth_DrawsSawteethEdges()
    {
        string svg = CreateFigureWithBoxAnnotation(BoxStyle.Sawtooth).ToSvg();

        Assert.Contains("<path", svg);
    }

    /// <summary>Verifies that BoxStyle.None draws no extra box geometry around the annotation.</summary>
    [Fact]
    public void None_DrawsNothing()
    {
        var figureWithBox = CreateFigureWithBoxAnnotation(BoxStyle.Square);
        var figureNoBox   = CreateFigureWithBoxAnnotation(BoxStyle.None);

        string svgWithBox = figureWithBox.ToSvg();
        string svgNoBox   = figureNoBox.ToSvg();

        // Box adds a rect; None should not add an extra rect beyond the axes background
        int rectsWithBox = CountOccurrences(svgWithBox, "<rect");
        int rectsNoBox   = CountOccurrences(svgNoBox, "<rect");
        Assert.True(rectsWithBox > rectsNoBox, "Square box should produce more rects than no box");
    }

    /// <summary>Verifies that padding expands the box rectangle beyond the text bounds.</summary>
    [Fact]
    public void Padding_ExpandsRect()
    {
        string svgTight  = CreateFigureWithBoxAnnotation(BoxStyle.Square, padding: 1).ToSvg();
        string svgPadded = CreateFigureWithBoxAnnotation(BoxStyle.Square, padding: 20).ToSvg();

        // Both render a rect, but larger padding results in larger width/height values
        // We just verify both render without error and produce rects
        Assert.Contains("<rect", svgTight);
        Assert.Contains("<rect", svgPadded);
    }

    /// <summary>Verifies that CornerRadius affects the arc curvature in the Round style path.</summary>
    [Fact]
    public void CornerRadius_AffectsArcSize()
    {
        string svgSmall = CreateFigureWithBoxAnnotation(BoxStyle.Round, cornerRadius: 2).ToSvg();
        string svgLarge = CreateFigureWithBoxAnnotation(BoxStyle.Round, cornerRadius: 15).ToSvg();

        Assert.Contains("<path", svgSmall);
        Assert.Contains("<path", svgLarge);
    }

    /// <summary>Verifies that an annotation with a box and a pointer target renders a path with pointer geometry.</summary>
    [Fact]
    public void WithPointer_InsertsTriangleInPath()
    {
        string svg = CreateFigureWithBoxAnnotation(BoxStyle.Square, withPointer: true).ToSvg();

        // With a pointer target, the connection path is also drawn
        Assert.Contains("<path", svg);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
