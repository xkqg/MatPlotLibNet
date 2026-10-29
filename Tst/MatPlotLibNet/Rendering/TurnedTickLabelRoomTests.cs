// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// A tick label that turns stands as far beside its axis as the turn carries it, and the layout gives it that room: the first
/// label of a bottom axis, hung from its end and turned up, stands out to the left of the plot area by its turned width, and the
/// margin that held its upright width would put its ink off the figure. The room is reserved by the one rule on both layout paths
/// (the default one and tight/constrained layout), and it settles at the width the plot is drawn at, which decides whether the
/// labels turn at all.
/// </summary>
public class TurnedTickLabelRoomTests
{
    private static string[] Names(int count) => [.. Enumerable.Range(1, count).Select(i => $"Category number {i}")];

    private static Figure Bars(string[] names, double width, bool tight = false, Action<AxesBuilder>? configure = null)
    {
        var builder = Plt.Create().WithSize(width, 350);
        if (tight) builder = builder.TightLayout();
        return builder.AddSubPlot(1, 1, 1, ax =>
        {
            ax.Bar(names, [.. names.Select((_, i) => i + 1.0)]);
            configure?.Invoke(ax);
        }).Build();
    }

    /// <summary>The ink of every tick label of <paramref name="names"/> in the picture: the turned box of each, left and
    /// right, and whether any is turned.</summary>
    private readonly record struct Ink(double Left, double Right, int Turned);

    private static Ink InkOf(string svg, string[] names)
    {
        double left = double.MaxValue, right = double.MinValue;
        int turned = 0;
        var measure = new SvgRenderContext();
        foreach (var name in names)
        {
            var tag = Regex.Match(svg, "<text ([^>]*)>" + Regex.Escape(name) + "</text>");
            if (!tag.Success) continue;
            string attributes = tag.Groups[1].Value;
            string Attribute(string key) => Regex.Match(attributes, key + "=\"([^\"]*)\"").Groups[1].Value;
            double x = double.Parse(Attribute("x"), CultureInfo.InvariantCulture), y = double.Parse(Attribute("y"), CultureInfo.InvariantCulture);
            double size = double.Parse(Attribute("font-size"), CultureInfo.InvariantCulture);
            var turn = Regex.Match(attributes, "rotate\\((-?[\\d.]+)");
            double degrees = turn.Success ? -double.Parse(turn.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
            if (turn.Success && degrees != 0) turned++;
            var alignment = Attribute("text-anchor") switch { "end" => TextAlignment.Right, "middle" => TextAlignment.Center, _ => TextAlignment.Left };
            var box = new TextBox(new Point(x, y), measure.MeasureText(name, new Font { Family = Attribute("font-family"), Size = size }), size, alignment,
                TextVerticalAlignment.Baseline, degrees).Bounds();
            left = Math.Min(left, box.X);
            right = Math.Max(right, box.Right);
        }
        return new Ink(left, right, turned);
    }

    // ---- the first and last turned label stay on the figure ------------------------------------------------------

    [Theory]
    [InlineData(12, 500.0, false)]
    [InlineData(12, 500.0, true)]
    [InlineData(100, 500.0, false)]
    [InlineData(100, 500.0, true)]
    [InlineData(12, 200.0, false)]
    public void TurnedXLabels_StayOnTheFigure_OnBothLayoutPaths(int count, double width, bool tight)
    {
        var names = Names(count);
        string svg = Bars(names, width, tight).ToSvg();

        var ink = InkOf(svg, names);

        Assert.True(ink.Turned > 0, "the labels were expected to turn at this width");
        Assert.True(ink.Left >= ConstrainedLayoutEngine.PadLeft - 0.001, $"the first label's ink starts at {ink.Left:F1}, inside the pad of {ConstrainedLayoutEngine.PadLeft}");
        Assert.True(ink.Right <= width - ConstrainedLayoutEngine.PadRight + 0.001, $"the last label's ink ends at {ink.Right:F1}, past the pad of {ConstrainedLayoutEngine.PadRight} from the edge");
    }

    [Fact]
    public void TurnedXLabels_AtAnAngleOfTheirOwn_StayOnTheFigure()
    {
        var names = Names(12);
        string svg = Bars(names, 500, configure: ax => ax.WithXTickLabelRotation(45)).ToSvg();

        var ink = InkOf(svg, names);

        Assert.True(ink.Left >= ConstrainedLayoutEngine.PadLeft - 0.001, $"ink starts at {ink.Left:F1}");
        Assert.True(ink.Right <= 500 - ConstrainedLayoutEngine.PadRight + 0.001, $"ink ends at {ink.Right:F1}");
    }

    /// <summary>The room is what the turn needs and no more: the first label's ink stands within a pixel of the pad, so a figure
    /// that needs the room does not waste any.</summary>
    [Fact]
    public void TheRoomReserved_IsWhatTheTurnNeeds_NoMoreThanAPixelOver()
    {
        var names = Names(12);
        string svg = Bars(names, 500).ToSvg();

        var ink = InkOf(svg, names);

        Assert.InRange(ink.Left, ConstrainedLayoutEngine.PadLeft - 0.001, ConstrainedLayoutEngine.PadLeft + 1);
    }

    /// <summary>The rotation the layout measures at is the rotation the drawing draws at, across widths: all the labels turn or
    /// none does, and where they turn their ink is on the figure.</summary>
    [Fact]
    public void AcrossWidths_TheLabelsAreAllTurnedOrNone_AndTheTurnedOnesStayOnTheFigure()
    {
        var names = Names(12);
        for (double width = 300; width <= 700; width += 7)
        {
            string svg = Bars(names, width).ToSvg();
            var ink = InkOf(svg, names);

            Assert.True(ink.Turned == 0 || ink.Turned == names.Length, $"at {width} px {ink.Turned} of {names.Length} labels are turned");
            if (ink.Turned > 0)
            {
                Assert.True(ink.Left >= ConstrainedLayoutEngine.PadLeft - 0.001, $"at {width} px the ink starts at {ink.Left:F1}");
                Assert.True(ink.Right <= width - ConstrainedLayoutEngine.PadRight + 0.001, $"at {width} px the ink ends at {ink.Right:F1}");
            }
        }
    }

    // ---- labels that do not turn are laid out as before -------------------------------------------------------------

    [Fact]
    public void UprightLabels_ThatFit_KeepTheLayoutTheyHad()
    {
        double[] ramp = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        string svg = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax.Plot(ramp, ramp)).ToSvg();

        var plot = Regex.Matches(svg, @"<rect x=""([\d.]+)"" y=""([\d.]+)"" width=""([\d.]+)"" height=""([\d.]+)"" fill=""#FFFFFF"" />")[1];

        Assert.Equal("62", plot.Groups[1].Value);
        Assert.Equal("388", plot.Groups[3].Value);
    }

    // ---- a Y label at its own turn -----------------------------------------------------------------------------------

    [Fact]
    public void TurnedYLabels_StayOnTheFigure_AndReserveTheirTurnedWidth()
    {
        double[] x = [1, 2, 3];
        string svg = Plt.Create().WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(x, [1000.0, 20000.0, 300000.0]).WithYTickLabelRotation(60)).ToSvg();

        double leftmost = double.MaxValue;
        var measure = new SvgRenderContext();
        foreach (Match tag in Regex.Matches(svg, "<text ([^>]*rotate[^>]*)>([^<]*)</text>"))
        {
            string attributes = tag.Groups[1].Value;
            string Attribute(string key) => Regex.Match(attributes, key + "=\"([^\"]*)\"").Groups[1].Value;
            if (Attribute("text-anchor") != "end") continue;
            double x0 = double.Parse(Attribute("x"), CultureInfo.InvariantCulture), y0 = double.Parse(Attribute("y"), CultureInfo.InvariantCulture);
            double size = double.Parse(Attribute("font-size"), CultureInfo.InvariantCulture);
            double degrees = -double.Parse(Regex.Match(attributes, "rotate\\((-?[\\d.]+)").Groups[1].Value, CultureInfo.InvariantCulture);
            var box = new TextBox(new Point(x0, y0), measure.MeasureText(tag.Groups[2].Value, new Font { Family = Attribute("font-family"), Size = size }), size,
                TextAlignment.Right, TextVerticalAlignment.Baseline, degrees).Bounds();
            leftmost = Math.Min(leftmost, box.X);
        }

        Assert.True(leftmost >= ConstrainedLayoutEngine.PadLeft - 0.001, $"the Y labels' ink starts at {leftmost:F1}, inside the pad");
    }

    // ---- what it costs ------------------------------------------------------------------------------------------------

    /// <summary>A context that counts how many times text is measured, and measures it as the vector context does.</summary>
    private sealed class MeasureCounter : IRenderContext
    {
        private readonly SvgRenderContext _inner = new();
        public int Measured { get; private set; }

        public Size MeasureText(string text, Font font)
        {
            Measured++;
            return _inner.MeasureText(text, font);
        }

        public void DrawLine(Point p1, Point p2, StrokeStyle stroke) => _inner.DrawLine(p1, p2, stroke);
        public void DrawLines(IReadOnlyList<Point> points, StrokeStyle stroke) => _inner.DrawLines(points, stroke);
        public void DrawPolygon(IReadOnlyList<Point> points, ShapeStyle shape) => _inner.DrawPolygon(points, shape);
        public void DrawCircle(Point center, double radius, ShapeStyle shape) => _inner.DrawCircle(center, radius, shape);
        public void DrawRectangle(Rect rect, ShapeStyle shape) => _inner.DrawRectangle(rect, shape);
        public void DrawEllipse(Rect bounds, ShapeStyle shape) => _inner.DrawEllipse(bounds, shape);
        public void DrawText(string text, Point position, Font font, TextAlignment alignment) => _inner.DrawText(text, position, font, alignment);
        public void DrawPath(IReadOnlyList<PathSegment> segments, ShapeStyle shape) => _inner.DrawPath(segments, shape);
        public void PushClip(Rect clipRect) => _inner.PushClip(clipRect);
        public void PopClip() => _inner.PopClip();
        public void SetOpacity(double opacity) => _inner.SetOpacity(opacity);
    }

    /// <summary>Settling the room measures the labels again at the width that remains; it does so a bounded number of times, so
    /// laying out a figure whose labels turn measures text a few times over, not without limit. The bound is pinned: three
    /// times what the same figure costs when its labels are upright and nothing stands out.</summary>
    [Fact]
    public void SettlingTheRoom_MeasuresTextAFewTimesOver_NotWithoutLimit()
    {
        int Measures(string[] names, double width)
        {
            var counter = new MeasureCounter();
            new ChartRenderer().ComputeLayout(Bars(names, width), counter);
            return counter.Measured;
        }

        int upright = Measures(Names(12), 1400);   // wide enough that no label turns: one measure of the labels at the width given
        int turned = Measures(Names(12), 500);

        Assert.True(turned <= 3 * upright, $"{turned} text measures with the labels turned against {upright} upright");
    }
}
