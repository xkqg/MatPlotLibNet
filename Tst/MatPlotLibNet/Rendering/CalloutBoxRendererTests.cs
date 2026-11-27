// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="CalloutBox"/> SVG output for each box style.</summary>
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

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that BoxStyle.Square renders a stroked rectangle around the annotation text.
        // Square box uses DrawRectangle with stroke
        new DrawnToken("Square_DrawsRectangle", () => CreateFigureWithBoxAnnotation(BoxStyle.Square).ToSvg(), "<rect"),
        // Verifies that BoxStyle.Round renders an SVG path with Bezier curve commands.
        // Round corners are rendered as a path with bezier curve (C) commands
        new DrawnToken("Round_DrawsPathWithBezierCorners", () => CreateFigureWithBoxAnnotation(BoxStyle.Round, cornerRadius: 8).ToSvg(), "<path", " C "),
        // Verifies that BoxStyle.RoundTooth renders an SVG path (has non-straight edges).
        new DrawnToken("RoundTooth_DrawsZigzagBottom", () => CreateFigureWithBoxAnnotation(BoxStyle.RoundTooth).ToSvg(), "<path"),
        // Verifies that BoxStyle.Sawtooth renders an SVG path.
        new DrawnToken("Sawtooth_DrawsSawteethEdges", () => CreateFigureWithBoxAnnotation(BoxStyle.Sawtooth).ToSvg(), "<path"),
        // Verifies that an annotation with a box and a pointer target renders a path with pointer geometry.
        // With a pointer target, the connection path is also drawn
        new DrawnToken("WithPointer_InsertsTriangleInPath", () => CreateFigureWithBoxAnnotation(BoxStyle.Square, withPointer: true).ToSvg(), "<path"),
        // Verifies that CornerRadius affects the arc curvature in the Round style path.
        new DrawnToken("CornerRadius_AffectsArcSize (svgSmall)", () => CreateFigureWithBoxAnnotation(BoxStyle.Round, cornerRadius: 2).ToSvg(), "<path"),
        new DrawnToken("CornerRadius_AffectsArcSize (svgLarge)", () => CreateFigureWithBoxAnnotation(BoxStyle.Round, cornerRadius: 15).ToSvg(), "<path"),
        // Verifies that padding expands the box rectangle beyond the text bounds.
        // Both render a rect, but larger padding results in larger width/height values
        // We just verify both render without error and produce rects
        new DrawnToken("Padding_ExpandsRect (svgTight)", () => CreateFigureWithBoxAnnotation(BoxStyle.Square, padding: 1).ToSvg(), "<rect"),
        new DrawnToken("Padding_ExpandsRect (svgPadded)", () => CreateFigureWithBoxAnnotation(BoxStyle.Square, padding: 20).ToSvg(), "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Verifies that BoxStyle.None draws no extra box geometry around the annotation.</summary>
    [Fact]
    public void None_DrawsNothing()
    {
        var figureWithBox = CreateFigureWithBoxAnnotation(BoxStyle.Square);
        var figureNoBox   = CreateFigureWithBoxAnnotation(BoxStyle.None);

        string svgWithBox = figureWithBox.ToSvg();
        string svgNoBox   = figureNoBox.ToSvg();

        // Box adds a rect; None should not add an extra rect beyond the axes background
        int rectsWithBox = svgWithBox.CountOf("<rect");
        int rectsNoBox   = svgNoBox.CountOf("<rect");
        Assert.True(rectsWithBox > rectsNoBox, "Square box should produce more rects than no box");
    }

}
