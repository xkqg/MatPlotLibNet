// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that annotation rendering emits the correct SVG for each connection and arrow style.</summary>
public class AnnotationRenderTests
{
    /// <summary>The markup of a diagonal line with one annotation, as the caller styles it.</summary>
    private static string AnnotationSvg(string text, double x, double y, Action<Annotation> style)
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        style(ax.Annotate(text, x, y));
        return figure.ToSvg();
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // matplotlib draws an annotation in data coordinates only where the axes show the point it annotates (Annotation._check_xy,
        // annotation_clip left at None, Axes.contains_point): its point, or the point its arrow targets when it has one (matplotlib's xy,
        // the text standing at xytext). An annotation whose point lies past the limits is not drawn, its text or its arrow; one placed in
        // fractions of the panel is always drawn.
        new DrawnToken("AnAnnotationWhosePointTheAxesDoNotShow_IsNotDrawn_ItsTextOrItsArrow", () => Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 1.0], [0.0, 1.0]).SetXLim(0, 1).SetYLim(0, 1)
            .Annotate("inside", 0.5, 0.5)
            .Annotate("outside", 5, 5)
            .Annotate("arrow out", 0.5, 0.5, 5, 5)
            .Annotate("arrow in", 5, 5, 0.5, 0.5)
            .Annotate("panel", 1.2, 0.5, a => a.Coordinates = AnnotationCoordinates.AxesFraction)).ToSvg(), ">inside</text>", ">arrow in</text>", ">panel</text>") { Absent = [">outside</text>", ">arrow out</text>"] },
        // Verifies that an annotation with Arc3 connection renders an SVG path element.
        new DrawnToken("Annotation_Arc3Connection_RendersSvgPath", () => AnnotationSvg("label", 1.0, 1.0, ann =>
        {
            ann.ArrowTargetX = 2.0;
            ann.ArrowTargetY = 2.0;
            ann.ConnectionStyle = ConnectionStyle.Arc3;
        }), "<path"),
        // Verifies that an annotation with Angle connection renders an SVG path element.
        new DrawnToken("Annotation_AngleConnection_RendersSvgPath", () => AnnotationSvg("label", 1.0, 1.0, ann =>
        {
            ann.ArrowTargetX = 2.5;
            ann.ArrowTargetY = 0.5;
            ann.ConnectionStyle = ConnectionStyle.Angle;
        }), "<path"),
        // Verifies that a Wedge arrow style renders a filled polygon element.
        new DrawnToken("Annotation_WedgeArrow_RendersFilledPolygon", () => AnnotationSvg("label", 1.0, 1.0, ann =>
        {
            ann.ArrowTargetX = 2.0;
            ann.ArrowTargetY = 2.0;
            ann.ArrowStyle = ArrowStyle.Wedge;
        }), "<polygon"),
        // Verifies that a BracketB arrow style renders SVG path elements.
        // BracketB adds a path (bracket line) at the target end
        new DrawnToken("Annotation_BracketArrow_RendersLines", () => AnnotationSvg("label", 1.0, 1.0, ann =>
        {
            ann.ArrowTargetX = 2.0;
            ann.ArrowTargetY = 2.0;
            ann.ArrowStyle = ArrowStyle.BracketB;
        }), "<path"),
        // Verifies that Straight connection backward-compatibility: still renders a path for existing charts.
        // ConnectionStyle defaults to Straight
        new DrawnToken("Annotation_StraightConnection_BackwardCompat", () => AnnotationSvg("label", 1.0, 1.0, ann =>
        {
            ann.ArrowTargetX = 2.0;
            ann.ArrowTargetY = 2.0;
            ann.ArrowStyle = ArrowStyle.FancyArrow;
        }), "<path", "<polygon"),
        // Verifies that an annotation with ArrowStyle.None and no target renders no path or polygon.
        // No arrow target means no path drawn for the annotation
        // (There may be paths elsewhere, but no polygon from annotation)
        // no ArrowTargetX/Y
        new DrawnToken("Annotation_NoArrow_NoPathElement", () => AnnotationSvg("label", 1.0, 1.0, _ => { }), "<text"),
        // Verifies that a Round callout box renders an SVG path with bezier curves.
        new DrawnToken("Annotation_RoundCallout_RendersSvgPath", () => AnnotationSvg("boxed", 1.5, 1.5, ann =>
        {
            ann.BoxStyle = BoxStyle.Round;
            ann.BoxFaceColor = MatPlotLibNet.Styling.Colors.White;
        }), "<path", " C "),
        // Verifies that a Square callout box renders an SVG rect element.
        new DrawnToken("Annotation_SquareCallout_RendersRect", () => AnnotationSvg("boxed", 1.5, 1.5, ann =>
        {
            ann.BoxStyle = BoxStyle.Square;
            ann.BoxEdgeColor = MatPlotLibNet.Styling.Colors.Black;
        }), "<rect"),
        // Verifies that a callout with pointer target renders connection path.
        new DrawnToken("Annotation_CalloutWithPointer_RendersPointerPath", () => AnnotationSvg("boxed", 1.0, 1.0, ann =>
        {
            ann.BoxStyle = BoxStyle.Square;
            ann.ArrowTargetX = 2.5;
            ann.ArrowTargetY = 2.5;
        }), "<path"),
        // Verifies that BackgroundColor still works when BoxStyle is None (backward compat).
        // Background color draws a rect — check it exists
        // BoxStyle is None by default
        new DrawnToken("Annotation_NoBox_BackgroundColorStillWorks", () => AnnotationSvg("labeled", 1.5, 1.5, ann =>
        {
            ann.BackgroundColor = MatPlotLibNet.Styling.Colors.Yellow;
        }), "<rect", "<text"),
        // An annotation is drawn in the colour it was given, whether or not a font came with it.
        new DrawnToken("Annotation_GivenItsOwnFont_IsStillDrawnInItsOwnColour", () => AnnotationSvg("label", 1.0, 1.0, ann =>
        {
            // Measured while drawing a 1200x630 card: an annotation with Font and Color set rendered black. The
            // font a caller supplies carries no colour of its own, and the colour they set beside it was only ever
            // read when no font was supplied at all — so asking for bigger text silently threw the colour away.
            ann.Color = MatPlotLibNet.Styling.Color.FromHex("#FF00AA");
            ann.Font = new MatPlotLibNet.Styling.Font { Family = "sans-serif", Size = 24 };
        }), "#FF00AA") { Comparison = StringComparison.OrdinalIgnoreCase },
        // A colour written on the font itself is the more specific instruction, and it wins.
        new DrawnToken("Annotation_WhoseFontCarriesAColour_KeepsTheFontsColour", () => AnnotationSvg("label", 1.0, 1.0, ann =>
        {
            ann.Color = MatPlotLibNet.Styling.Color.FromHex("#FF00AA");
            ann.Font = new MatPlotLibNet.Styling.Font { Size = 24, Color = MatPlotLibNet.Styling.Color.FromHex("#0044FF") };
        }), "#0044FF") { Absent = ["#FF00AA"], Comparison = StringComparison.OrdinalIgnoreCase },
        // A font with no colour and an annotation with no colour fall back to the theme's text colour.
        new DrawnToken("Annotation_WithNeitherColour_IsDrawnInTheThemesTextColour", () => AnnotationSvg("label", 1.0, 1.0, ann =>
        {
            ann.Font = new MatPlotLibNet.Styling.Font { Size = 24 };
        }), $"fill=\"{new Figure().Theme.ForegroundText.ToHex()}\"") { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ---- annotations that move out of the way ----------------------------------------------------------------------

    private static string Crowded(bool avoid) => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
        // A legend that stays in the upper right: a Best one would move off the annotations by itself.
        .Plot([0.0, 10.0], [0.0, 10.0], s => s.Label = "line").WithLegend(LegendPosition.UpperRight)
        .Annotate("alpha", 5, 5, a => a.AvoidOverlap = avoid)
        .Annotate("beta label", 5.05, 5.1, a => a.AvoidOverlap = avoid)
        .Annotate("gamma", 9.2, 9.0, a => a.AvoidOverlap = avoid)).ToSvg();

    /// <summary>The box a drawn text stands in, from its element in the picture.</summary>
    private static Rect TextBoxOf(string svg, string text)
    {
        var tag = svg.TextReading(text);
        double size = tag.Number("font-size");
        var font = new MatPlotLibNet.Styling.Font { Size = size };
        return new TextBox(new TextPlacement(new Point(tag.Number("x"), tag.Number("y")), TextAlignment.Left), TextExtent.Of(new SvgRenderContext().MeasureText(text, font), size)).Bounds();
    }

    [Fact]
    public void CrowdedAnnotationsThatStayPut_OverlapEachOtherAndTheLegend()
    {
        string svg = Crowded(avoid: false);
        var legend = svg.LegendFrame()!.Value;

        Assert.True(TextBoxOf(svg, "alpha").Overlaps(TextBoxOf(svg, "beta label")), "alpha and beta label were expected to collide");
        Assert.True(TextBoxOf(svg, "gamma").Overlaps(legend), "gamma was expected to stand on the legend");
    }

    [Fact]
    public void CrowdedAnnotationsThatAskToMove_StandClearOfEachOtherAndOfTheLegend()
    {
        string svg = Crowded(avoid: true);
        var legend = svg.LegendFrame()!.Value;
        var boxes = new[] { "alpha", "beta label", "gamma" }.Select(t => TextBoxOf(svg, t)).ToList();

        for (int i = 0; i < boxes.Count; i++)
        {
            Assert.False(boxes[i].Overlaps(legend), $"annotation {i} stands on the legend");
            for (int j = i + 1; j < boxes.Count; j++)
                Assert.False(boxes[i].Overlaps(boxes[j]), $"annotations {i} and {j} overlap");
        }
    }

    [Fact]
    public void AnAnnotationThatAsksToMoveButOverlapsNothing_IsDrawnExactlyAsWithoutTheAsk()
    {
        string Chart(bool avoid) => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 1.0], [5.0, 5.0]).SetXLim(0, 10).SetYLim(0, 10)
            .Annotate("alone", 2, 2, a => a.AvoidOverlap = avoid)).ToSvg();

        Assert.Equal(Chart(avoid: false), Chart(avoid: true));
    }
}
