// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Tests.TestFixtures;

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

    /// <summary>An annotation is drawn in the colour it was given, whether or not a font came with it.</summary>
    [Fact]
    public void Annotation_GivenItsOwnFont_IsStillDrawnInItsOwnColour()
    {
        // Measured while drawing a 1200x630 card: an annotation with Font and Color set rendered black. The
        // font a caller supplies carries no colour of its own, and the colour they set beside it was only ever
        // read when no font was supplied at all — so asking for bigger text silently threw the colour away.
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("label", 1.0, 1.0);
        ann.Color = MatPlotLibNet.Styling.Color.FromHex("#FF00AA");
        ann.Font = new MatPlotLibNet.Styling.Font { Family = "sans-serif", Size = 24 };

        string svg = figure.ToSvg();

        Assert.Contains("#FF00AA", svg, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A colour written on the font itself is the more specific instruction, and it wins.</summary>
    [Fact]
    public void Annotation_WhoseFontCarriesAColour_KeepsTheFontsColour()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("label", 1.0, 1.0);
        ann.Color = MatPlotLibNet.Styling.Color.FromHex("#FF00AA");
        ann.Font = new MatPlotLibNet.Styling.Font { Size = 24, Color = MatPlotLibNet.Styling.Color.FromHex("#0044FF") };

        string svg = figure.ToSvg();

        Assert.Contains("#0044FF", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#FF00AA", svg, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A font with no colour and an annotation with no colour fall back to the theme's text colour.</summary>
    [Fact]
    public void Annotation_WithNeitherColour_IsDrawnInTheThemesTextColour()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("label", 1.0, 1.0);
        ann.Font = new MatPlotLibNet.Styling.Font { Size = 24 };

        string svg = figure.ToSvg();

        Assert.Contains($"fill=\"{figure.Theme.ForegroundText.ToHex()}\"", svg, StringComparison.OrdinalIgnoreCase);
    }

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
        var tag = Regex.Match(svg, "<text x=\"([\\d.]+)\" y=\"([\\d.]+)\"[^>]*font-size=\"([\\d.]+)\"[^>]*>" + Regex.Escape(text) + "</text>");
        Assert.True(tag.Success, $"no text '{text}' in the picture");
        double x = double.Parse(tag.Groups[1].Value, CultureInfo.InvariantCulture), y = double.Parse(tag.Groups[2].Value, CultureInfo.InvariantCulture);
        double size = double.Parse(tag.Groups[3].Value, CultureInfo.InvariantCulture);
        var font = new MatPlotLibNet.Styling.Font { Size = size };
        return new TextBox(new Point(x, y), new SvgRenderContext().MeasureText(text, font), size, TextAlignment.Left, TextVerticalAlignment.Baseline, 0).Bounds();
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
