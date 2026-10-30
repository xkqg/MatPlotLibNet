// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A gradient is a property of the fill, the way a hatch is: it travels in the <see cref="ShapeStyle"/> through
/// every shape-drawing operation, and the SVG backend writes it as a <c>linearGradient</c> in user space that the shape's
/// <c>fill</c> refers to. Nothing was added to <see cref="IRenderContext"/> for it.</summary>
public class GradientParityContractTests
{
    private static readonly Color Red = Color.FromHex("#FF0000");
    private static readonly Color Blue = Color.FromHex("#0000FF");

    private static ShapeStyle Blend(double x0 = 0, double x1 = 100) =>
        new(Red, null, 0) { Gradient = new LinearGradient(new Point(x0, 0), new Point(x1, 0), Red, Blue) };

    private static string Draw(Action<SvgRenderContext> draw, SvgIdScope ids = default)
    {
        var ctx = new SvgRenderContext(ids);
        draw(ctx);
        return ctx.GetOutput();
    }

    [Fact]
    public void AGradientFill_IsALinearGradientInUserSpace_WithTwoStops()
    {
        string svg = Draw(ctx => ctx.DrawRectangle(new Rect(0, 0, 100, 20), Blend()));

        var gradient = svg.Elements("linearGradient").Single();
        Assert.Equal("grad-0", gradient["id"]);
        Assert.Equal("userSpaceOnUse", gradient["gradientUnits"]);
        Assert.Equal([0.0, 0.0, 100.0, 0.0], new[] { "x1", "y1", "x2", "y2" }.Select(gradient.Number));
        var stops = svg.Elements("stop");
        Assert.Equal(2, stops.Count);
        Assert.Equal(["#FF0000", "#0000FF"], stops.Select(stop => stop["stop-color"]!.ToUpperInvariant()));
        Assert.Equal("url(#grad-0)", svg.Elements("rect").Single()["fill"]);
    }

    [Fact]
    public void ATranslucentEnd_KeepsItsOpacityOnTheStop()
    {
        var shape = new ShapeStyle(Red, null, 0) { Gradient = new LinearGradient(new Point(0, 0), new Point(10, 0), Red, new Color(0, 0, 255, 128)) };

        string svg = Draw(ctx => ctx.DrawRectangle(new Rect(0, 0, 10, 10), shape));

        Assert.Null(svg.Elements("stop")[0]["stop-opacity"]);
        Assert.NotNull(svg.Elements("stop")[1]["stop-opacity"]);
    }

    [Fact]
    public void TheSameGradientTwice_IsDefinedOnce()
    {
        string svg = Draw(ctx =>
        {
            ctx.DrawRectangle(new Rect(0, 0, 100, 20), Blend());
            ctx.DrawRectangle(new Rect(0, 30, 100, 20), Blend());
        });

        Assert.Single(svg.Elements("linearGradient"));
        Assert.All(svg.Elements("rect"), rect => Assert.Equal("url(#grad-0)", rect["fill"]));
    }

    [Fact]
    public void ADifferentGradient_GetsItsOwnId()
    {
        string svg = Draw(ctx =>
        {
            ctx.DrawRectangle(new Rect(0, 0, 100, 20), Blend());
            ctx.DrawRectangle(new Rect(0, 30, 100, 20), Blend(0, 50));
        });

        Assert.Equal(["grad-0", "grad-1"], svg.Elements("linearGradient").Select(gradient => gradient["id"]));
    }

    [Fact]
    public void AGradientOfALaterSubplot_CarriesThePrefixOfItsSubplot()
    {
        string svg = Draw(ctx => ctx.DrawRectangle(new Rect(0, 0, 100, 20), Blend()), SvgIdScope.ForSubplot(2));

        Assert.Equal("s2-grad-0", svg.Elements("linearGradient").Single()["id"]);
        Assert.Equal("url(#s2-grad-0)", svg.Elements("rect").Single()["fill"]);
    }

    [Fact]
    public void AGradientWithAHatch_IsTheGradientWithTheHatchLaidOverIt()
    {
        var shape = Blend() with { Hatch = HatchPattern.ForwardDiagonal, HatchColor = Colors.Black };

        string svg = Draw(ctx => ctx.DrawRectangle(new Rect(0, 0, 100, 20), shape));

        var rects = svg.Elements("rect").Where(rect => rect["width"] == "100").ToArray();
        Assert.Equal(["url(#grad-0)", "url(#hatch-0)"], rects.Select(rect => rect["fill"]));
        // The hatch tile paints strokes only: its own background rectangle (the tile is 6 wide) would hide the gradient.
        Assert.DoesNotContain(svg.Elements("rect"), rect => rect["width"] == "6");
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void EveryShapeKind_WithAGradientAndAHatch_IsTheGradientWithTheHatchLaidOverIt(string kind)
    {
        var shape = Blend() with { Hatch = HatchPattern.ForwardDiagonal, HatchColor = Colors.Black };
        string svg = Draw(ctx =>
        {
            switch (kind)
            {
                case "polygon": ctx.DrawPolygon([new Point(0, 0), new Point(50, 0), new Point(25, 40)], shape); break;
                case "circle": ctx.DrawCircle(new Point(50, 50), 20, shape); break;
                case "rectangle": ctx.DrawRectangle(new Rect(0, 0, 100, 20), shape); break;
                case "ellipse": ctx.DrawEllipse(new Rect(0, 0, 100, 20), shape); break;
                default: ctx.DrawPath([new MoveToSegment(new Point(0, 0)), new LineToSegment(new Point(50, 0)), new LineToSegment(new Point(25, 40)), new CloseSegment()], shape); break;
            }
        });

        string tag = kind == "rectangle" ? "rect" : kind;
        // The element the hatch tile itself draws is not a shape of the figure: only the shapes that carry the paint count.
        var drawn = svg.Elements(tag).Where(element => element["fill"] is "url(#grad-0)" or "url(#hatch-0)").ToArray();
        Assert.Equal(["url(#grad-0)", "url(#hatch-0)"], drawn.Select(element => element["fill"]));
    }

    [Fact]
    public void AShapeWithoutAGradient_WritesNoGradient()
    {
        string svg = Draw(ctx => ctx.DrawRectangle(new Rect(0, 0, 100, 20), new ShapeStyle(Red, null, 0)));

        Assert.Empty(svg.Elements("linearGradient"));
        Assert.Equal("#FF0000", svg.Elements("rect").Single()["fill"]!.ToUpperInvariant());
    }

    [Fact]
    public void AGradientNeedsAFill_ToBeVisible()
    {
        var shape = new ShapeStyle(null, null, 0) { Gradient = new LinearGradient(new Point(0, 0), new Point(1, 0), Red, Blue) };

        Assert.False(shape.HasVisibleGradient);
        Assert.True(Blend().HasVisibleGradient);
        string svg = Draw(ctx => ctx.DrawRectangle(new Rect(0, 0, 10, 10), shape));
        Assert.Empty(svg.Elements("linearGradient"));
        Assert.Equal("none", svg.Elements("rect").Single()["fill"]);
    }

    /// <summary>The two members the Sankey renderer used before the gradient travelled in the shape are published API: they
    /// still define a gradient and fill a path with it, and say what to use instead.</summary>
    [Fact]
    public void TheObsoleteMembers_StillDefineAGradientAndFillAPathWithIt()
    {
#pragma warning disable CS0618 // the members under test are the obsolete ones
        string svg = Draw(ctx =>
        {
            string id = ctx.DefineLinearGradient(Red, Blue, 0, 0, 100, 0);
            ctx.DrawPathWithGradientFill([new MoveToSegment(new Point(0, 0)), new LineToSegment(new Point(50, 0)), new LineToSegment(new Point(25, 40)), new CloseSegment()], id, Colors.Black, 2);
        });
#pragma warning restore CS0618

        Assert.Equal("grad-0", svg.Elements("linearGradient").Single()["id"]);
        var path = svg.Elements("path").Single();
        Assert.Equal("url(#grad-0)", path["fill"]);
        Assert.Equal("2", path["stroke-width"]);
    }

    public static TheoryData<string> Shapes { get; } = new("polygon", "circle", "rectangle", "ellipse", "path");

    [Theory]
    [MemberData(nameof(Shapes))]
    public void EveryShapeKind_TakesTheGradient(string kind)
    {
        string svg = Draw(ctx =>
        {
            var shape = Blend();
            switch (kind)
            {
                case "polygon": ctx.DrawPolygon([new Point(0, 0), new Point(50, 0), new Point(25, 40)], shape); break;
                case "circle": ctx.DrawCircle(new Point(50, 50), 20, shape); break;
                case "rectangle": ctx.DrawRectangle(new Rect(0, 0, 100, 20), shape); break;
                case "ellipse": ctx.DrawEllipse(new Rect(0, 0, 100, 20), shape); break;
                default: ctx.DrawPath([new MoveToSegment(new Point(0, 0)), new LineToSegment(new Point(50, 0)), new LineToSegment(new Point(25, 40)), new CloseSegment()], shape); break;
            }
        });

        Assert.Single(svg.Elements("linearGradient"));
        Assert.Contains("fill=\"url(#grad-0)\"", svg, StringComparison.Ordinal);
    }
}
