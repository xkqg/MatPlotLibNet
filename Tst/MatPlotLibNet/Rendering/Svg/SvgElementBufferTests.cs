// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>The one envelope of an SVG element (<see cref="SvgElementBuffer"/>): a mark applies the clips a text lifted, a text lifts
/// them; every element is written as its opening, then what was asked of the next element, then its close; the clips and the paint
/// servers it defines are its own.</summary>
public class SvgElementBufferTests
{
    /// <summary>An element of the test: an empty tag.</summary>
    private readonly record struct Probe(string Tag) : ISvgElement
    {
        public void WriteOpening(SvgElementBuffer buffer) => buffer.Markup.Append('<').Append(Tag);

        public void WriteClosing(StringBuilder markup) => markup.AppendLineLf(" />");
    }

    private static readonly RectGeometry Box = new(new Rect(1, 2, 3, 4));

    [Fact]
    public void AnElement_IsItsOpening_ThenWhatWasAskedOfIt_ThenItsClose()
    {
        var buffer = new SvgElementBuffer(default);
        buffer.SetNextTextLevel(2);
        buffer.SetNextClass("pane");
        buffer.SetNextData("key", "a&b");

        buffer.WriteMark(new Probe("m"));

        Assert.Equal("<m aria-level=\"2\" class=\"pane\" data-key=\"a&amp;b\" />\n", buffer.Markup.ToString());
    }

    [Fact]
    public void WhatWasAskedOfTheNextElement_IsWrittenOnce()
    {
        var buffer = new SvgElementBuffer(default);
        buffer.SetNextClass("pane");
        buffer.SetNextData("key", "v");

        buffer.WriteMark(new Probe("m"));
        buffer.WriteOutsideClips(new Probe("t"));

        Assert.EndsWith("<t />\n", buffer.Markup.ToString());
    }

    [Fact]
    public void ALevelTakenBack_IsWrittenOnNoElement()
    {
        var buffer = new SvgElementBuffer(default);
        buffer.SetNextTextLevel(3);
        buffer.SetNextTextLevel(null);

        buffer.WriteOutsideClips(new Probe("t"));

        Assert.Equal("<t />\n", buffer.Markup.ToString());
    }

    [Fact]
    public void AMark_AppliesTheClipATextLifted_AndATextLiftsIt()
    {
        var buffer = new SvgElementBuffer(default);
        buffer.PushClip(Box);

        buffer.WriteMark(new Probe("m"));
        buffer.WriteOutsideClips(new Probe("t"));
        buffer.WriteMark(new Probe("m"));
        buffer.PopClip();

        Assert.Equal(
            "<defs><clipPath id=\"clip-0\"><rect x=\"1\" y=\"2\" width=\"3\" height=\"4\" /></clipPath></defs>\n"
            + "<g clip-path=\"url(#clip-0)\">\n<m />\n</g>\n<t />\n<g clip-path=\"url(#clip-0)\">\n<m />\n</g>\n",
            buffer.Markup.ToString());
    }

    [Fact]
    public void WhatIsWrittenOutsideTheClips_LiftsThem_AndPoppingAClipThenTakesNothingOff()
    {
        var buffer = new SvgElementBuffer(default);
        buffer.PushClip(Box);

        buffer.OutsideClips().Append("<g>");
        buffer.PopClip();

        Assert.EndsWith("<g clip-path=\"url(#clip-0)\">\n</g>\n<g>", buffer.Markup.ToString());
    }

    [Fact]
    public void APopWithNoClip_WritesNothing()
    {
        var buffer = new SvgElementBuffer(default);

        buffer.PopClip();

        Assert.Equal(0, buffer.Markup.Length);
    }

    [Fact]
    public void AnOutline_IsTheClipsPath()
    {
        var buffer = new SvgElementBuffer(default);
        PathSegment[] triangle = [new MoveToSegment(new(0, 0)), new LineToSegment(new(4, 0)), new CloseSegment()];

        buffer.PushClip(new PathGeometry(triangle));

        Assert.StartsWith("<defs><clipPath id=\"clip-0\"><path d=\"M 0 0 L 4 0 Z", buffer.Markup.ToString());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("TheClipsOfALaterSubplot_CarryItsPrefix_AndEachClipANewNumber", () =>
        {
            var buffer = new SvgElementBuffer(SvgIdScope.ForSubplot(2));
            buffer.PushClip(Box);
            buffer.PushClip(Box);
            return buffer.Markup.ToString();
        }, "<clipPath id=\"s2-clip-0\">", "<clipPath id=\"s2-clip-1\">"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void TheWidthAskedOfTheNextText_IsTakenOnce()
    {
        var buffer = new SvgElementBuffer(default);
        buffer.SetNextTextLength(40);

        Assert.Equal(40, buffer.TakeTextLength());
        Assert.Null(buffer.TakeTextLength());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AWidthThatIsNoPositiveNumber_AsksNone(double width)
    {
        var buffer = new SvgElementBuffer(default);
        buffer.SetNextTextLength(40);

        buffer.SetNextTextLength(width);

        Assert.Null(buffer.TakeTextLength());
    }

    [Fact]
    public void AFlatFill_HasNoPaintServer()
    {
        var buffer = new SvgElementBuffer(default);

        Assert.Null(buffer.PaintServerOf(ShapeStyle.Filled(Colors.Red)));
        Assert.Equal(0, buffer.Markup.Length);
    }

    [Fact]
    public void AGradient_IsDefinedOnce_ForEveryShapeThatCarriesIt()
    {
        var buffer = new SvgElementBuffer(default);
        var shape = ShapeStyle.Filled(Colors.Red) with { Gradient = new LinearGradient(new(0, 0), new(10, 0), Colors.Red, Colors.Blue) };

        string? first = buffer.PaintServerOf(shape);
        string? second = buffer.PaintServerOf(shape);

        Assert.Equal("grad-0", first);
        Assert.Equal(first, second);
        Assert.Equal(1, buffer.Markup.ToString().Split("<linearGradient").Length - 1);
    }

    [Fact]
    public void AHatch_IsDefinedOnce_ForEveryShapeThatCarriesIt()
    {
        var buffer = new SvgElementBuffer(SvgIdScope.ForSubplot(1));
        var shape = ShapeStyle.Filled(Colors.Red) with { Hatch = HatchPattern.Cross };

        string? first = buffer.PaintServerOf(shape);
        string? second = buffer.PaintServerOf(shape);

        Assert.Equal("s1-hatch-0", first);
        Assert.Equal(first, second);
        Assert.Equal(1, buffer.Markup.ToString().Split("<pattern ").Length - 1);
    }
}
