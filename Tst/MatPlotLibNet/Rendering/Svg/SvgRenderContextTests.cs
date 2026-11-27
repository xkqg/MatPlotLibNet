// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Packaging;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies <see cref="SvgRenderContext"/> behavior.</summary>
[Xunit.Collection("ChartServicesGlobalState")] // mutates GlyphPathProviderDefault.Current — must not run concurrently with render tests
public class SvgRenderContextTests
{
    // The shapes and the colour the rows below draw with stand above them: a static is read in the order it is written.
    private static readonly Point[] Triangle = [new(0, 0), new(10, 0), new(5, 10)];

    private static readonly PathSegment[] TrianglePath =
        [new MoveToSegment(new Point(0, 0)), new LineToSegment(new Point(10, 0)), new LineToSegment(new Point(5, 10)), new CloseSegment()];

    private static readonly Color HalfRed = new(255, 0, 0, 128);

    /// <summary>What a fresh context has written once <paramref name="draw"/> has drawn into it.</summary>
    private static string Drawn(Action<SvgRenderContext> draw)
    {
        var ctx = new SvgRenderContext();
        draw(ctx);
        return ctx.GetOutput();
    }

    /// <summary>What a fresh context has written once <paramref name="draw"/> has drawn "Hi" at (50, 50) into it, the glyph outlines held at
    /// <paramref name="outlines"/> (<see langword="null"/> writes a text element).</summary>
    private static string DrawnHi(IGlyphPathProvider? outlines, Font font, double rotation = 0)
    {
        using var heldOutlines = GlyphPathProviderDefault.Holding(outlines);
        return Drawn(ctx => ctx.DrawText("Hi", new Point(50, 50), font, TextAlignment.Left, rotation));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that DrawLine produces an SVG line element with correct attributes and color.
        new DrawnToken("DrawLine_ProducesSvgLineElement", () => Drawn(ctx =>
        {
            ctx.DrawLine(new Point(10, 20), new Point(30, 40),
                new StrokeStyle(Colors.Red, 2.0, LineStyle.Solid));
        }), "<line", "x1=", "x2=", "#FF0000"),
        // Verifies that DrawRectangle produces an SVG rect element with the specified fill color.
        new DrawnToken("DrawRectangle_ProducesSvgRectElement", () => Drawn(ctx => ctx.DrawRectangle(new Rect(10, 20, 100, 50), new ShapeStyle(Colors.Blue, null, 0))), "<rect", "#0000FF"),
        // Verifies that DrawCircle produces an SVG circle element.
        new DrawnToken("DrawCircle_ProducesSvgCircleElement", () => Drawn(ctx => ctx.DrawCircle(new Point(50, 50), 10, new ShapeStyle(Colors.Green, null, 0))), "<circle"),
        // Verifies that DrawLines produces an SVG polyline element from multiple points.
        new DrawnToken("DrawLines_ProducesSvgPolylineElement", () => Drawn(ctx =>
        {
            ctx.DrawLines([new Point(0, 0), new Point(10, 10), new Point(20, 5)],
                new StrokeStyle(Colors.Blue, 1.5, LineStyle.Solid));
        }), "<polyline"),
        // Verifies that a dashed line includes a stroke-dasharray attribute.
        new DrawnToken("DashedLine_HasStrokeDasharray", () => Drawn(ctx =>
        {
            ctx.DrawLine(new Point(0, 0), new Point(10, 10),
                new StrokeStyle(Colors.Black, 1, LineStyle.Dashed));
        }), "stroke-dasharray"),
        // Verifies that DrawPolygon with a fill color produces a polygon element.
        new DrawnToken("DrawPolygon_WithFill_ContainsPolygonElement", () => Drawn(ctx => ctx.DrawPolygon([new Point(0, 0), new Point(10, 0), new Point(5, 10)], new ShapeStyle(Colors.Red, null, 0))), "<polygon"),
        // Verifies that DrawPolygon with a stroke color includes a stroke attribute.
        new DrawnToken("DrawPolygon_WithStroke_HasStrokeAttribute", () => Drawn(ctx => ctx.DrawPolygon([new Point(0, 0), new Point(10, 0), new Point(5, 10)], new ShapeStyle(null, Colors.Blue, 2))), "stroke="),
        // Verifies that DrawPolygon with both fill and stroke includes both attributes.
        new DrawnToken("DrawPolygon_WithBothFillAndStroke", () => Drawn(ctx => ctx.DrawPolygon([new Point(0, 0), new Point(10, 0), new Point(5, 10)], new ShapeStyle(Colors.Green, Colors.Black, 1))), "fill=", "stroke="),
        // Verifies that DrawEllipse produces an SVG ellipse element with cx, cy, rx, and ry attributes.
        new DrawnToken("DrawEllipse_ContainsEllipseElement", () => Drawn(ctx => ctx.DrawEllipse(new Rect(10, 20, 100, 50), new ShapeStyle(Colors.Red, null, 0))), "<ellipse", "cx=", "cy=", "rx=", "ry="),
        // Verifies that DrawEllipse with a fill color includes the hex color value.
        new DrawnToken("DrawEllipse_WithFill_HasFillColor", () => Drawn(ctx => ctx.DrawEllipse(new Rect(0, 0, 80, 40), new ShapeStyle(Colors.Blue, null, 0))), "#0000FF"),
        // Verifies that DrawPath with MoveTo and LineTo segments produces M and L commands in the path data.
        new DrawnToken("DrawPath_MoveToLineTo_ContainsMAndLCommands", () => Drawn(ctx =>
        {
            ctx.DrawPath(
                [new MoveToSegment(new Point(0, 0)), new LineToSegment(new Point(10, 20))], new ShapeStyle(null, Colors.Black, 1));
        }), "<path d=\"", "M ", "L "),
        // Verifies that DrawPath with a BezierSegment produces a C command in the path data.
        new DrawnToken("DrawPath_BezierSegment_ContainsCCommand", () => Drawn(ctx =>
        {
            ctx.DrawPath(
                [new MoveToSegment(new Point(0, 0)),
                 new BezierSegment(new Point(5, 10), new Point(15, 10), new Point(20, 0))], new ShapeStyle(null, Colors.Black, 1));
        }), "C "),
        // Verifies that DrawPath with a CloseSegment produces a Z command to close the path.
        new DrawnToken("DrawPath_CloseSegment_ContainsZCommand", () => Drawn(ctx =>
        {
            ctx.DrawPath(
                [new MoveToSegment(new Point(0, 0)),
                 new LineToSegment(new Point(10, 0)),
                 new LineToSegment(new Point(5, 10)),
                 new CloseSegment()], new ShapeStyle(Colors.Red, null, 0));
        }), "Z"),
        // A path stroked dashed carries the dash array of the dashed line, after the stroke and its width.
        new DrawnToken("ADashedPathStroke_WritesStrokeDasharrayAfterTheWidth", () => Drawn(ctx => ctx.DrawPath(TrianglePath, Stroked(LineStyle.Dashed))), "stroke=\"#000000\" stroke-width=\"2\" stroke-dasharray=\"5,2\""),
        // A path stroked solid, the default, writes no dash array: the bytes of every figure drawn before a shape had a line style.
        new DrawnToken("ASolidPathStroke_WritesNoDasharray", () => Drawn(ctx => ctx.DrawPath(TrianglePath, Stroked(LineStyle.Solid))), "stroke=\"#000000\" stroke-width=\"2\"") { Absent = ["stroke-dasharray"] },
        // A path whose stroke has the line style None is filled and not stroked.
        new DrawnToken("ANonePathStroke_WritesNoStroke", () => Drawn(ctx => ctx.DrawPath(TrianglePath, new ShapeStyle(Colors.Red, Colors.Black, 2) { StrokeLineStyle = LineStyle.None })), "fill=\"#FF0000\"") { Absent = ["stroke"] },
        // A polygon stroked dashed carries the dash array of the dashed line, after the stroke and its width.
        new DrawnToken("ADashedPolygonStroke_WritesStrokeDasharrayAfterTheWidth", () => Drawn(ctx => ctx.DrawPolygon(Triangle, Stroked(LineStyle.Dashed))), "stroke=\"#000000\" stroke-width=\"2\" stroke-dasharray=\"5,2\""),
        // A polygon stroked solid writes no dash array.
        new DrawnToken("ASolidPolygonStroke_WritesNoDasharray", () => Drawn(ctx => ctx.DrawPolygon(Triangle, Stroked(LineStyle.Solid)))) { Absent = ["stroke-dasharray"] },
        // A polygon whose stroke has the line style None is filled and not stroked.
        new DrawnToken("ANonePolygonStroke_WritesNoStroke", () => Drawn(ctx => ctx.DrawPolygon(Triangle, new ShapeStyle(Colors.Red, Colors.Black, 2) { StrokeLineStyle = LineStyle.None })), "fill=\"#FF0000\"") { Absent = ["stroke"] },
        // A line style belongs to a stroke: a shape that has no stroke writes no dash array, however it is set.
        new DrawnToken("AShapeWithoutAStroke_WritesNoDasharray_WhateverItsLineStyle", () => Drawn(ctx => ctx.DrawPolygon(Triangle, new ShapeStyle(Colors.Red, null, 2) { StrokeLineStyle = LineStyle.Dashed }))) { Absent = ["stroke-dasharray"] },
        // The view of an axes is a group, and a group drawn in a clip would hold more than marks: it stands outside the clips as every
        // other group does.
        new DrawnToken("TheViewOfAnAxes_StandsOutsideTheClips", () => Drawn(ctx =>
        {
            ctx.PushClip(new Rect(0, 0, 10, 10));
            ((IMarkupRenderContext)ctx).WriteAxesView(1, new Rect(1, 2, 3, 4), null, null);
        }), "<g clip-path=\"url(#clip-0)\">\n</g>\n<g data-axes-index=\"1\""),
        // Verifies that PushClip creates a clipPath definition in the SVG output.
        new DrawnToken("PushClip_CreatesClipPathDef", () => Drawn(ctx => ctx.PushClip(new Rect(0, 0, 100, 100))), "<clipPath"),
        // Verifies that PushClip opens a group element referencing the clip path URL.
        new DrawnToken("PushClip_OpensGroupWithClipPathUrl", () => Drawn(ctx => ctx.PushClip(new Rect(0, 0, 100, 100))), "clip-path=\"url(#clip-"),
        // Verifies that PopClip closes the clipping group element.
        new DrawnToken("PopClip_ClosesGroup", () => Drawn(ctx =>
        {
            ctx.PushClip(new Rect(0, 0, 100, 100));
            ctx.PopClip();
        }), "</g>"),
        // Verifies that SetOpacity creates a group element with an opacity attribute.
        new DrawnToken("SetOpacity_CreatesGroupWithOpacityAttribute", () => Drawn(ctx => ctx.SetOpacity(0.5)), "<g opacity=\""),
        new DrawnToken("GlyphOutline_NoneFromAProviderThatShapes_FallsBackToATextElement", () => ShapedGlyphOutline(null, TextAlignment.Center, out _), "<text") { Absent = ["<path"] },
        new DrawnToken("GlyphOutline_Turned_MovesAlongTheUnturnedBaselineBeforeItTurns", () => GlyphOutline(TextAlignment.Center, 90, out _), " transform=\"translate(50,60) rotate(-90) translate(-20,0)\""),
        // Verifies that bold font weight produces a font-weight="bold" attribute.
        new DrawnToken("DrawText_BoldFont_HasBoldWeight", () => Drawn(ctx =>
        {
            ctx.DrawText("Bold", new Point(50, 50),
                new Font { Weight = FontWeight.Bold }, TextAlignment.Left);
        }), "font-weight=\"bold\""),
        // Verifies that italic font slant produces a font-style="italic" attribute.
        new DrawnToken("DrawText_ItalicFont_HasItalicStyle", () => Drawn(ctx =>
        {
            ctx.DrawText("Italic", new Point(50, 50),
                new Font { Slant = FontSlant.Italic }, TextAlignment.Left);
        }), "font-style=\"italic\""),
        new DrawnToken("DrawText_HebrewRotated_KeepsBothTheRotationAndTheDirection", () => Drawn(ctx => ctx.DrawText("שלום", new Point(50, 50), new Font(), TextAlignment.Center, 90)), "direction=\"rtl\"", "rotate(-90"),
        new DrawnToken("DrawText_Latin_CarriesNoDirectionAttribute", () => Drawn(ctx => ctx.DrawText("Center", new Point(50, 50), new Font(), TextAlignment.Center)), "text-anchor=\"middle\"") { Absent = ["direction="] },
        new DrawnToken("DrawText_LatinWithAnArabicWord_ReadsLeftToRight", () =>
        {
            // The first strong character is Latin, so the paragraph is left-to-right; the browser reorders the
            // Arabic word inside it on its own.
            var ctx = new SvgRenderContext();
            ctx.DrawText("Temp درجة", new Point(50, 50), new Font(), TextAlignment.Left);
            return ctx.GetOutput();
        }, "text-anchor=\"start\"") { Absent = ["direction="] },
        new DrawnToken("DrawText_ArabicWithADigit_ReadsRightToLeft", () => Drawn(ctx => ctx.DrawText("درجة 12", new Point(50, 50), new Font(), TextAlignment.Left)), "direction=\"rtl\""),
        new DrawnToken("DrawText_EmptyString_CarriesNoDirectionAttribute", () => Drawn(ctx => ctx.DrawText("", new Point(50, 50), new Font(), TextAlignment.Left))) { Absent = ["direction="] },
        new DrawnToken("DrawRichText_HebrewWithASuperscript_HasRtlDirectionOnTheTextElement", () => Drawn(ctx =>
        {
            ctx.DrawRichText(MatPlotLibNet.Rendering.MathText.MathTextParser.Parse("שלום$^{2}$"),
                new Point(50, 50), new Font(), TextAlignment.Left);
        }), "<text", "direction=\"rtl\"", "text-anchor=\"end\"", ">2</text>"),
        // A distance given by the camera is written as given, not as the default.
        new DrawnToken("ThreeDSceneGroup_WritesTheDistanceOfTheCamera", () =>
        {
            var ctx = new SvgRenderContext();
            ((IMarkupRenderContext)ctx).Begin3DSceneGroup(0, 0, 7.5, new Rect(0, 0, 1, 1), default);
            return Written(ctx);
        }, " data-distance=\"7.5\""),
        // The class and the tree level asked for the next element land on a rich text too.
        new DrawnToken("RichText_WithASuperscript_CarriesTheWaitingClass", () => Drawn(ctx =>
        {
            ((IMarkupRenderContext)ctx).SetNextElementClass("pane");
            ctx.DrawRichText(MatPlotLibNet.Rendering.MathText.MathTextParser.Parse("x$^{2}$"), new Point(50, 60),
                new Font { Family = "serif", Size = 12 }, TextAlignment.Left);
        }), " class=\"pane\">x</text>"),
        // An opacity is not a pixel: a translucent colour, a group's opacity and a gradient stop keep every digit, so the alpha the
        // browser paints is the alpha of the colour.
        new DrawnToken("AnOpacity_KeepsEveryDigit", () =>
        {
            var ctx = new SvgRenderContext();
            ctx.DrawLine(new Point(0, 0), new Point(1, 1), new StrokeStyle(new Color(0, 0, 0, 128), 1, LineStyle.Solid));
            ctx.SetOpacity(1.0 / 3);
            return Written(ctx);
        }, $" stroke-opacity=\"{Opacity(128)}\"", $"<g opacity=\"{(1.0 / 3).ToString("G", CultureInfo.InvariantCulture)}\">"),
        AViewKeepsEveryDigit(),
        // The alpha of a text colour, as a text element and as glyph outlines: Skia paints the text in that colour's alpha.
        new DrawnToken("ATranslucentTextColour_WritesItsFillOpacity_AsATextElement", () => DrawnHi(null, new Font { Color = HalfRed }),
            $" fill=\"#FF0000\" fill-opacity=\"{Opacity(128)}\""),
        new DrawnToken("ATranslucentTextColour_WritesItsFillOpacity_AsGlyphOutlines", () => DrawnHi(new TestGlyphPaths("M 0 0"), new Font { Color = HalfRed }),
            $"<path d=\"M 0 0\" fill=\"#FF0000\" fill-opacity=\"{Opacity(128)}\" transform="),
        // A glyph source that has an outline writes it as a path, with the font's colour and the rotation; one that has none writes a text.
        new DrawnToken("DrawText_GlyphProvider_NonNullPath_EmitsPathElement", () => DrawnHi(new TestGlyphPaths("M 0 0 L 10 0"), new Font()),
            "<path d=\"M 0 0 L 10 0\"") { Absent = ["<text"] },
        new DrawnToken("DrawText_GlyphProvider_NullPath_FallsBackToTextElement", () => DrawnHi(new TestGlyphPaths((string?)null), new Font()), "<text"),
        new DrawnToken("DrawText_GlyphProvider_FontWithColor_EmitsFillOnPath", () => DrawnHi(new TestGlyphPaths("M 0 0"), new Font { Color = Colors.Red }),
            "fill=\"#FF0000\""),
        new DrawnToken("DrawText_GlyphProvider_WithRotation_EmitsRotateInTransform", () => DrawnHi(new TestGlyphPaths("M 0 0"), new Font(), 90.0), "rotate("),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A view is read back by the browser script to turn the pointer into data, so its numbers keep every digit
    /// (the plot area, the data range, the symlog threshold, the secondary ranges and the camera), while the drawing beside
    /// it is written to two decimals of a pixel.</summary>
    private static DrawnToken AViewKeepsEveryDigit()
    {
        double third = 1.0 / 3, twoThirds = 2.0 / 3;
        string Full(double v) => v.ToString("G", CultureInfo.InvariantCulture);
        var plot = new Rect(10 + third, 20 + twoThirds, 300 + third, 200 + twoThirds);
        var range = new DataTransform(0, third, 0, twoThirds, plot, null, null, 0, third, 0, twoThirds,
            AxisScale.Linear, AxisScale.SymLog, 1.0, third);
        var left = new SecondaryAxisTransform(AxisSide.Left, 0, new DataTransform(0, 10, third, twoThirds, plot, null, null, 0, 10, third, twoThirds,
            AxisScale.Linear, AxisScale.SymLog, 1.0, twoThirds));
        (string Name, double Value)[] attributes =
        [
            ("data-plot-x", plot.X), ("data-plot-y", plot.Y), ("data-plot-w", plot.Width), ("data-plot-h", plot.Height),
            ("data-xmax", third), ("data-ymax", twoThirds), ("data-ylinthresh", third),
            ("data-sec-left-0-ymin", third), ("data-sec-left-0-ymax", twoThirds), ("data-sec-left-0-ylinthresh", twoThirds),
            ("data-elevation", third), ("data-azimuth", twoThirds), ("data-distance", 10 + third),
        ];

        return new DrawnToken("AView_KeepsEveryDigit_WhileTheDrawingIsWrittenToTwoDecimals", () =>
        {
            IRenderContext ctx = new SvgRenderContext();
            ctx.WriteAxesView(0, plot, range, [left]);
            ((IMarkupRenderContext)ctx).Begin3DSceneGroup(third, twoThirds, 10 + third, plot, default);
            ctx.DrawLine(new Point(third, twoThirds), new Point(1 + third, 2 + twoThirds), new StrokeStyle(Colors.Black, 1 + third, LineStyle.Solid));
            return Written((SvgRenderContext)ctx);
        },
        [.. attributes.Select(a => $" {a.Name}=\"{Full(a.Value)}\""),
            "<line x1=\"0.33\" y1=\"0.67\" x2=\"1.33\" y2=\"2.67\" stroke=\"#000000\" stroke-width=\"1.33\""]);
    }

    [Theory]
    [InlineData("Hello", TextAlignment.Center, "<text", "Hello")] // DrawText_ProducesSvgTextElement
    [InlineData("שלום", TextAlignment.Left, "direction=\"rtl\"", "text-anchor=\"end\"")] // DrawText_HebrewLeftAligned_HasRtlDirection_AndAnchorsAtTheEnd
    [InlineData("שלום", TextAlignment.Center, "direction=\"rtl\"", "text-anchor=\"middle\"")] // DrawText_HebrewCenterAligned_HasRtlDirection_AndStaysInTheMiddle
    [InlineData("שלום", TextAlignment.Right, "direction=\"rtl\"", "text-anchor=\"start\"")] // DrawText_HebrewRightAligned_HasRtlDirection_AndAnchorsAtTheStart
    public void DrawText_WritesTheTextAsAnSvgTextElement_AnchoredAndDirectedByItsAlignment(string text, TextAlignment alignment, string element, string attribute)
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText(text, new Point(50, 50), new Font(), alignment);

        string svg = ctx.GetOutput();
        Assert.Contains(element, svg);
        Assert.Contains(attribute, svg);
    }

    // --- The line style of a shape's stroke ---
    // A shape's stroke is dashed by the rule a line's stroke is dashed by (AppendDashArray, the one writer of
    // stroke-dasharray), and a stroke whose style is None is not written. A shape without a line style writes the
    // bytes it always wrote.

    private static ShapeStyle Stroked(LineStyle style) =>
        new(null, Colors.Black, 2) { StrokeLineStyle = style };

    /// <summary>The dotted and dash-dot styles write the pattern a line of that style writes.</summary>
    [Theory]
    [InlineData(LineStyle.Dotted, "1,3")]
    [InlineData(LineStyle.DashDot, "5,2,1,2")]
    public void ADottedOrDashDotPathStroke_WritesThePatternOfALineOfThatStyle(LineStyle style, string pattern)
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPath(TrianglePath, Stroked(style));

        Assert.Contains($"stroke-dasharray=\"{pattern}\"", ctx.GetOutput());
    }

    /// <summary>The dotted and dash-dot styles write the pattern a line of that style writes.</summary>
    [Theory]
    [InlineData(LineStyle.Dotted, "1,3")]
    [InlineData(LineStyle.DashDot, "5,2,1,2")]
    public void ADottedOrDashDotPolygonStroke_WritesThePatternOfALineOfThatStyle(LineStyle style, string pattern)
    {
        var ctx = new SvgRenderContext();
        ctx.DrawPolygon(Triangle, Stroked(style));

        Assert.Contains($"stroke-dasharray=\"{pattern}\"", ctx.GetOutput());
    }

    /// <summary>The circle, the rectangle and the ellipse write their stroke through the same writer as the path and the
    /// polygon, so a dashed one is dashed and a None one is not stroked, whichever shape it is.</summary>
    [Fact]
    public void TheOtherShapes_StrokeByTheSameRule()
    {
        var dashed = new SvgRenderContext();
        dashed.DrawCircle(new Point(5, 5), 4, Stroked(LineStyle.Dashed));
        dashed.DrawRectangle(new Rect(0, 0, 10, 10), Stroked(LineStyle.Dashed));
        dashed.DrawEllipse(new Rect(0, 0, 10, 6), Stroked(LineStyle.Dashed));

        var none = new SvgRenderContext();
        none.DrawCircle(new Point(5, 5), 4, new ShapeStyle(Colors.Red, Colors.Black, 2) { StrokeLineStyle = LineStyle.None });
        none.DrawRectangle(new Rect(0, 0, 10, 10), new ShapeStyle(Colors.Red, Colors.Black, 2) { StrokeLineStyle = LineStyle.None });
        none.DrawEllipse(new Rect(0, 0, 10, 6), new ShapeStyle(Colors.Red, Colors.Black, 2) { StrokeLineStyle = LineStyle.None });

        Assert.Equal(3, dashed.GetOutput().Split("stroke-dasharray=\"5,2\"").Length - 1);
        Assert.DoesNotContain("stroke", none.GetOutput());
    }

    /// <summary>A shape with a gradient and a hatch is two elements. The lower one is the shape with its outline and the
    /// upper one the hatch alone, so the dashes are written once, on the outline.</summary>
    [Fact]
    public void AGradientAndHatchedShape_DashesItsOutlineOnce()
    {
        var gradient = new LinearGradient(new Point(0, 0), new Point(10, 0), Colors.Red, Colors.Blue);
        var shape = new ShapeStyle(Colors.Red, Colors.Black, 2)
        {
            Gradient = gradient,
            Hatch = HatchPattern.Cross,
            StrokeLineStyle = LineStyle.Dashed,
        };

        var ctx = new SvgRenderContext();
        ctx.DrawPolygon(Triangle, shape);

        string svg = ctx.GetOutput();
        Assert.Equal(2, svg.Split("<polygon ").Length - 1);
        Assert.Equal(1, svg.Split("stroke-dasharray=\"5,2\"").Length - 1);
    }

    // --- The alpha of a stroke colour ---
    // A hex colour carries no alpha, so the SVG writes a translucent stroke's alpha as stroke-opacity, the one rule a shape's
    // outline already followed. Skia paints a stroke's alpha; before this a line or polyline in a translucent colour (the minor
    // grid at half the grid's alpha, a residual's dashed zero line) was opaque in the SVG and translucent in the PNG.

    private static string Opacity(byte alpha) => (alpha / 255.0).ToString("G", CultureInfo.InvariantCulture);

    [Fact]
    public void ATranslucentLine_WritesItsStrokeOpacity()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLine(new Point(0, 0), new Point(10, 10), new StrokeStyle(HalfRed, 2, LineStyle.Solid));

        Assert.Equal($"<line x1=\"0\" y1=\"0\" x2=\"10\" y2=\"10\" stroke=\"#FF0000\" stroke-width=\"2\" stroke-opacity=\"{Opacity(128)}\" />",
            ctx.GetOutput().TrimEnd());
    }

    [Fact]
    public void ATranslucentPolyline_WritesItsStrokeOpacity()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLines([new Point(0, 0), new Point(10, 10), new Point(20, 5)], new StrokeStyle(new Color(0, 0, 255, 64), 1.5, LineStyle.Solid));

        Assert.Equal($"<polyline points=\"0,0 10,10 20,5\" fill=\"none\" stroke=\"#0000FF\" stroke-width=\"1.5\" stroke-opacity=\"{Opacity(64)}\" />",
            ctx.GetOutput().TrimEnd());
    }

    /// <summary>A translucent dashed line writes its opacity after its width and before its dashes, the order of a shape's
    /// outline.</summary>
    [Fact]
    public void ATranslucentDashedLine_WritesItsOpacityBeforeItsDashes()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLine(new Point(0, 0), new Point(10, 0), new StrokeStyle(HalfRed, 1, LineStyle.Dashed));
        ctx.DrawLines([new Point(0, 0), new Point(10, 10)], new StrokeStyle(HalfRed, 1, LineStyle.Dashed));

        Assert.Equal(2, ctx.GetOutput().Split($"stroke=\"#FF0000\" stroke-width=\"1\" stroke-opacity=\"{Opacity(128)}\" stroke-dasharray=\"5,2\"").Length - 1);
    }

    /// <summary>An opaque line and polyline write the bytes they always wrote: no new attribute.</summary>
    [Fact]
    public void AnOpaqueLineAndPolyline_WriteTheBytesTheyAlwaysWrote()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawLine(new Point(0, 0), new Point(10, 10), new StrokeStyle(Colors.Red, 2, LineStyle.Solid));
        ctx.DrawLines([new Point(0, 0), new Point(10, 10)], new StrokeStyle(Colors.Blue, 1.5, LineStyle.Dashed));

        string[] written = SourceFile.ElementLinesOf(ctx.GetOutput());
        Assert.Equal(
            new[]
            {
                "<line x1=\"0\" y1=\"0\" x2=\"10\" y2=\"10\" stroke=\"#FF0000\" stroke-width=\"2\" />",
                "<polyline points=\"0,0 10,10\" fill=\"none\" stroke=\"#0000FF\" stroke-width=\"1.5\" stroke-dasharray=\"5,2\" />",
            },
            written);
    }

    // --- The alpha of a text colour ---
    // The same root: a text's colour is written as a hex fill, and Skia paints the text in that colour's alpha.

    [Fact]
    public void ATranslucentTextColour_WritesItsFillOpacity_AsRichText()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawRichText(MatPlotLibNet.Rendering.MathText.MathTextParser.Parse("x$^{2}$"), new Point(50, 50),
            new Font { Color = HalfRed }, TextAlignment.Left);

        string svg = ctx.GetOutput();
        Assert.Equal(2, svg.Split($" fill=\"#FF0000\" fill-opacity=\"{Opacity(128)}\">").Length - 1); // each run of the label
    }

    [Fact]
    public void AnOpaqueTextColour_WritesNoFillOpacity()
    {
        var ctx = new SvgRenderContext();
        using (GlyphPathProviderDefault.Holding(null))
        {
            ctx.DrawText("Hi", new Point(50, 50), new Font { Color = Colors.Red }, TextAlignment.Left);
            ctx.DrawRichText(MatPlotLibNet.Rendering.MathText.MathTextParser.Parse("x$^{2}$"), new Point(50, 50),
                new Font { Color = Colors.Red }, TextAlignment.Left);
        }

        using (GlyphPathProviderDefault.Holding(new TestGlyphPaths("M 0 0"))) ctx.DrawText("Hi", new Point(50, 50), new Font { Color = Colors.Red }, TextAlignment.Left);

        string svg = ctx.GetOutput();
        Assert.Equal(4, svg.Split("fill=\"#FF0000\"").Length - 1); // the rich text is two runs, each a text in the colour
        Assert.DoesNotContain("opacity", svg);
    }

    /// <summary>A text drawn with its depth in a tree carries it, and the text after it does not.</summary>
    [Fact]
    public void ATextWithALevel_CarriesItOnThatTextAlone()
    {
        using var heldOutlines = GlyphPathProviderDefault.Holding(null);
        var ctx = new SvgRenderContext();

        ctx.DrawTextWithLevel("a", new Point(0, 0), new Font(), 2);
        ctx.DrawText("b", new Point(0, 0), new Font(), TextAlignment.Left);

        string[] texts = SourceFile.ElementLinesOf(ctx.GetOutput());
        Assert.EndsWith(" aria-level=\"2\">a</text>", texts[0]);
        Assert.DoesNotContain("aria-level", texts[1]);
    }

    /// <summary>The surface answers what the figure asked the subplot drawn into it to write for the browser.</summary>
    [Fact]
    public void TheSurface_AnswersWhatItWasAskedToWriteForTheBrowser()
    {
        var emit = new SvgEmit(null, true, false, 3, false);
        var ctx = new SvgRenderContext(SvgIdScope.ForSubplot(1)) { Emit = emit };

        Assert.Equal(emit, ((IMarkupRenderContext)ctx).Emit);
    }

    // --- MeasureText ---

    /// <summary>Verifies that MeasureText returns a non-zero width and height for non-empty text.</summary>
    [Fact]
    public void MeasureText_ReturnsNonZeroSize()
    {
        var ctx = new SvgRenderContext();
        var size = ctx.MeasureText("Hello", new Font());

        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    /// <summary>Verifies that MeasureText returns zero width for an empty string.</summary>
    [Fact]
    public void MeasureText_EmptyString_ReturnsZeroWidth()
    {
        var ctx = new SvgRenderContext();
        var size = ctx.MeasureText("", new Font());

        Assert.Equal(0, size.Width);
    }

    // ── Glyph outlines: the tests below hold the glyph source at a stub for their scope (GlyphPathProviderDefault.Holding) ──

    /// <summary>EmitGlyphPath: a Center or a Right alignment emits a second translate() for the align offset: one for the
    /// position, one for the offset.</summary>
    [Theory]
    [InlineData(TextAlignment.Center)]
    [InlineData(TextAlignment.Right)]
    public void DrawText_GlyphProvider_CenterOrRightAlignment_EmitsAlignTranslate(TextAlignment alignment)
    {
        using var heldOutlines = GlyphPathProviderDefault.Holding(new TestGlyphPaths("M 0 0"));
        var ctx = new SvgRenderContext();
        ctx.DrawText("Hi", new Point(50, 50), new Font { Size = 12 }, alignment);
        string svg = ctx.GetOutput();
        int translateCount = 0;
        int idx = 0;
        while ((idx = svg.IndexOf("translate(", idx, StringComparison.Ordinal)) >= 0)
        { translateCount++; idx++; }
        Assert.True(translateCount >= 2, $"Expected ≥2 translate() in transform; got {translateCount}. SVG={svg}");
    }

    // --- Glyph outlines: where the text starts is the alignment's StartOffset, and a left text is never measured ---

    private static string GlyphOutline(TextAlignment alignment, double rotation, out int measured)
    {
        var metrics = new TestFontMetrics((_, _) => new(40, 10));
        using var heldOutlines = GlyphPathProviderDefault.Holding(new TestGlyphPaths("M 0 0"));
        using var heldMetrics = FontMetricsDefault.Holding(metrics);
        var ctx = new SvgRenderContext();
        ctx.DrawText("Hi", new Point(50, 60), new Font { Size = 12 }, alignment, rotation);
        measured = metrics.Calls;
        return ctx.GetOutput();
    }

    [Theory]
    [InlineData(TextAlignment.Left, 0, " transform=\"translate(50,60)\"")] // GlyphOutline_LeftAligned_IsNeverMeasured_AndCarriesNoOffset
    [InlineData(TextAlignment.Center, 1, " transform=\"translate(50,60) translate(-20,0)\"")] // GlyphOutline_Centred_StartsHalfTheMeasuredWidthBeforeTheAnchor
    [InlineData(TextAlignment.Right, 1, " transform=\"translate(50,60) translate(-40,0)\"")] // GlyphOutline_RightAligned_EndsAtTheAnchor
    public void GlyphOutline_StartsTheMeasuredShareOfItsWidthBeforeTheAnchor(TextAlignment alignment, int measures, string transform)
    {
        string svg = GlyphOutline(alignment, 0, out int measured);

        Assert.Equal(measures, measured);
        Assert.Contains(transform, svg);
    }

    /// <summary>A provider that hands out the advance of each outline with it, as the Skia one does from its shaping.</summary>
    private static TestShapedGlyphPaths ShapedStubGlyphProvider(string? pathData, double advance) =>
        new(_ => pathData, (_, _) => pathData is null ? null : new ShapedOutline(pathData, advance));

    private static string ShapedGlyphOutline(string? pathData, TextAlignment alignment, out int measured)
    {
        var metrics = new TestFontMetrics((_, _) => new(40, 10));
        using var heldOutlines = GlyphPathProviderDefault.Holding(ShapedStubGlyphProvider(pathData, 30));
        using var heldMetrics = FontMetricsDefault.Holding(metrics);
        var ctx = new SvgRenderContext();
        ctx.DrawText("Hi", new Point(50, 60), new Font { Size = 12 }, alignment);
        measured = metrics.Calls;
        return ctx.GetOutput();
    }

    [Theory]
    [InlineData(TextAlignment.Center, "<path d=\"M 0 0\" transform=\"translate(50,60) translate(-15,0)\"")]
    [InlineData(TextAlignment.Right, " transform=\"translate(50,60) translate(-30,0)\"")]
    public void GlyphOutline_Aligned_TakesTheAdvanceTheProviderShapedItWith_AndIsNotMeasuredAgain(TextAlignment alignment, string expected)
    {
        string svg = ShapedGlyphOutline("M 0 0", alignment, out int measured);

        Assert.Equal(0, measured);
        Assert.Contains(expected, svg);
    }

    [Theory]
    [InlineData("Center", TextAlignment.Center, "text-anchor=\"middle\"")] // DrawText_CenterAlignment_HasMiddleAnchor
    [InlineData("Right", TextAlignment.Right, "text-anchor=\"end\"")] // DrawText_RightAlignment_HasEndAnchor
    [InlineData("<Test&>", TextAlignment.Left, "&lt;Test&amp;&gt;")] // DrawText_SpecialChars_AreEscaped
    public void DrawText_AnAlignment_HasItsAnchor_AndTheTextIsEscaped(string text, TextAlignment alignment, string written)
    {
        var ctx = new SvgRenderContext();
        ctx.DrawText(text, new Point(50, 50), new Font(), alignment);

        string svg = ctx.GetOutput();
        Assert.Contains(written, svg);
    }

    // ── Reading direction (1.17.0) ───────────────────────────────────────────
    // Seven of the fourteen packages never load the Skia backend, so the <text> element is their only text
    // path. A right-to-left label needs the browser told its paragraph direction; the alignment enum stays
    // geometric, which means the anchor keyword flips with the direction.

    /// <summary>The view a server-interaction axes writes numbers the Y axes PAST the primary, per side:
    /// <c>data-sec-left-0-ymin</c>/<c>-ymax</c> is the range of the first OUTWARD left axis, and the primary Y axis, which the
    /// left side also holds, keeps the plain <c>data-ymin</c>/<c>data-ymax</c> and is never a <c>data-sec-left-*</c> entry.
    /// The script sends that index back in a secondary range, so the numbering is a contract an open tab keeps across a
    /// redeploy.</summary>
    [Fact]
    public void DataSecLeftZero_IsTheFirstOutwardLeftSlotsRange()
    {
        var svg = Plt.Create().WithSize(600, 500).WithServerInteraction("c", s => s.EnableZoom())
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [10.0, 20, 30]).SetYLim(0, 100)
                .WithAddRightYAxis(y2 => y2.SetYLim(0, 500).Plot([1.0, 2, 3], [100.0, 300, 200]))
                .WithAddLeftYAxis(left => left.SetYLim(-7, 7).Plot([1.0, 2, 3], [1.0, -2, 3])))
            .ToSvg();

        var view = svg.Xml().Descendants().Single(e => e.Attribute("data-axes-index") is not null);
        double Number(string name)
        {
            var attribute = view.Attribute(name);
            Assert.True(attribute is not null, $"the axes' view carries no {name}");
            return double.Parse(attribute!.Value, CultureInfo.InvariantCulture);
        }

        Assert.Equal(-7, Number("data-sec-left-0-ymin"));
        Assert.Equal(7, Number("data-sec-left-0-ymax"));
        Assert.Equal("1", (string?)view.Attribute("data-sec-left-count"));
        Assert.Null(view.Attribute("data-sec-left-1-ymin"));
        Assert.Equal(0, Number("data-sec-right-0-ymin"));
        Assert.Equal(500, Number("data-sec-right-0-ymax"));
        Assert.Equal("1", (string?)view.Attribute("data-sec-right-count"));
        Assert.Equal(0, Number("data-ymin"));
        Assert.Equal(100, Number("data-ymax"));
    }

    /// <summary>The attributes an axes' view carries number the Y axes PAST the primary, per side, exactly as the published
    /// 1.23.0 numbered them: a browser tab that still holds an older SVG sends those indices back after a redeploy, so a
    /// number may never change its meaning. The four-axes figure of the 1.23.0 fixture drawn by today's code carries the
    /// attribute names and values 1.23.0 wrote (<c>v1.23.0-four-axes-view.json</c>, everything the view holds apart from the
    /// plot area, which moves with the layout), and a figure with two axes out on each side numbers them 0 and 1 per side,
    /// the primary Y axis being none of them.
    /// <para>To reproduce the fixture, run the figure below with the 1.23.0 package from nuget.org (that package's own
    /// verbs for the first right axis and for the axes stacked past it, as in the recipe of
    /// <c>YAxisContainerWireCompatTests</c>, in place of the container verbs, <c>WithServerInteraction("c", s =&gt;
    /// s.EnableZoom())</c> added) and print the <c>data-*</c> attributes of the <c>&lt;g data-axes-index="0"&gt;</c> group
    /// other than <c>data-plot-*</c>.</para></summary>
    [Fact]
    public void DataSecAttributes_AreNumberedOutwardOnly()
    {
        var published = Plt.Create().WithTitle("Four Y axes").WithSize(900, 500)
            .WithServerInteraction("c", s => s.EnableZoom())
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 1, 2, 3], [1.0, 3, 2, 4])
                .WithAddRightYAxis(y => y.Plot([0.0, 1, 2, 3], [10.0, 30, 20, 40]))
                .WithAddRightYAxis(y => y.Plot([0.0, 1, 2, 3], [100.0, 300, 200, 400]))
                .WithAddLeftYAxis(y => y.Plot([0.0, 1, 2, 3], [0.5, 1.5, 1.0, 2.0])))
            .ToSvg().Xml();

        var written = AxesView(published).Attributes()
            .Where(a => a.Name.LocalName.StartsWith("data-", StringComparison.Ordinal) && !a.Name.LocalName.StartsWith("data-plot-", StringComparison.Ordinal)
                        && a.Name.LocalName != "data-axes-index")
            .Select(a => KeyValuePair.Create(a.Name.LocalName, a.Value))
            .ToList();
        var fixture = System.Text.Json.Nodes.JsonNode.Parse(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Serialization", "Compat", "v1.23.0-four-axes-view.json")))!
            .AsObject()
            .Select(p => KeyValuePair.Create(p.Key, (string)p.Value!))
            .ToList();
        Assert.Equal(fixture, written);

        var stacked = AxesView(Plt.Create().WithSize(700, 500).WithServerInteraction("c", s => s.EnableZoom())
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [10.0, 20, 30]).SetYLim(0, 100)
                .WithAddRightYAxis(y => y.SetYLim(0, 500).Plot([1.0, 2, 3], [100.0, 300, 200]))
                .WithAddRightYAxis(y => y.SetYLim(0, 5000).Plot([1.0, 2, 3], [1000.0, 3000, 2000]))
                .WithAddLeftYAxis(y => y.SetYLim(-7, 7).Plot([1.0, 2, 3], [1.0, -2, 3]))
                .WithAddLeftYAxis(y => y.SetYLim(-70, 70).Plot([1.0, 2, 3], [10.0, -20, 30])))
            .ToSvg().Xml());

        void AssertRange(string prefix, string min, string max)
        {
            Assert.Equal(min, (string?)stacked.Attribute(prefix + "ymin"));
            Assert.Equal(max, (string?)stacked.Attribute(prefix + "ymax"));
        }

        AssertRange("data-sec-left-0-", "-7", "7");
        AssertRange("data-sec-left-1-", "-70", "70");
        AssertRange("data-sec-right-0-", "0", "500");
        AssertRange("data-sec-right-1-", "0", "5000");
        AssertRange("data-", "0", "100");
        Assert.Equal("2", (string?)stacked.Attribute("data-sec-left-count"));
        Assert.Equal("2", (string?)stacked.Attribute("data-sec-right-count"));
        Assert.DoesNotContain(stacked.Attributes(), a => a.Name.LocalName.StartsWith("data-sec-left-2-", StringComparison.Ordinal)
                                                          || a.Name.LocalName.StartsWith("data-sec-right-2-", StringComparison.Ordinal));
    }

    // --- Bytes the writer must keep: pinned before the number writer, the group openers and the rich-text opener are reworked ---

    private static string Written(SvgRenderContext ctx) => SourceFile.LineFeedTextOf(ctx.GetOutput());

    private static SecondaryAxisTransform SecondaryOn(AxisScale scale, double linthresh) =>
        new(AxisSide.Right, 0, new DataTransform(0, 10, 0, 3, new Rect(10, 20, 300, 200), null, null, 0, 10, 0, 3,
            AxisScale.Linear, scale, 1.0, linthresh));

    /// <summary>A Y axis stacked past the primary one that is drawn on a log scale writes its scale name beside its range, and
    /// no linear threshold: <c>data-sec-right-0-yscale="log"</c>. A script that inverts the pointer reads that name.</summary>
    [Fact]
    public void ASecondaryLogYAxis_WritesItsRangeAndItsScaleName()
    {
        IRenderContext ctx = new SvgRenderContext();

        ctx.WriteAxesView(2, new Rect(10, 20, 300, 200), null, [SecondaryOn(AxisScale.Log, 1.0)]);

        Assert.Equal("<g data-axes-index=\"2\" data-plot-x=\"10\" data-plot-y=\"20\" data-plot-w=\"300\" data-plot-h=\"200\""
                     + " data-sec-right-0-ymin=\"0\" data-sec-right-0-ymax=\"3\" data-sec-right-0-yscale=\"log\""
                     + " data-sec-right-count=\"1\" />\n", Written((SvgRenderContext)ctx));
    }

    /// <summary>A symlog Y axis past the primary one also writes the width of its linear stretch, <c>ylinthresh</c>, so the
    /// script can invert the same curve; the left side counts apart from the right.</summary>
    [Fact]
    public void ASecondarySymlogYAxis_WritesItsScaleNameAndItsLinearThreshold()
    {
        IRenderContext ctx = new SvgRenderContext();
        var left = new SecondaryAxisTransform(AxisSide.Left, 3, SecondaryOn(AxisScale.SymLog, 0.25).Transform);

        ctx.WriteAxesView(0, new Rect(10, 20, 300, 200), null, [left]);

        Assert.Equal("<g data-axes-index=\"0\" data-plot-x=\"10\" data-plot-y=\"20\" data-plot-w=\"300\" data-plot-h=\"200\""
                     + " data-sec-left-3-ymin=\"0\" data-sec-left-3-ymax=\"3\" data-sec-left-3-yscale=\"symlog\""
                     + " data-sec-left-3-ylinthresh=\"0.25\" data-sec-left-count=\"1\" />\n", Written((SvgRenderContext)ctx));
    }

    /// <summary>The camera of a 3-D scene is written in a fixed order: the class, the two angles, which side of each axis is at
    /// the back, the distance (10 when none is given) and the four plot-area numbers.</summary>
    [Fact]
    public void ThreeDSceneGroup_WritesItsAttributesInAFixedOrder()
    {
        var ctx = new SvgRenderContext();
        var faces = new CubeFaceSelection(new CubePlane(CubeAxis.X, CubeSide.Max), new CubePlane(CubeAxis.Y, CubeSide.Min),
            new CubePlane(CubeAxis.Z, CubeSide.Max));

        Assert.True(((IMarkupRenderContext)ctx).Begin3DSceneGroup(30, -60, null, new Rect(10, 20, 300, 200), faces));

        Assert.Equal("<g class=\"mpl-3d-scene\" data-elevation=\"30\" data-azimuth=\"-60\" data-faces=\"101\" data-distance=\"10\""
                     + " data-plot-x=\"10\" data-plot-y=\"20\" data-plot-w=\"300\" data-plot-h=\"200\">\n", Written(ctx));
    }

    /// <summary>The three tiers of a 3-D scene open as a group with the tier's class and close with a plain end tag.</summary>
    [Fact]
    public void ThreeDSubgroup_OpensWithItsClassAndClosesPlain()
    {
        var ctx = new SvgRenderContext();
        var markup = (IMarkupRenderContext)ctx;

        markup.Begin3DSubgroup("mpl-3d-data");
        markup.End3DSubgroup();

        Assert.Equal("<g class=\"mpl-3d-data\">\n</g>\n", Written(ctx));
    }

    /// <summary>The opening tag of a rich text with a superscript carries, in this order: position, family, size, anchor, the
    /// rotation (negated, about the position), italic, bold and the fill; the script follows as a text of its own.</summary>
    [Fact]
    public void RichText_OpensItsTextElementWithTheFontRotationAndFillInAFixedOrder()
    {
        var ctx = new SvgRenderContext();
        var font = new Font { Family = "serif", Size = 12, Slant = FontSlant.Italic, Weight = FontWeight.Bold, Color = Colors.Blue };

        ctx.DrawRichText(MatPlotLibNet.Rendering.MathText.MathTextParser.Parse("x$^{2}$"), new Point(50, 60), font, TextAlignment.Left, 30);

        Assert.StartsWith("<text x=\"50\" y=\"60\" font-family=\"serif\" font-size=\"12\" text-anchor=\"start\""
                          + " transform=\"rotate(-30,50,60)\" font-style=\"italic\" font-weight=\"bold\" fill=\"#0000FF\">x</text>",
            Written(ctx));
    }

    /// <summary>A right-to-left run adds its direction after the anchor, which then names the other edge; painted right to left, the
    /// superscript stands first.</summary>
    [Fact]
    public void RichText_InARightToLeftScript_WritesItsDirectionAfterTheAnchor()
    {
        var ctx = new SvgRenderContext();

        ctx.DrawRichText(MatPlotLibNet.Rendering.MathText.MathTextParser.Parse("שלום$^{2}$"), new Point(50, 60),
            new Font { Family = "serif", Size = 12 }, TextAlignment.Left);

        Assert.Contains(" y=\"60\" font-family=\"serif\" font-size=\"12\" text-anchor=\"end\" direction=\"rtl\">\u05E9\u05DC\u05D5\u05DD</text>",
            Written(ctx));
        Assert.True(Written(ctx).IndexOf(">2</text>", StringComparison.Ordinal) < Written(ctx).IndexOf("\u05E9", StringComparison.Ordinal));
    }

    // --- Bytes the group, link and path writers must keep: pinned before they are reduced to one writer each ---

    /// <summary>A hyperlink opens with its target, its cursor and, when given, its accessible name and its expanded state; a
    /// name or target with markup characters is escaped.</summary>
    [Fact]
    public void Hyperlink_WritesItsTargetNameAndExpandedState_InAFixedOrder()
    {
        var ctx = new SvgRenderContext();

        ctx.BeginHyperlink("/x?a=1&b=2", "Go & see", expanded: true);
        ctx.BeginHyperlink("/y", null);
        ctx.BeginHyperlink("/z", "", expanded: false);
        ctx.EndHyperlink();

        Assert.Equal("<a href=\"/x?a=1&amp;b=2\" style=\"cursor:pointer\" aria-label=\"Go &amp; see\" aria-expanded=\"true\">\n"
                     + "<a href=\"/y\" style=\"cursor:pointer\">\n"
                     + "<a href=\"/z\" style=\"cursor:pointer\" aria-expanded=\"false\">\n"
                     + "</a>\n", Written(ctx));
    }

    /// <summary>A legend entry's group carries its index and cursor, and its accessible name only when it has one.</summary>
    [Fact]
    public void LegendItemGroup_WritesItsIndexCursorAndName_WhenItHasOne()
    {
        var ctx = new SvgRenderContext();
        var markup = (IMarkupRenderContext)ctx;

        Assert.True(markup.BeginLegendItemGroup(4, "Series <a>"));
        Assert.True(markup.BeginLegendItemGroup(5, null));
        Assert.True(markup.BeginLegendItemGroup(6, ""));

        Assert.Equal("<g data-legend-index=\"4\" style=\"cursor:pointer\" aria-label=\"Series &lt;a&gt;\">\n"
                     + "<g data-legend-index=\"5\" style=\"cursor:pointer\">\n"
                     + "<g data-legend-index=\"6\" style=\"cursor:pointer\">\n", Written(ctx));
    }

    /// <summary>A series group of an interactive figure carries its slot, its accessible name when it has one and, when the data
    /// cursor is on, the view it was drawn with; a labelled series of a plain figure gets an accessible group, an unlabelled one
    /// none.</summary>
    [Fact]
    public void SeriesGroup_WritesItsSlotNameAndView_ByTheFigureInteraction()
    {
        var plot = new Rect(10, 20, 300, 200);
        var view = new DataTransform(0, 10, 0, 3, plot, null, null, 0, 10, 0, 3, AxisScale.Linear, AxisScale.SymLog, 1.0, 0.25);
        var interactive = new SvgEmit(null, true, true, null, false);
        var ctx = new SvgRenderContext();
        var markup = (IMarkupRenderContext)ctx;

        Assert.True(markup.OpenSeriesGroup(interactive, "Rev <q>", 2, view));
        Assert.True(markup.OpenSeriesGroup(interactive with { DataCursorMapping = false }, null, 3, view));
        Assert.True(markup.OpenSeriesGroup(interactive, "", 4, null));
        Assert.True(markup.OpenSeriesGroup(default, "Plain", 5, view));
        Assert.False(markup.OpenSeriesGroup(default, null, 6, view));

        Assert.Equal("<g class=\"series\" data-series-index=\"2\" aria-label=\"Rev &lt;q&gt;\""
                     + " data-plot-x=\"10\" data-plot-y=\"20\" data-plot-w=\"300\" data-plot-h=\"200\""
                     + " data-xmin=\"0\" data-xmax=\"10\" data-ymin=\"0\" data-ymax=\"3\" data-yscale=\"symlog\" data-ylinthresh=\"0.25\">\n"
                     + "<g class=\"series\" data-series-index=\"3\">\n"
                     + "<g class=\"series\" data-series-index=\"4\">\n"
                     + "<g class=\"series\" aria-label=\"Plain\">\n", Written(ctx));
    }

    /// <summary>Every group closes with the same plain end tag, whichever opened it: a clip, an opacity, a group, a tooltip.</summary>
    [Fact]
    public void EveryGroupCloser_WritesTheSamePlainEndTag()
    {
        var ctx = new SvgRenderContext();
        var markup = (IMarkupRenderContext)ctx;

        ctx.PushClip(new Rect(0, 0, 10, 10));
        ctx.PopClip();
        ctx.SetOpacity(0.5);
        ctx.SetOpacity(1.0);
        ctx.BeginGroup("g1");
        ctx.EndGroup();
        markup.BeginTooltipGroup("tip & text");
        markup.EndTooltipGroup();

        Assert.Equal("<defs><clipPath id=\"clip-0\"><rect x=\"0\" y=\"0\" width=\"10\" height=\"10\" /></clipPath></defs>\n"
                     + "<g clip-path=\"url(#clip-0)\">\n</g>\n"
                     + "<g opacity=\"0.5\">\n</g>\n"
                     + "<g class=\"g1\">\n</g>\n"
                     + "<g><title>tip &amp; text</title>\n</g>\n", Written(ctx));
    }

    /// <summary>A text drawn as a <c>&lt;text&gt;</c> element ends its opening tag with the fit-to-width length and then the waiting
    /// attributes, in that order, and uses them up.</summary>
    [Fact]
    public void ATextElement_WritesItsTextLengthThenTheWaitingData_AndUsesThemUp()
    {
        using var heldOutlines = GlyphPathProviderDefault.Holding(null);
        var ctx = new SvgRenderContext();
        var font = new Font { Family = "serif", Size = 12, Color = Colors.Blue };
        ctx.SetNextElementData("k", "v");
        ctx.SetNextTextLength(20);
        ctx.DrawText("Hi", new Point(50, 60), font, TextAlignment.Left);
        ctx.DrawText("Lo", new Point(50, 60), font, TextAlignment.Left);

        string[] written = SourceFile.ElementLinesOf(ctx.GetOutput());
        Assert.Equal("<text x=\"50\" y=\"60\" font-family=\"serif\" font-size=\"12\" text-anchor=\"start\" fill=\"#0000FF\""
                     + " textLength=\"20\" lengthAdjust=\"spacingAndGlyphs\" data-k=\"v\">Hi</text>", written[0]);
        Assert.Equal("<text x=\"50\" y=\"60\" font-family=\"serif\" font-size=\"12\" text-anchor=\"start\" fill=\"#0000FF\">Lo</text>", written[1]);
    }

    /// <summary>A text drawn as glyph outlines writes the path, the fill, the transform (anchor, rotation, alignment offset) and
    /// the waiting data; it has no font to fit, so a waiting text length is dropped.</summary>
    [Fact]
    public void AGlyphOutlineText_WritesItsPathFillTransformAndData_AndDropsTheTextLength()
    {
        using var heldOutlines = GlyphPathProviderDefault.Holding(new TestGlyphPaths("M 0 0"));
        var ctx = new SvgRenderContext();
        var font = new Font { Family = "serif", Size = 12, Color = Colors.Blue };
        ctx.SetNextElementData("k", "v");
        ctx.SetNextTextLength(20);
        ctx.DrawText("Hi", new Point(50, 60), font, TextAlignment.Left, 30);

        Assert.Equal("<path d=\"M 0 0\" fill=\"#0000FF\" transform=\"translate(50,60) rotate(-30)\" data-k=\"v\" />\n", Written(ctx));
    }

    /// <summary>A centred glyph outline is moved back by half the measured width of its text, along the unrotated baseline.</summary>
    [Fact]
    public void ACentredGlyphOutlineText_IsMovedBackByHalfItsMeasuredWidth()
    {
        using var heldOutlines = GlyphPathProviderDefault.Holding(new TestGlyphPaths("M 0 0"));
        var ctx = new SvgRenderContext();
        var font = new Font { Family = "serif", Size = 12 };
        double width = ctx.MeasureText("Hello", font).Width;

        ctx.DrawText("Hello", new Point(50, 60), font, TextAlignment.Center);

        string svg = Written(ctx);
        Assert.StartsWith("<path d=\"M 0 0\" transform=\"translate(50,60) translate(", svg);
        Assert.True(width > 0);
        Assert.Contains(",0)\"", svg);
    }

    // --- Rich text uses up the waiting attributes, as every other text does ---

    /// <summary>The data asked for the next element lands on the first run of a rich text, the next element written, and does not
    /// wait on to the element after it.</summary>
    [Fact]
    public void RichText_WithASuperscript_CarriesTheWaitingData_AndLeavesNoneForTheNextElement()
    {
        var ctx = new SvgRenderContext();
        ctx.SetNextElementData("k", "v<1>");

        ctx.DrawRichText(MatPlotLibNet.Rendering.MathText.MathTextParser.Parse("x$^{2}$"), new Point(50, 60),
            new Font { Family = "serif", Size = 12 }, TextAlignment.Left);
        ctx.DrawLine(new Point(0, 0), new Point(1, 1), new StrokeStyle(Colors.Black, 1, LineStyle.Solid));

        string[] written = SourceFile.ElementLinesOf(ctx.GetOutput());
        Assert.StartsWith("<text x=\"50\" y=\"60\" font-family=\"serif\" font-size=\"12\" text-anchor=\"start\" data-k=\"v&lt;1&gt;\">x</text>", written[0]);
        Assert.DoesNotContain("data-k", written[1]); // the superscript, the label's second run
        Assert.DoesNotContain("data-k", written[2]);
    }

    /// <summary>A text length asked for the next text fits the first text written, the first run of a rich text, and is used up by it.</summary>
    [Fact]
    public void RichText_WithASuperscript_WritesTheWaitingTextLength_AndUsesItUp()
    {
        using var heldOutlines = GlyphPathProviderDefault.Holding(null);
        var ctx = new SvgRenderContext();
        var font = new Font { Family = "serif", Size = 12 };
        ctx.SetNextTextLength(20);

        ctx.DrawRichText(MatPlotLibNet.Rendering.MathText.MathTextParser.Parse("x$^{2}$"), new Point(50, 60), font, TextAlignment.Left);
        ctx.DrawText("Lo", new Point(50, 60), font, TextAlignment.Left);

        string[] written = SourceFile.ElementLinesOf(ctx.GetOutput());
        Assert.StartsWith("<text x=\"50\" y=\"60\" font-family=\"serif\" font-size=\"12\" text-anchor=\"start\""
                          + " textLength=\"20\" lengthAdjust=\"spacingAndGlyphs\">x</text>", written[0]);
        Assert.DoesNotContain("textLength", written[1]);
        Assert.DoesNotContain("textLength", written[2]);
    }

    private static XElement AxesView(XDocument svg) =>
        svg.Descendants().Single(e => e.Attribute("data-axes-index") is not null);
}
