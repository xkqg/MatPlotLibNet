// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>An SVG <c>&lt;text&gt;</c> is set by the browser in whichever font it finds, so a line that was measured and packed
/// in one font can come out wider in another and run into its neighbour. A drawing can say how wide the next text is to be
/// (<see cref="IRenderContext.SetNextTextLength"/>): the SVG context then writes <c>textLength</c> with
/// <c>lengthAdjust="spacingAndGlyphs"</c>, and the browser squeezes or stretches the text to that width. It applies to the next
/// text only, and a context that draws no <c>&lt;text&gt;</c> ignores it.</summary>
public class SvgTextLengthTests
{
    [Fact]
    public void TheNextText_IsAskedToBeAsWideAsSaid()
    {
        var ctx = new SvgRenderContext();

        ctx.SetNextTextLength(123.5);
        ctx.DrawText("hello", new Point(10, 20), new Font(), TextAlignment.Left);

        string svg = ctx.GetOutput();
        Assert.Contains("textLength=\"123.5\"", svg, StringComparison.Ordinal);
        Assert.Contains("lengthAdjust=\"spacingAndGlyphs\"", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyTheNextText_IsFitted()
    {
        var ctx = new SvgRenderContext();

        ctx.SetNextTextLength(50);
        ctx.DrawText("first", new Point(0, 10), new Font(), TextAlignment.Left);
        ctx.DrawText("second", new Point(0, 30), new Font(), TextAlignment.Left);

        Assert.Single(System.Text.RegularExpressions.Regex.Matches(ctx.GetOutput(), "textLength="));
    }

    [Fact]
    public void ALengthThatIsNotAPositiveNumber_IsLeftOut()
    {
        var ctx = new SvgRenderContext();

        ctx.SetNextTextLength(double.NaN);
        ctx.DrawText("a", new Point(0, 10), new Font(), TextAlignment.Left);
        ctx.SetNextTextLength(0);
        ctx.DrawText("b", new Point(0, 30), new Font(), TextAlignment.Left);
        ctx.SetNextTextLength(-4);
        ctx.DrawText("c", new Point(0, 50), new Font(), TextAlignment.Left);

        Assert.DoesNotContain("textLength", ctx.GetOutput(), StringComparison.Ordinal);
    }

    [Fact]
    public void AContextThatDrawsNoTextElement_IgnoresIt()
    {
        IRenderContext recording = new RecordingRenderContext();

        var thrown = Record.Exception(() => recording.SetNextTextLength(40));

        Assert.Null(thrown);
    }
}
