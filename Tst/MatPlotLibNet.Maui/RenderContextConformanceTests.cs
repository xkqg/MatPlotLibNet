// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using Microsoft.Maui.Graphics;
using NSubstitute;
using MplColor = MatPlotLibNet.Styling.Colors;
using MplFont = MatPlotLibNet.Styling.Font;
using MplFontSlant = MatPlotLibNet.Styling.FontSlant;
using MplFontWeight = MatPlotLibNet.Styling.FontWeight;
using MplPoint = MatPlotLibNet.Rendering.Point;
using MplRect = MatPlotLibNet.Rendering.Rect;

namespace MatPlotLibNet.Maui.Tests;

/// <summary>What every drawing surface of the library does with a call, whatever it draws on, held for the MAUI surface: the rules are
/// the core's, and this surface holds only the conversion of each to its canvas. The SVG surface is held by the same-named class of the
/// core suite, the PNG and PDF surface by the one of the Skia suite.</summary>
public class RenderContextConformanceTests
{
    /// <summary>An opacity a caller states outside 0 to 1 is held at the nearest end, by every surface: the canvas is never given an
    /// alpha it has to cut itself, and the opacity a scope puts back is the one held.</summary>
    [Theory]
    [InlineData(-0.5, 0f)]
    [InlineData(0.25, 0.25f)]
    [InlineData(1.5, 1f)]
    public void ASurface_HoldsAnOpacityBetweenZeroAndOne(double asked, float alpha)
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.SetOpacity(asked);

        canvas.Received().Alpha = alpha;
        Assert.Equal(alpha, ((IOpacityRenderContext)ctx).Opacity, 6);
    }

    /// <summary>A polygon is drawn from the points it is given, whatever their number, and a polyline needs two points to be a line:
    /// the same on every surface.</summary>
    [Fact]
    public void APolygonOfTwoPoints_IsDrawnAsTheOutlineItIs_AndALineOfOnePointIsNot()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawLines([new MplPoint(3, 4)], StrokeStyle.Solid(MplColor.Black, 1));
        canvas.DidNotReceive().DrawPath(Arg.Any<PathF>());
        ctx.DrawPolygon([new MplPoint(3, 4), new MplPoint(3, 20)], new ShapeStyle(null, MplColor.Black, 1));

        canvas.Received(1).DrawPath(Arg.Any<PathF>());
    }

    /// <summary>A text whose font names no colour is drawn black, as the PNG surface draws it and the SVG surface leaves it to the viewer's
    /// default: the canvas keeps its font colour between calls, and the text before it (a red label) must not colour the next.</summary>
    [Fact]
    public void ATextWithNoColour_IsDrawnBlack_NotInTheColourOfTheTextBefore()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawText("red", new MplPoint(1, 1), new MplFont { Color = MplColor.Red }, MatPlotLibNet.Rendering.TextAlignment.Left);
        ctx.DrawText("plain", new MplPoint(1, 2), new MplFont(), MatPlotLibNet.Rendering.TextAlignment.Left);

        var colours = canvas.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "set_FontColor")
            .Select(call => (Microsoft.Maui.Graphics.Color)call.GetArguments()[0]!).ToList();
        Assert.Equal(2, colours.Count);
        Assert.Equal(1f, colours[0].Red);
        Assert.Equal(0f, colours[1].Red);
        Assert.Equal(0f, colours[1].Green);
        Assert.Equal(0f, colours[1].Blue);
    }

    /// <summary>A bold font and an italic font are drawn bold and italic on the canvas, as the SVG and PNG surfaces draw them: the MAUI
    /// surface drew every text in the canvas' regular face. A light weight is drawn as the regular face and an oblique slant as the italic
    /// one, as on the other surfaces (<c>Font.IsBold</c>, <c>Font.IsItalic</c>), and every text sets the face, since the canvas keeps it.</summary>
    [Theory]
    [InlineData(MplFontWeight.Normal, MplFontSlant.Normal, FontWeights.Regular, FontStyleType.Normal)]
    [InlineData(MplFontWeight.Bold, MplFontSlant.Normal, FontWeights.Bold, FontStyleType.Normal)]
    [InlineData(MplFontWeight.Normal, MplFontSlant.Italic, FontWeights.Regular, FontStyleType.Italic)]
    [InlineData(MplFontWeight.Bold, MplFontSlant.Italic, FontWeights.Bold, FontStyleType.Italic)]
    [InlineData(MplFontWeight.Light, MplFontSlant.Oblique, FontWeights.Regular, FontStyleType.Italic)]
    public void AFont_IsDrawnBoldAndItalicAsTheOtherSurfacesDrawIt(MplFontWeight weight, MplFontSlant slant, int canvasWeight, FontStyleType canvasStyle)
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawText("t", new MplPoint(1, 1), new MplFont { Weight = weight, Slant = slant }, MatPlotLibNet.Rendering.TextAlignment.Left);

        canvas.Received(1).Font = Arg.Is<IFont>(font => font.Weight == canvasWeight && font.StyleType == canvasStyle);
    }

    /// <summary>A text starts where its alignment puts it by the width the layout measured it at (<c>TextAlignment.StartOffset</c> of the
    /// library's metrics), as the SVG and PNG surfaces start it: the MAUI canvas centred and right-aligned the string by the width of its
    /// own fonts, which is not the width the layout had left room for, so a label stood off its place by the difference.</summary>
    [Theory]
    [InlineData(MatPlotLibNet.Rendering.TextAlignment.Left)]
    [InlineData(MatPlotLibNet.Rendering.TextAlignment.Center)]
    [InlineData(MatPlotLibNet.Rendering.TextAlignment.Right)]
    public void AText_StartsWhereItsAlignmentPutsIt_ByTheWidthTheLayoutMeasured(MatPlotLibNet.Rendering.TextAlignment alignment)
    {
        var (canvas, ctx) = MauiRig.Fresh();
        var font = new MplFont { Size = 14 };
        double width = ctx.MeasureText("Centre label", font).Width;

        ctx.DrawText("Centre label", new MplPoint(50, 60), font, alignment);

        canvas.Received(1).DrawString("Centre label", (float)(50 + alignment.StartOffset(width)), 60f, HorizontalAlignment.Left);
    }

    /// <summary>The y a text is drawn at is its baseline on every surface (the SVG <c>&lt;text y&gt;</c>, Skia's <c>DrawText(blob, x, y)</c>), and
    /// it is the y the MAUI canvas takes for its baseline: <c>ICanvas.DrawString(value, x, y, HorizontalAlignment)</c> draws the line on y on
    /// iOS and Mac Catalyst (a <c>CTLine</c> drawn with its text matrix at (x, y), where Core Text puts the baseline), and on Android and
    /// Windows sets the top of the line one font size above y (Android translates to <c>y - ScaledFontSize</c> and draws a static layout
    /// without padding; Win2D lays the string top-aligned in a box from <c>y - FontSize</c>), so the baseline stands at y give or take the
    /// difference between the face's ascent and its size, which the platform's face decides (read from Microsoft.Maui.Graphics 10.0.120,
    /// decompiled). So the surface hands the canvas the library's baseline as it is, plain or
    /// turned (the turn is about the anchor), and moves no text up or down.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(90.0)]
    public void AText_IsHandedToTheCanvasOnItsBaseline(double rotation)
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawText("Baseline", new MplPoint(50, 60), new MplFont { Size = 14 }, MatPlotLibNet.Rendering.TextAlignment.Left, rotation);

        canvas.Received(1).DrawString("Baseline", 50f, 60f, HorizontalAlignment.Left);
    }

    [Fact]
    public void ARect_BecomesTheRectOfItsPositionAndSize()
    {
        Assert.Equal(new RectF(5.5f, 7.25f, 100f, 40f), MauiGraphicsRenderContext.ToRectF(new MplRect(5.5, 7.25, 100, 40)));
    }

    [Theory]
    [InlineData(JoinStyle.Round, LineJoin.Round)]
    [InlineData(JoinStyle.Bevel, LineJoin.Bevel)]
    [InlineData(JoinStyle.Miter, LineJoin.Miter)]
    public void AJoin_IsTheJoinOfTheCanvas(JoinStyle join, LineJoin expected)
    {
        Assert.Equal(expected, MauiGraphicsRenderContext.ToMaui(join));
    }

    /// <summary>A shape is filled before its outline is drawn, whatever it is, so the outline stands on the fill: the one sequence of
    /// the surface, for a polygon, a circle, a rectangle, an ellipse and a path.</summary>
    [Theory]
    [InlineData("polygon")]
    [InlineData("circle")]
    [InlineData("rectangle")]
    [InlineData("ellipse")]
    [InlineData("path")]
    public void AShape_IsFilledBeforeItsOutlineIsDrawn(string kind)
    {
        var (canvas, ctx) = MauiRig.Fresh();
        var style = new ShapeStyle(MplColor.Red, MplColor.Blue, 2);
        var box = new MplRect(10, 20, 30, 40);

        switch (kind)
        {
            case "polygon": ctx.DrawPolygon([new(0, 0), new(10, 0), new(5, 8)], style); break;
            case "circle": ctx.DrawCircle(new MplPoint(10, 10), 5, style); break;
            case "rectangle": ctx.DrawRectangle(box, style); break;
            case "ellipse": ctx.DrawEllipse(box, style); break;
            default: ctx.DrawPath([new MoveToSegment(new(0, 0)), new LineToSegment(new(10, 0)), new LineToSegment(new(5, 8)), new CloseSegment()], style); break;
        }

        Assert.Equal(1, canvas.ReceivedCalls().Count(call => IsFill(call.GetMethodInfo().Name)));
        Assert.Equal(1, canvas.ReceivedCalls().Count(call => IsOutline(call.GetMethodInfo().Name)));
        var names = canvas.ReceivedCalls().Select(call => call.GetMethodInfo().Name).ToList();
        Assert.True(names.FindIndex(n => IsFill(n)) < names.FindIndex(n => IsOutline(n)));
    }

    private static bool IsFill(string method) => method is "FillPath" or "FillCircle" or "FillRectangle" or "FillEllipse";

    private static bool IsOutline(string method) => method is "DrawPath" or "DrawCircle" or "DrawRectangle" or "DrawEllipse";
}
