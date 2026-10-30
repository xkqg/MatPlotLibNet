// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The axes of a ternary diagram: an equilateral triangle with a grid at the fifths, the ticks counter-clockwise
/// (Right along the base, Top up the right edge, Left down the left edge), and the three parts named at the corners. It
/// draws in the largest box of the triangle's own shape that its cell allows, and that box is the whole plot area, so the
/// view transform, the hit test and the drawing agree on where the triangle stands.</summary>
public class TernaryAxesRendererTests
{
    private static readonly double Height = Math.Sqrt(3) / 2;

    private static readonly Rect Box = new(100, 50, 400, 400 * Math.Sqrt(3) / 2);

    private static Axes NewAxes() => new() { CoordinateSystem = CoordinateSystem.Ternary };

    private static RecordingRenderContext Draw(Axes axes)
    {
        var ctx = new RecordingRenderContext();
        AxesRenderer.Create(axes, Box, ctx, Theme.Default).Render();
        return ctx;
    }

    /// <summary>A pixel as the position it stands for in the frame one unit wide.</summary>
    private static Point InFrame(Point pixel) => new((pixel.X - Box.X) / Box.Width, (Box.Y + Box.Height - pixel.Y) / Box.Width);

    private static (Point From, Point To) LineOf(CallRecord call)
    {
        dynamic args = call.Args!;
        return (InFrame((Point)args.p1), InFrame((Point)args.p2));
    }

    private static bool Joins((Point From, Point To) line, (double X, double Y) one, (double X, double Y) other)
    {
        static bool Is(Point at, (double X, double Y) expected) => Math.Abs(at.X - expected.X) < 1e-6 && Math.Abs(at.Y - expected.Y) < 1e-6;
        return (Is(line.From, one) && Is(line.To, other)) || (Is(line.From, other) && Is(line.To, one));
    }

    [Fact]
    public void TheCoordinateSystem_IsDrawnByTheTernaryRenderer()
    {
        var renderer = AxesRenderer.Create(NewAxes(), Box, new RecordingRenderContext(), Theme.Default);

        Assert.IsType<TernaryAxesRenderer>(renderer);
    }

    [Fact]
    public void TheViewTransform_IsTheFrame_OverTheWholePlotArea()
    {
        var view = AxesRenderer.Create(NewAxes(), Box, new RecordingRenderContext(), Theme.Default).ComputeViewTransform();

        Assert.NotNull(view);
        Assert.Equal(0.0, view!.DataXMin);
        Assert.Equal(1.0, view.DataXMax);
        Assert.Equal(0.0, view.DataYMin);
        Assert.Equal(Height, view.DataYMax, 12);
        Assert.Equal(Box, view.PlotBounds);
    }

    [Fact]
    public void ThePlaceOfAComposition_InThePlotArea_IsWhereTheViewTransformSaysItIs()
    {
        var view = AxesRenderer.Create(NewAxes(), Box, new RecordingRenderContext(), Theme.Default).ComputeViewTransform()!;
        var frame = new TernaryPoint(0.2, 0.5, 0.3).InFrame();

        var pixel = view.DataToPixel(frame.X, frame.Y);

        Assert.Equal(Box.X + 0.4 * Box.Width, pixel.X, 6);
        Assert.Equal(Box.Y + Box.Height - 0.173205 * Box.Width, pixel.Y, 3);
    }

    [Fact]
    public void TheBackground_IsTheTriangle_OnThreeCorners()
    {
        var ctx = Draw(NewAxes());

        dynamic polygon = ctx.OfKind("DrawPolygon").First(c => ((dynamic)c.Args!).fill is not null).Args!;
        Point[] corners = polygon.points;

        Assert.Equal(3, corners.Length);
        Assert.Contains(corners, c => Math.Abs(c.X - Box.X) < 1e-9 && Math.Abs(c.Y - (Box.Y + Box.Height)) < 1e-9);      // bottom left
        Assert.Contains(corners, c => Math.Abs(c.X - (Box.X + Box.Width)) < 1e-9 && Math.Abs(c.Y - (Box.Y + Box.Height)) < 1e-9);   // bottom right
        Assert.Contains(corners, c => Math.Abs(c.X - (Box.X + Box.Width / 2)) < 1e-9 && Math.Abs(c.Y - Box.Y) < 1e-9);   // apex
    }

    [Fact]
    public void TheGrid_IsFourLinesForEachPart_AtTheFifths()
    {
        var lines = Draw(NewAxes()).OfKind("DrawLine").Select(LineOf).ToArray();

        Assert.Equal(12, lines.Length);   // 0.2, 0.4, 0.6 and 0.8 of Top, Left and Right
        // T5: the lines at 0.2.
        Assert.Contains(lines, l => Joins(l, (0.1, 0.173205), (0.9, 0.173205)));   // Top = 0.2
        Assert.Contains(lines, l => Joins(l, (0.4, 0.692820), (0.8, 0.0)));        // Left = 0.2
        Assert.Contains(lines, l => Joins(l, (0.2, 0.0), (0.6, 0.692820)));        // Right = 0.2
    }

    [Fact]
    public void AGridTheAxesSwitchedOff_IsNotDrawn_ButTheTriangleStays()
    {
        var axes = NewAxes();
        axes.GridChange = new GridChange(Visible: false);

        var ctx = Draw(axes);

        Assert.Equal(0, ctx.CountOf("DrawLine"));
        Assert.True(ctx.CountOf("DrawPolygon") >= 2);   // the background and the outline
    }

    [Fact]
    public void TheTicks_AreTheFifths_OnEachOfTheThreeEdges()
    {
        var texts = Draw(NewAxes()).OfKind("DrawText").Select(c => (string)((dynamic)c.Args!).text).ToArray();

        foreach (string tick in new[] { "0.2", "0.4", "0.6", "0.8" })
        {
            Assert.Equal(3, texts.Count(t => t == tick));
        }
    }

    [Fact]
    public void TheTicksRunCounterClockwise_RightAlongTheBase_TopUpTheRightEdge_LeftDownTheLeftEdge()
    {
        var ticks = Draw(NewAxes()).OfKind("DrawText")
            .Where(c => (string)((dynamic)c.Args!).text == "0.2")
            .Select(c => InFrame((Point)((dynamic)c.Args!).position))
            .ToArray();

        // Right = 0.2 on the base, 0.2 from the bottom left; Top = 0.2 on the right edge, near the bottom right; Left = 0.2 on the
        // left edge, near the bottom left: the label of each sits just outside its edge.
        Assert.Contains(ticks, t => Math.Abs(t.X - 0.2) < 0.03 && t.Y < 0);               // under the base
        Assert.Contains(ticks, t => t.X > 0.9 && Math.Abs(t.Y - 0.173205) < 0.05);        // right of the right edge
        Assert.Contains(ticks, t => t.X is > 0.3 and < 0.4 && Math.Abs(t.Y - 0.692820) < 0.05);   // left of the left edge, high up: Left = 0.2 is 0.8 up
    }

    [Fact]
    public void ThePartsAreNamedAtTheirCorners_ByTheLabelsOfTheThreeAxes()
    {
        var axes = NewAxes();
        axes.XAxis.Label = "Sand";   // Right
        axes.YAxis.Label = "Clay";   // Top
        axes.ZAxis.Label = "Silt";   // Left

        var texts = Draw(axes).OfKind("DrawText").Select(c => (Text: (string)((dynamic)c.Args!).text, At: InFrame((Point)((dynamic)c.Args!).position))).ToArray();

        var clay = Assert.Single(texts, t => t.Text == "Clay");
        var silt = Assert.Single(texts, t => t.Text == "Silt");
        var sand = Assert.Single(texts, t => t.Text == "Sand");
        Assert.Equal(0.5, clay.At.X, 6);
        Assert.True(clay.At.Y > Height);   // above the apex
        Assert.Equal(0.0, silt.At.X, 6);
        Assert.True(silt.At.Y < 0);        // below the bottom left corner
        Assert.Equal(1.0, sand.At.X, 6);
        Assert.True(sand.At.Y < 0);        // below the bottom right corner
    }

    [Fact]
    public void ATitle_IsDrawnAboveTheTopCornerLabel()
    {
        var axes = NewAxes();
        axes.Title = "Soil";
        axes.YAxis.Label = "Clay";

        var texts = Draw(axes).OfKind("DrawText").Select(c => (Text: (string)((dynamic)c.Args!).text, At: (Point)((dynamic)c.Args!).position)).ToArray();

        var title = Assert.Single(texts, t => t.Text == "Soil");
        var clay = Assert.Single(texts, t => t.Text == "Clay");
        Assert.True(title.At.Y < clay.At.Y - 5, $"title at {title.At.Y} collides with the top label at {clay.At.Y}");
    }

    [Fact]
    public void ATernaryFigure_RendersToSvg_WithoutASeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot(1, 1, 1);
        axes.CoordinateSystem = CoordinateSystem.Ternary;

        string svg = figure.ToSvg();

        Assert.Contains("<polygon", svg, StringComparison.Ordinal);
    }

    // ── the box the triangle is drawn in ────────────────────────────────────────

    [Fact]
    public void TheCellOfATernaryAxes_IsFittedToTheTriangleBox_WhateverTheAspectSays()
    {
        var wide = new Rect(0, 0, 800, 400);

        var fitted = NewAxes().FittedTo(wide, Theme.Default);

        Assert.Equal(400.0, fitted.Height, 9);
        Assert.Equal(400.0 / Height, fitted.Width, 9);
        Assert.Equal((800 - fitted.Width) / 2, fitted.X, 9);   // centred

        var withAspect = NewAxes();
        withAspect.Aspect = 3;
        Assert.Equal(fitted, withAspect.FittedTo(wide, Theme.Default));
    }

    [Fact]
    public void ATallCell_IsFittedByItsWidth()
    {
        var tall = new Rect(0, 0, 300, 600);

        var fitted = NewAxes().FittedTo(tall, Theme.Default);

        Assert.Equal(300.0, fitted.Width, 9);
        Assert.Equal(300.0 * Height, fitted.Height, 9);
        Assert.Equal((600 - fitted.Height) / 2, fitted.Y, 9);
    }

    [Fact]
    public void TheLayoutOfAFigure_GivesTheTernaryAxesItsBox()
    {
        var figure = new Figure { Width = 800, Height = 600 };
        var axes = figure.AddSubPlot(1, 1, 1);
        axes.CoordinateSystem = CoordinateSystem.Ternary;

        var plotArea = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];

        Assert.Equal(Height, plotArea.Height / plotArea.Width, 9);
    }

    [Fact]
    public void ANameThatContainsMath_IsDrawnAsRichText()
    {
        var axes = NewAxes();
        axes.YAxis.Label = @"$lpha$ part";

        var ctx = Draw(axes);

        Assert.NotEmpty(ctx.OfKind("DrawRichText"));
    }

    [Fact]
    public void ANameWithATextStyle_IsDrawnInThatStyle()
    {
        var axes = NewAxes();
        axes.XAxis.Label = "Sand";
        axes.XAxis.LabelStyle = new TextStyle { FontSize = 23 };

        var name = Draw(axes).OfKind("DrawText").Select(c => (dynamic)c.Args!).Single(a => (string)a.text == "Sand");

        Assert.Equal(23.0, (double)name.font.Size, 6);
    }
}
