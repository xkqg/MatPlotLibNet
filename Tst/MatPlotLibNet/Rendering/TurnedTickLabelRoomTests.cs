// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

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
            var tag = svg.Elements("text").FirstOrDefault(text => text.Text == name);
            if (tag.Attributes is null) continue;
            double size = tag.Number("font-size");
            double? turn = tag.Rotation();
            double degrees = turn is { } angle ? -angle : 0;
            if (turn.HasValue && degrees != 0) turned++;
            var alignment = tag["text-anchor"] switch { "end" => TextAlignment.Right, "middle" => TextAlignment.Center, _ => TextAlignment.Left };
            var box = new TextBox(new TextPlacement(new Point(tag.Number("x"), tag.Number("y")), alignment) { Rotation = degrees },
                TextExtent.Of(measure.MeasureText(name, new Font { Family = tag["font-family"] ?? string.Empty, Size = size }), size)).Bounds();
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
        Assert.True(ink.Left >= ConstrainedLayoutEngine.PadLeft - SvgMarkupExtensions.WrittenPrecision, $"the first label's ink starts at {ink.Left:F4}, inside the pad of {ConstrainedLayoutEngine.PadLeft}");
        Assert.True(ink.Right <= width - ConstrainedLayoutEngine.PadRight + SvgMarkupExtensions.WrittenPrecision, $"the last label's ink ends at {ink.Right:F1}, past the pad of {ConstrainedLayoutEngine.PadRight} from the edge");
    }

    [Fact]
    public void TurnedXLabels_AtAnAngleOfTheirOwn_StayOnTheFigure()
    {
        var names = Names(12);
        string svg = Bars(names, 500, configure: ax => ax.WithXTickLabelRotation(45)).ToSvg();

        var ink = InkOf(svg, names);

        Assert.True(ink.Left >= ConstrainedLayoutEngine.PadLeft - SvgMarkupExtensions.WrittenPrecision, $"ink starts at {ink.Left:F1}");
        Assert.True(ink.Right <= 500 - ConstrainedLayoutEngine.PadRight + SvgMarkupExtensions.WrittenPrecision, $"ink ends at {ink.Right:F1}");
    }

    /// <summary>The room is what the turn needs and no more: the first label's ink stands within a pixel of the pad, so a figure
    /// that needs the room does not waste any.</summary>
    [Fact]
    public void TheRoomReserved_IsWhatTheTurnNeeds_NoMoreThanAPixelOver()
    {
        var names = Names(12);
        string svg = Bars(names, 500).ToSvg();

        var ink = InkOf(svg, names);

        Assert.InRange(ink.Left, ConstrainedLayoutEngine.PadLeft - SvgMarkupExtensions.WrittenPrecision, ConstrainedLayoutEngine.PadLeft + 1);
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
                Assert.True(ink.Left >= ConstrainedLayoutEngine.PadLeft - SvgMarkupExtensions.WrittenPrecision, $"at {width} px the ink starts at {ink.Left:F4}");
                Assert.True(ink.Right <= width - ConstrainedLayoutEngine.PadRight + SvgMarkupExtensions.WrittenPrecision, $"at {width} px the ink ends at {ink.Right:F1}");
            }
        }
    }

    // ---- labels that do not turn are laid out as before -------------------------------------------------------------

    [Fact]
    public void UprightLabels_ThatFit_KeepTheLayoutTheyHad()
    {
        double[] ramp = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        string svg = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax.Plot(ramp, ramp)).ToSvg();

        var plot = svg.Elements("rect").Where(rect => rect["fill"] == "#FFFFFF").ElementAt(1);

        Assert.Equal("62", plot["x"]);
        Assert.Equal("388", plot["width"]);
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
        foreach (var tag in svg.Elements("text").Where(text => text.Rotation().HasValue && text["text-anchor"] == "end"))
        {
            double size = tag.Number("font-size");
            double degrees = -tag.Rotation()!.Value;
            var box = new TextBox(new TextPlacement(new Point(tag.Number("x"), tag.Number("y")), TextAlignment.Right) { Rotation = degrees },
                TextExtent.Of(measure.MeasureText(tag.Text, new Font { Family = tag["font-family"] ?? string.Empty, Size = size }), size)).Bounds();
            leftmost = Math.Min(leftmost, box.X);
        }

        Assert.True(leftmost >= ConstrainedLayoutEngine.PadLeft - SvgMarkupExtensions.WrittenPrecision, $"the Y labels' ink starts at {leftmost:F1}, inside the pad");
    }

    // ---- what it costs ------------------------------------------------------------------------------------------------

    /// <summary>A context that counts how many times text is measured, and measures it as the vector context does.</summary>
    private sealed class MeasureCounter : SilentRenderContext
    {
        private readonly SvgRenderContext _inner = new();
        public int Measured { get; private set; }

        public override Size MeasureText(string text, Font font)
        {
            Measured++;
            return _inner.MeasureText(text, font);
        }
    }

    /// <summary>Settling the room measures the labels again at the width that remains; it does so a bounded number of times, so
    /// laying out a figure whose labels turn measures text a few times over, not without limit. The bound is pinned: at most
    /// the four passes the loop allows and the ends measured once more at the plots it settled on, five times what the same
    /// figure costs when its labels are upright and nothing stands out, whose one pass hands its ends on.</summary>
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

        Assert.True(turned <= 5 * upright, $"{turned} text measures with the labels turned against {upright} upright");
    }
}
