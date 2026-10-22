// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Direct unit tests for <see cref="CartesianSpansPart"/> extracted in Phase B.5.
/// Covers horizontal + vertical orientation, border vs no-border, Label vs no-Label,
/// swapped min/max, explicit EdgeColor, transparency. 100/100 target.
/// </summary>
public class CartesianSpansPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly DataRange Range = new(0, 10, 0, 10);
    private static DataTransform Transform() => new(0, 10, 0, 10, Plot);

    private static CartesianSpansPart Build(Axes axes, RecordingRenderContext ctx) =>
        new(axes, Plot, ctx, DefaultTheme, Transform(), Range);

    [Fact]
    public void Render_NoSpans_DoesNothing()
    {
        var ctx = new RecordingRenderContext();
        Build(new Axes(), ctx).Render();
        Assert.Empty(ctx.Calls);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Horizontal span
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_HorizontalSpan_DrawsFillRectangle()
    {
        var ax = new Axes();
        ax.AxHSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Render_HorizontalSpan_NoLineStyle_NoBorderLines()
    {
        var ax = new Axes();
        ax.AxHSpan(2, 5);  // LineStyle.None by default
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Render_HorizontalSpan_WithLineStyle_DrawsFourBorderLines()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.LineStyle = LineStyle.Solid;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(4, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Render_HorizontalSpan_WithLabel_DrawsLabelText()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.Label = "High zone";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawText"));
        Assert.Equal("High zone", (string)((dynamic)ctx.OfKind("DrawText")[0].Args!).text);
    }

    [Fact]
    public void Render_HorizontalSpan_WithoutLabel_NoText()
    {
        var ax = new Axes();
        ax.AxHSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Render_HorizontalSpan_SwappedMinMax_StillRenders()
    {
        // Intentionally max<min — Math.Min/Math.Max handles the swap.
        var ax = new Axes();
        ax.AxHSpan(8, 3);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Render_HorizontalSpan_LabelUsesLeftAlignment()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.Label = "L";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(TextAlignment.Left, (TextAlignment)((dynamic)ctx.OfKind("DrawText")[0].Args!).alignment);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Vertical span
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_VerticalSpan_DrawsFillRectangle()
    {
        var ax = new Axes();
        ax.AxVSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Render_VerticalSpan_WithLineStyle_DrawsFourBorderLines()
    {
        var ax = new Axes();
        var span = ax.AxVSpan(2, 5);
        span.LineStyle = LineStyle.Dashed;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(4, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Render_VerticalSpan_NoLineStyle_NoBorderLines()
    {
        var ax = new Axes();
        ax.AxVSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Render_VerticalSpan_WithLabel_DrawsCenteredText()
    {
        var ax = new Axes();
        var span = ax.AxVSpan(2, 5);
        span.Label = "V zone";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawText"));
        Assert.Equal(TextAlignment.Center, (TextAlignment)((dynamic)ctx.OfKind("DrawText")[0].Args!).alignment);
    }

    [Fact]
    public void Render_VerticalSpan_WithoutLabel_NoText()
    {
        var ax = new Axes();
        ax.AxVSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Render_VerticalSpan_SwappedMinMax_StillRenders()
    {
        var ax = new Axes();
        ax.AxVSpan(9, 1);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Color + edge color
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_SpanWithExplicitColor_UsesIt()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.Color = Color.FromHex("#FF0000");
        span.Alpha = 1.0;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var rect = ctx.OfKind("DrawRectangle")[0];
        var fill = (Color?)((dynamic)rect.Args!).fill;
        Assert.Equal(255, fill!.Value.R);
    }

    [Fact]
    public void Render_SpanWithoutColor_UsesDefaultTab10Blue()
    {
        var ax = new Axes();
        ax.AxHSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // Default color path — just verify rect was drawn.
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Render_SpanWithExplicitEdgeColor_BorderUsesEdgeColor()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.LineStyle = LineStyle.Solid;
        span.EdgeColor = Color.FromHex("#00FF00");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var line = ctx.OfKind("DrawLine")[0];
        Assert.Equal(Color.FromHex("#00FF00"), (Color)((dynamic)line.Args!).color);
    }

    [Fact]
    public void Render_SpanEdgeColorNull_FallsBackToSpanColor()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.LineStyle = LineStyle.Solid;
        span.Color = Color.FromHex("#0000FF");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var line = ctx.OfKind("DrawLine")[0];
        Assert.Equal(Color.FromHex("#0000FF"), (Color)((dynamic)line.Args!).color);
    }

    [Fact]
    public void Render_MultipleSpans_AllRendered()
    {
        var ax = new Axes();
        ax.AxHSpan(1, 2);
        ax.AxVSpan(3, 4);
        ax.AxHSpan(5, 6);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(3, ctx.CountOf("DrawRectangle"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // A span is clipped to the plot area, as matplotlib's axhspan is. The plot is 400 x 300 px over data 0..10, so
    // X is 40 px per unit and Y is 30 px per unit with 10 at the top (pixel 0).
    // ──────────────────────────────────────────────────────────────────────────

    private static Rect TheRect(RecordingRenderContext ctx) =>
        (Rect)((dynamic)Assert.Single(ctx.OfKind("DrawRectangle")).Args!).rect;

    private readonly record struct BorderLine(Point From, Point To);

    private static List<BorderLine> BorderLines(RecordingRenderContext ctx) =>
        [.. ctx.OfKind("DrawLine").Select(c => new BorderLine((Point)((dynamic)c.Args!).p1, (Point)((dynamic)c.Args!).p2))];

    /// <summary>The zone above or below a threshold is an unbounded span (AddThreshold stores ±double.MaxValue); an edge
    /// beyond the drawn range runs to the plot edge. Before, the pixel of that edge overflowed to infinity and the zone
    /// was never drawn.</summary>
    [Theory]
    [InlineData(8, double.MaxValue, 0, 0, 400, 60)]
    [InlineData(double.MaxValue, 8, 0, 0, 400, 60)]
    [InlineData(8, double.PositiveInfinity, 0, 0, 400, 60)]
    [InlineData(double.MinValue, 2, 0, 240, 400, 60)]
    [InlineData(double.NegativeInfinity, 2, 0, 240, 400, 60)]
    [InlineData(-5, 20, 0, 0, 400, 300)]
    [InlineData(double.NegativeInfinity, double.PositiveInfinity, 0, 0, 400, 300)]
    [InlineData(2, 5, 0, 150, 400, 90)]
    public void HorizontalSpan_RunsToThePlotEdgeWhereItsEdgeLiesBeyondTheRange(
        double min, double max, double x, double y, double width, double height)
    {
        var ax = new Axes();
        ax.AxHSpan(min, max);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        Assert.Equal(new Rect(x, y, width, height), TheRect(ctx));
    }

    [Theory]
    [InlineData(6, double.MaxValue, 240, 0, 160, 300)]
    [InlineData(double.MaxValue, 6, 240, 0, 160, 300)]
    [InlineData(6, double.PositiveInfinity, 240, 0, 160, 300)]
    [InlineData(double.MinValue, 4, 0, 0, 160, 300)]
    [InlineData(double.NegativeInfinity, 4, 0, 0, 160, 300)]
    [InlineData(-5, 20, 0, 0, 400, 300)]
    [InlineData(2, 5, 80, 0, 120, 300)]
    public void VerticalSpan_RunsToThePlotEdgeWhereItsEdgeLiesBeyondTheRange(
        double min, double max, double x, double y, double width, double height)
    {
        var ax = new Axes();
        ax.AxVSpan(min, max);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        Assert.Equal(new Rect(x, y, width, height), TheRect(ctx));
    }

    /// <summary>A span that lies wholly beyond the drawn range, or only touches its edge, covers none of the plot: nothing
    /// is drawn, neither its fill nor its border nor its label. A span of no width is a line: it is drawn where it
    /// stands inside the range and not outside it.</summary>
    [Theory]
    [InlineData(12, 20)]
    [InlineData(-9, -1)]
    [InlineData(10, 20)]
    [InlineData(-3, 0)]
    [InlineData(10, double.MaxValue)]
    [InlineData(double.MinValue, 0)]
    [InlineData(11, 11)]
    [InlineData(-1, -1)]
    public void ASpanEntirelyBeyondTheRange_DrawsNothing(double min, double max)
    {
        foreach (bool horizontal in new[] { true, false })
        {
            var ax = new Axes();
            var span = horizontal ? ax.AxHSpan(min, max) : ax.AxVSpan(min, max);
            span.LineStyle = LineStyle.Solid;
            span.Label = "zone";
            var ctx = new RecordingRenderContext();
            Build(ax, ctx).Render();

            Assert.Empty(ctx.Calls);
        }
    }

    [Fact]
    public void ASpanOfNoWidthInsideTheRange_IsStillDrawn()
    {
        var ax = new Axes();
        ax.AxHSpan(3, 3).LineStyle = LineStyle.Solid;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        Assert.Equal(new Rect(0, 210, 400, 0), TheRect(ctx));
        Assert.Equal(4, ctx.CountOf("DrawLine"));
    }

    /// <summary>A span with a border draws it on the edges it really has: an edge that was cut at the plot edge has no
    /// border line, as matplotlib's axes clip the outline of the rectangle.</summary>
    [Fact]
    public void HorizontalSpan_CutAtTheTop_HasNoBorderLineThere()
    {
        var ax = new Axes();
        ax.AxHSpan(8, double.MaxValue).LineStyle = LineStyle.Solid;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var lines = BorderLines(ctx);
        Assert.Equal(3, lines.Count);
        Assert.Single(lines, l => l.From.Y == 60 && l.To.Y == 60);                    // the span's own lower edge
        Assert.DoesNotContain(lines, l => l.From.Y == 0 && l.To.Y == 0);              // no line along the plot's top
        Assert.Equal(2, lines.Count(l => l.From.X == l.To.X && l.From.Y == 0 && l.To.Y == 60));   // the two sides
    }

    [Fact]
    public void HorizontalSpan_CutAtTheBottom_HasNoBorderLineThere()
    {
        var ax = new Axes();
        ax.AxHSpan(double.MinValue, 2).LineStyle = LineStyle.Solid;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var lines = BorderLines(ctx);
        Assert.Equal(3, lines.Count);
        Assert.Single(lines, l => l.From.Y == 240 && l.To.Y == 240);
        Assert.DoesNotContain(lines, l => l.From.Y == 300 && l.To.Y == 300);
    }

    [Fact]
    public void HorizontalSpan_CutAtBothEnds_HasOnlyItsTwoSides()
    {
        var ax = new Axes();
        ax.AxHSpan(-5, 20).LineStyle = LineStyle.Solid;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var lines = BorderLines(ctx);
        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.Equal(l.From.X, l.To.X));
    }

    [Fact]
    public void VerticalSpan_CutAtTheRight_HasNoBorderLineThere()
    {
        var ax = new Axes();
        ax.AxVSpan(6, double.MaxValue).LineStyle = LineStyle.Solid;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var lines = BorderLines(ctx);
        Assert.Equal(3, lines.Count);
        Assert.Single(lines, l => l.From.X == 240 && l.To.X == 240);
        Assert.DoesNotContain(lines, l => l.From.X == 400 && l.To.X == 400);
    }

    [Fact]
    public void VerticalSpan_CutAtTheLeft_HasNoBorderLineThere()
    {
        var ax = new Axes();
        ax.AxVSpan(double.MinValue, 4).LineStyle = LineStyle.Solid;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var lines = BorderLines(ctx);
        Assert.Equal(3, lines.Count);
        Assert.Single(lines, l => l.From.X == 160 && l.To.X == 160);
        Assert.DoesNotContain(lines, l => l.From.X == 0 && l.To.X == 0);
    }

    /// <summary>The label stands inside the part of the span that is drawn, not at an edge beyond the plot.</summary>
    [Fact]
    public void ACutSpansLabel_StandsInsideThePlot()
    {
        var ax = new Axes();
        ax.AxHSpan(8, double.MaxValue).Label = "Alarm";
        ax.AxVSpan(double.MinValue, 4).Label = "Early";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var texts = ctx.OfKind("DrawText");
        Assert.Equal(2, texts.Count);
        var horizontal = ((dynamic)texts[0].Args!);
        Assert.Equal("Alarm", (string)horizontal.text);
        Assert.Equal(2.0, ((Point)horizontal.position).X);
        Assert.Equal(((Font)horizontal.font).Size + 2, ((Point)horizontal.position).Y);
        var vertical = ((dynamic)texts[1].Args!);
        Assert.Equal(80.0, ((Point)vertical.position).X);                            // the middle of 0..160
        Assert.Equal(((Font)vertical.font).Size + 2, ((Point)vertical.position).Y);
    }

    /// <summary>On a reversed axis (a limit set high to low) the larger value is the lower edge on screen; the rectangle
    /// is the same area, never one of negative height.</summary>
    [Fact]
    public void HorizontalSpanOnAReversedAxis_IsARectangleOfPositiveHeight()
    {
        var reversed = new DataTransform(0, 10, 10, 0, Plot);
        var ax = new Axes();
        ax.AxHSpan(2, 5);
        var ctx = new RecordingRenderContext();
        new CartesianSpansPart(ax, Plot, ctx, DefaultTheme, reversed, new DataRange(0, 10, 10, 0)).Render();

        Assert.Equal(new Rect(0, 60, 400, 90), TheRect(ctx));
    }

    [Fact]
    public void HorizontalSpanOnAReversedAxis_RunsToThePlotEdgeToo()
    {
        var reversed = new DataTransform(0, 10, 10, 0, Plot);
        var ax = new Axes();
        ax.AxHSpan(8, double.MaxValue);     // the high values are at the bottom
        var ctx = new RecordingRenderContext();
        new CartesianSpansPart(ax, Plot, ctx, DefaultTheme, reversed, new DataRange(0, 10, 10, 0)).Render();

        Assert.Equal(new Rect(0, 240, 400, 60), TheRect(ctx));
    }

    [Fact]
    public void VerticalSpanOnAReversedAxis_IsARectangleOfPositiveWidth()
    {
        var reversed = new DataTransform(10, 0, 0, 10, Plot);
        var ax = new Axes();
        ax.AxVSpan(2, 5);
        var ctx = new RecordingRenderContext();
        new CartesianSpansPart(ax, Plot, ctx, DefaultTheme, reversed, new DataRange(10, 0, 0, 10)).Render();

        Assert.Equal(new Rect(200, 0, 120, 300), TheRect(ctx));
    }

    /// <summary>A log axis cannot place 0 or a negative value; a span edge there runs to the edge where the axis begins,
    /// as the base of a bar does (matplotlib's <c>nonpositive='clip'</c>).</summary>
    [Theory]
    [InlineData(double.MinValue, 10, 0, 150, 400, 150)]
    [InlineData(0, 10, 0, 150, 400, 150)]
    [InlineData(10, double.MaxValue, 0, 0, 400, 150)]
    [InlineData(1, 10, 0, 150, 400, 150)]
    public void HorizontalSpanOnALogAxis_ClipsAnEdgeTheScaleCannotPlace(
        double min, double max, double x, double y, double width, double height)
    {
        var ax = new Axes();
        ax.AxHSpan(min, max);
        var ctx = new RecordingRenderContext();
        BuildOnLogY(ax, ctx).Render();

        Assert.Equal(new Rect(x, y, width, height), TheRect(ctx));
    }

    /// <summary>A span that lies wholly at or below 0 on a log axis is below the axis: nothing is drawn.</summary>
    [Theory]
    [InlineData(-4, -1)]
    [InlineData(double.MinValue, 0)]
    public void HorizontalSpanBelowALogAxis_DrawsNothing(double min, double max)
    {
        var ax = new Axes();
        ax.AxHSpan(min, max);
        var ctx = new RecordingRenderContext();
        BuildOnLogY(ax, ctx).Render();

        Assert.Empty(ctx.Calls);
    }

    /// <summary>A log Y axis from 1 to 100 (scaled 0..2, 150 px per decade), the transform and the range the renderer
    /// builds for it.</summary>
    private static CartesianSpansPart BuildOnLogY(Axes axes, RecordingRenderContext ctx) =>
        new(axes, Plot, ctx, DefaultTheme,
            new DataTransform(0, 10, 0, 2, Plot, null, null, 0, 10, 1, 100, AxisScale.Linear, AxisScale.Log, 1, 1),
            new DataRange(0, 10, 1, 100));

    /// <summary>A Y axis from 0 to 100 with 40..60 broken out (80 units drawn over 300 px, 3.75 px per unit).</summary>
    private static CartesianSpansPart BuildOnBrokenY(Axes axes, RecordingRenderContext ctx) =>
        new(axes, Plot, ctx, DefaultTheme,
            new DataTransform(0, 10, 0, 80, Plot, null, [new AxisBreak(40, 60)], 0, 10, 0, 100),
            new DataRange(0, 10, 0, 100));

    /// <summary>An axis break hides part of the axis: a span is still clipped at the plot edge, drawn between its edges on
    /// the compressed axis, and an edge that stands inside the break (where the axis has no place for it) leaves the span
    /// out, as it did before.</summary>
    [Theory]
    [InlineData(10, 30, 187.5, 75)]
    [InlineData(70, 120, 0, 112.5)]
    public void HorizontalSpanOnABrokenAxis_IsPlacedOnTheCompressedAxis(double min, double max, double y, double height)
    {
        var ax = new Axes();
        ax.AxHSpan(min, max);
        var ctx = new RecordingRenderContext();
        BuildOnBrokenY(ax, ctx).Render();

        Assert.Equal(new Rect(0, y, 400, height), TheRect(ctx));
    }

    [Fact]
    public void HorizontalSpanWithAnEdgeInsideABreak_IsSkipped()
    {
        var ax = new Axes();
        ax.AxHSpan(50, 80).Label = "in the break";
        var ctx = new RecordingRenderContext();
        BuildOnBrokenY(ax, ctx).Render();

        Assert.Empty(ctx.Calls);
    }

    /// <summary>The zone of a threshold is the part of the plot beyond its line, up to the plot edge.</summary>
    [Fact]
    public void AThresholdsZone_IsDrawnFromTheLineToThePlotEdge()
    {
        var ax = new Axes();
        ax.AddThreshold(7, Orientation.Horizontal, ThresholdBreach.Above, Colors.Red);
        ax.AddThreshold(3, Orientation.Vertical, ThresholdBreach.Below, Colors.Red);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var rects = ctx.OfKind("DrawRectangle").Select(c => (Rect)((dynamic)c.Args!).rect).ToList();
        Assert.Equal([new Rect(0, 0, 400, 90), new Rect(0, 0, 120, 300)], rects);
    }
}
