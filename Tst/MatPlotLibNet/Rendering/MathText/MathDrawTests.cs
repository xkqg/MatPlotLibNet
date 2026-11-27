// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>The one verb draws a label of math as its layout, through the public members of any surface: each run a <c>DrawText</c>
/// at the left end of where the layout put it (the label's start placed by its alignment, the whole turned about the anchor), each
/// rule a <c>DrawRectangle</c> upright or a <c>DrawPolygon</c> turned, filled in the label's colour.</summary>
public sealed class MathDrawTests
{
    private static readonly Font Font = new() { Size = 10, Color = Colors.Blue };

    private sealed class Surface : SilentRenderContext, IRenderContext
    {
        public List<string> Calls { get; } = [];
        public List<Point> Texts { get; } = [];
        public List<Font> Fonts { get; } = [];
        public List<Rect> Rectangles { get; } = [];
        public List<IReadOnlyList<Point>> Polygons { get; } = [];
        public List<ShapeStyle> Fills { get; } = [];

        /// <summary>Whether the surface keeps no record of what is drawn on it, so a draw's own allocations can be counted.</summary>
        public bool Quiet { get; init; }

        public override void DrawText(string text, Point position, Font font, TextAlignment alignment)
        {
            if (Quiet) return;
            Calls.Add($"{text}:{alignment}");
            Texts.Add(position);
            Fonts.Add(font);
        }

        public void DrawText(string text, Point position, Font font, TextAlignment alignment, double rotation)
        {
            Calls.Add($"{text}:{alignment}@{rotation}");
            Texts.Add(position);
            Fonts.Add(font);
        }

        public override void DrawRectangle(Rect rect, ShapeStyle shape)
        {
            if (Quiet) return;
            Rectangles.Add(rect);
            Fills.Add(shape);
        }

        public override void DrawPolygon(IReadOnlyList<Point> points, ShapeStyle shape)
        {
            Polygons.Add(points);
            Fills.Add(shape);
        }

        public override Size MeasureText(string text, Font font) => new(text.Length * font.Size / 2, font.Size * 1.2);
    }

    [Fact]
    public void ALabelOfManyRuns_IsDrawnRunByRun_FromWhereItsAlignmentStartsIt()
    {
        var surface = new Surface();
        var label = TextLabel.Of("$a+b$");
        var ink = surface.LayOut(label.Parsed(null).Tree!, Font);

        surface.Draw(label, Font, new TextPlacement(new Point(100, 50), TextAlignment.Right));

        Assert.Equal(["a:Left", "+:Left", "b:Left"], surface.Calls);
        Assert.Equal(100 - ink.Extent.Width, surface.Texts[0].X, 9);
        Assert.Equal(100 - ink.Extent.Width + ink.Glyphs[2].X, surface.Texts[2].X, 9);
        Assert.All(surface.Fonts, font => Assert.Equal(Colors.Blue, font.Color));
    }

    [Fact]
    public void ARule_IsAFilledRectangleInTheLabelsColour()
    {
        var surface = new Surface();
        var label = TextLabel.Of(@"$\frac{a}{b}$");
        var rule = surface.LayOut(label.Parsed(null).Tree!, Font).Rules[0];

        surface.Draw(label, Font, new TextPlacement(new Point(100, 50), TextAlignment.Left));

        Assert.Equal(new Rect(100 + rule.X, 50 + rule.Y, rule.Width, rule.Height), Assert.Single(surface.Rectangles));
        Assert.Equal(ShapeStyle.Filled(Colors.Blue), Assert.Single(surface.Fills));
    }

    [Fact]
    public void ARuleOfALabelWithNoColour_IsBlack()
    {
        var surface = new Surface();

        surface.Draw(TextLabel.Of(@"$\sqrt{x}$"), Font with { Color = null }, new TextPlacement(new Point(0, 0), TextAlignment.Left));

        Assert.Equal(ShapeStyle.Filled(Colors.Black), Assert.Single(surface.Fills));
    }

    /// <summary>Turned, each run is drawn turned about its own start, which stands on the turned baseline; a rule is the polygon of its
    /// four turned corners.</summary>
    [Fact]
    public void ATurnedLabel_TurnsEachRunAndEachRuleAboutTheAnchor()
    {
        var surface = new Surface();
        var label = TextLabel.Of(@"$\frac{a}{b}$");
        var ink = surface.LayOut(label.Parsed(null).Tree!, Font);
        var rule = ink.Rules[0];

        surface.Draw(label, Font, new TextPlacement(new Point(100, 50), TextAlignment.Left) { Rotation = 90 });

        Assert.All(surface.Calls, call => Assert.EndsWith("@90", call, StringComparison.Ordinal));
        Assert.Empty(surface.Rectangles);
        var corners = Assert.Single(surface.Polygons);
        Assert.Equal(new Point(100 + rule.Y, 50 - rule.X), corners[0] with { X = Math.Round(corners[0].X, 9), Y = Math.Round(corners[0].Y, 9) });
        Assert.Equal(4, corners.Count);
        var a = ink.Glyphs.Single(g => g.Text == "a");
        Assert.Equal(100 + a.Y, surface.Texts[0].X, 9);
        Assert.Equal(50 - a.X, surface.Texts[0].Y, 9);
    }

    /// <summary>A label laid out as one run on its baseline is drawn as a plain text is, placed by the surface's own alignment.</summary>
    [Fact]
    public void ALabelOfOneRun_IsDrawnByItsAlignment()
    {
        var surface = new Surface();

        surface.Draw(TextLabel.Of("$\\alpha$"), Font, new TextPlacement(new Point(100, 50), TextAlignment.Center));

        Assert.Equal(["α:Center"], surface.Calls);
        Assert.Equal(new Point(100, 50), Assert.Single(surface.Texts));
    }

    /// <summary>A label drawn again is drawn from the layout its tree keeps: each run is handed to the surface in the font the layout made
    /// for it (its face at its size, in the label's colour), so the draw of a label laid out already allocates nothing: no font for a
    /// script or a fraction's part, nothing to walk its runs and its rules.</summary>
    [Theory]
    [InlineData("$x^2$")]
    [InlineData(@"$\alpha_i^2 + \beta_i$")]
    [InlineData(@"$\frac{\Delta y}{\Delta x}$")]
    [InlineData(@"$\sqrt{x^2 + y^2}$")]
    public void ALabelDrawnAgain_AllocatesNothing(string text)
    {
        var surface = new Surface { Quiet = true };
        var label = TextLabel.Of(text).Parsed(null);
        var placement = new TextPlacement(new Point(100, 50), TextAlignment.Left);
        for (int i = 0; i < 50; i++) surface.Draw(label, Font, placement);

        long before = GC.GetAllocatedBytesForCurrentThread();
        surface.Draw(label, Font, placement);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    /// <summary>The font a run is drawn in is made once, by the layout: two draws hand the surface the same font.</summary>
    [Fact]
    public void ARun_IsDrawnInTheFontItsLayoutMade()
    {
        var surface = new Surface();
        var label = TextLabel.Of("$x^2$").Parsed(null);
        var placement = new TextPlacement(new Point(0, 0), TextAlignment.Left);

        surface.Draw(label, Font, placement);
        surface.Draw(label, Font, placement);

        Assert.Equal(4, surface.Fonts.Count);
        Assert.Same(surface.Fonts[1], surface.Fonts[3]);
        Assert.Equal((Font.Size * MathConstants.Library.ShrinkFactor, Colors.Blue), (surface.Fonts[1].Size, surface.Fonts[1].Color!.Value));
    }

    /// <summary>A word of a word cloud is fitted to the width it was packed at when it is plain; a word of math is laid out run by run,
    /// so no text is fitted (an SVG writes the length on the next text, which would be the math's first run alone).</summary>
    [Fact]
    public void AWidthToFit_IsAskedForAPlainText_AndNotForMath()
    {
        var svg = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();

        svg.Draw(TextLabel.Of("word"), Font, new TextPlacement(new Point(0, 10), TextAlignment.Left) { FitWidth = 30 });
        svg.Draw(TextLabel.Of("$x^2$"), Font, new TextPlacement(new Point(0, 20), TextAlignment.Left) { FitWidth = 30 });

        Assert.Equal(1, svg.GetOutput().Split("textLength=\"30\"").Length - 1);
        Assert.Contains("textLength=\"30\" lengthAdjust=\"spacingAndGlyphs\">word</text>", svg.GetOutput());
    }
}
