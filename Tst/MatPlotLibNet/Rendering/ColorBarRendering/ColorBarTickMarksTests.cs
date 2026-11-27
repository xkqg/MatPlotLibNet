// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>The tick marks of a colour bar under the matplotlib themes, as matplotlib's colour-bar axes draws them. Printed with
/// matplotlib 3.11.2 for <c>fig.colorbar(im, ax=ax)</c>: the long axis' ticks stand on the bar's outer side only
/// (<c>tick1line</c> hidden, <c>tick2line</c> shown, its marker <c>TICKRIGHT</c> under the default style, pointing out, and
/// <c>TICKLEFT</c> under the classic one, pointing in), as long and as wide as <c>ytick.major.size</c> / <c>ytick.major.width</c>
/// (3.5 / 0.8 pt under the default style, 4 / 0.5 pt under the classic one) and black; the labels stand <c>ytick.major.pad</c> beyond
/// the part of the mark outside the bar (3.5 + 3.5 pt, 4 + 0 pt). A linear bar has no minor ticks (<c>get_minorticklocs()</c> is
/// empty: the long axis' minor locator is a <c>NullLocator</c>); a bar of a log norm over 1 to 1000 has major ticks on the powers
/// and minor ones on 2 to 9 times each power in view (<c>LogLocator(subs='auto')</c>), 2 pt long and 0.6 pt wide (0.5 under the classic
/// style). The library's own themes draw no tick marks on a colour bar.</summary>
public sealed class ColorBarTickMarksTests
{
    private static readonly double[,] Ramp = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 }, { 9, 10, 11 } };

    private static List<CallRecord> BarCalls(Theme theme, double[,] data, ColorBarOrientation orientation = ColorBarOrientation.Vertical,
        INormalizer? normalizer = null)
    {
        var figure = Plt.Create().WithTheme(theme).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(data, s => s.Normalizer = normalizer).WithColorBar(cb => cb with { Orientation = orientation })).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        int start = ctx.Calls.FindIndex(c => c is BeginGroupCall { CssClass: "colorbar" });
        int end = ctx.Calls.FindIndex(start, c => c is EndGroupCall);
        return ctx.Calls.GetRange(start + 1, end - start - 1);
    }

    private static List<(Point From, Point To, double Width, Color Color)> Marks(List<CallRecord> bar) =>
        [.. bar.OfType<DrawLineCall>()
            .Select(c => (c.P1, c.P2, c.Thickness, c.Color))];

    private static List<(string Text, Point At)> Labels(List<CallRecord> bar) =>
        [.. bar.OfType<DrawTextCall>().Select(c => (c.Text, c.Position))];

    private static readonly double Pt = 100.0 / 72;

    [Fact]
    public void UnderV2_AVerticalBar_DrawsAMarkOutwardAtEveryTick_AndItsLabelsPastThem()
    {
        var bar = BarCalls(Theme.MatplotlibV2, Ramp);

        var frame = bar.Frame().Rect;
        double right = frame.X + frame.Width;
        var marks = Marks(bar);
        var labels = Labels(bar);
        Assert.NotEmpty(labels);
        Assert.Equal(labels.Count, marks.Count);
        foreach (var (from, to, width, color) in marks)
        {
            Assert.Equal(from.Y, to.Y, 9);
            Assert.Equal(right + 3.5 * Pt, Math.Max(from.X, to.X), 9);
            Assert.True(Math.Min(from.X, to.X) <= right && Math.Min(from.X, to.X) >= right - 1, "the mark starts at the bar's edge");
            Assert.Equal(0.8 * Pt, width, 9);
            Assert.Equal(Colors.Black, color);
        }
        // A label stands the mark and the pad past the bar, at the height of its mark.
        Assert.All(labels, label => Assert.Equal(right + 3.5 * Pt + 3.5 * Pt, label.At.X, 9));
        Assert.Equal(labels.Select(l => l.At.Y - 0.35 * Theme.MatplotlibV2.TickFont().Size).Order(), marks.Select(m => m.From.Y).Order(), new Tolerance(1e-9));   // centred on its mark
    }

    [Fact]
    public void UnderClassic_AVerticalBar_PointsItsMarksIn_AndStandsItsLabelsThePadFromTheBar()
    {
        var bar = BarCalls(Theme.MatplotlibClassic, Ramp);

        var frame = bar.Frame().Rect;
        double right = frame.X + frame.Width;
        var marks = Marks(bar);
        Assert.NotEmpty(marks);
        foreach (var (from, to, width, _) in marks)
        {
            Assert.Equal(right - 4 * Pt, Math.Min(from.X, to.X), 9);
            Assert.True(Math.Max(from.X, to.X) >= right && Math.Max(from.X, to.X) <= right + 1, "the mark ends at the bar's edge");
            Assert.Equal(0.5 * Pt, width, 9);
        }
        Assert.All(Labels(bar), label => Assert.Equal(right + 4 * Pt, label.At.X, 9));
    }

    [Fact]
    public void UnderV2_AHorizontalBar_DrawsItsMarksDownward_AndItsLabelsBelowThem()
    {
        var bar = BarCalls(Theme.MatplotlibV2, Ramp, ColorBarOrientation.Horizontal);

        var frame = bar.Frame().Rect;
        double foot = frame.Y + frame.Height;
        var marks = Marks(bar);
        var labels = Labels(bar);
        Assert.Equal(labels.Count, marks.Count);
        Assert.All(marks, m =>
        {
            Assert.Equal(m.From.X, m.To.X, 9);
            Assert.Equal(foot + 3.5 * Pt, Math.Max(m.From.Y, m.To.Y), 9);
        });
        double size = Theme.MatplotlibV2.TickFont().Size;
        Assert.All(labels, label => Assert.Equal(foot + 7 * Pt + size, label.At.Y, 9));
        Assert.Equal(labels.Select(l => l.At.X).Order(), marks.Select(m => m.From.X).Order(), new Tolerance(1e-9));
    }

    /// <summary>A bar of a log norm over 1 to 1000: major marks on 1, 10, 100 and 1000, minor marks on 2 to 9, 20 to 90 and 200 to 900
    /// (24), each minor 2 pt long and 0.6 pt wide.</summary>
    [Fact]
    public void UnderV2_ABarOfALogNorm_DrawsMinorMarksBetweenThePowers()
    {
        var data = new double[,] { { 1, 10 }, { 100, 1000 } };
        var bar = BarCalls(Theme.MatplotlibV2, data, normalizer: new LogNormalizer());

        var marks = Marks(bar);
        var major = marks.Where(m => Math.Abs(m.Width - 0.8 * Pt) < 1e-9).ToList();
        var minor = marks.Where(m => Math.Abs(m.Width - 0.6 * Pt) < 1e-9).ToList();
        Assert.Equal(4, major.Count);
        Assert.Equal(24, minor.Count);
        Assert.Equal(marks.Count, major.Count + minor.Count);
        var frame = bar.Frame().Rect;
        Assert.All(minor, m => Assert.Equal(frame.X + frame.Width + 2 * Pt, Math.Max(m.From.X, m.To.X), 9));
    }

    /// <summary>The minor ticks of a log bar follow matplotlib's minor <c>LogLocator(subs='auto')</c>: none when ten or more powers
    /// stand in view (<c>n_avail >= 10</c>), and the ticks of <c>AutoLocator</c> when the view holds no more than one major and minor
    /// tick together, those that are not a power (3.11.2 over 1e0..1e12: none; over 2.5..3.5: 2.6, 2.8, 3.0, 3.2, 3.4 laid by
    /// <c>AutoLocator</c>).</summary>
    [Fact]
    public void TheMinorsOfALogBar_AreNoneOverTenPowers_AndAutoLocatorsInANarrowView()
    {
        Assert.Empty(LogMinors(1, 1e12));
        Assert.Equal(24, LogMinors(1, 1000).Length);
        Assert.Equal([2.6, 2.8, 3.0, 3.2, 3.4], LogMinors(2.5, 3.5), new Tolerance(1e-9));
    }

    /// <summary>The stride arm of the source (3.11.2, <c>LogLocator.tick_values</c>): with nine powers in view and room for two labels,
    /// the default style strides <c>9 // 3 + 1 = 4</c> decades, and a minor locator whose major ticks stride lays none. The classic mode
    /// asks for 15 ticks whatever the room (<c>numticks = 15</c> under <c>_internal.classic_mode</c>), so it strides
    /// <c>ceil(8 / 14) = 1</c> and lays the multiples: printed, a classic bar 1.6 inches tall over 1 .. 1e5 (tick space 3) has every
    /// power a major and 40 minor ticks in view, where the default style's (tick space 4) has the majors 1, 100, 10000 and none.</summary>
    [Fact]
    public void TheMinorsOfALogBar_AreNoneWhenTheMajorsStride_UnderTheDefaultStyle_AndTheClassicModeDoesNotStride()
    {
        Assert.Empty(MatPlotLibNet.Rendering.TickLocators.LogLocator.MinorsOver(new Range1D(1, 1e8), [], 2, Theme.MatplotlibV2));
        Assert.Equal(64, MatPlotLibNet.Rendering.TickLocators.LogLocator.MinorsOver(new Range1D(1, 1e8), [], 2, Theme.MatplotlibClassic).Length);
        Assert.Equal(40, MatPlotLibNet.Rendering.TickLocators.LogLocator.MinorsOver(new Range1D(1, 1e5), [], 3, Theme.MatplotlibClassic).Length);
        Assert.Empty(MatPlotLibNet.Rendering.TickLocators.LogLocator.MinorsOver(new Range1D(1, 1e5), [], 4, Theme.MatplotlibV2));
        Assert.Equal(24, MatPlotLibNet.Rendering.TickLocators.LogLocator.MinorsOver(new Range1D(1, 1000), [], 9, Theme.MatplotlibClassic).Length);
    }

    /// <summary>A log bar over 2 to 80 under the default style (printed: the major tick 10, the minor ticks 2 to 9 and 20 to 80, the
    /// minor labels 2, 3, 4, 6, 20, 30, 40 and 60 times their power): the label of a minor tick stands a minor mark and the minor pad
    /// from the bar (2 + 3.4 pt), the label of 10 a major mark and the major pad (3.5 + 3.5 pt); either way round.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void UnderV2_TheLabelOfAMinorTick_StandsFromItsMinorMark(ColorBarOrientation orientation)
    {
        var data = new double[,] { { 2, 20 }, { 40, 80 } };
        var bar = BarCalls(Theme.MatplotlibV2, data, orientation, new LogNormalizer());

        var frame = bar.Frame().Rect;
        bool vertical = orientation == ColorBarOrientation.Vertical;
        double edge = vertical ? frame.X + frame.Width : frame.Y + frame.Height;
        double size = Theme.MatplotlibV2.TickFont().Size;
        var labels = Labels(bar);
        var ten = Assert.Single(labels, l => l.Text == "10¹");
        Assert.Equal(edge + 7 * Pt + (vertical ? 0 : size), vertical ? ten.At.X : ten.At.Y, 9);
        var minors = labels.Where(l => l.Text != "10¹").ToList();
        Assert.NotEmpty(minors);
        Assert.All(minors, l => Assert.Equal(edge + 5.4 * Pt + (vertical ? 0 : size), vertical ? l.At.X : l.At.Y, 9));
        Assert.Single(Marks(bar), m => Math.Abs(m.Width - 0.8 * Pt) < 1e-9);
        Assert.Equal(15, Marks(bar).Count(m => Math.Abs(m.Width - 0.6 * Pt) < 1e-9));
    }

    /// <summary>A bar given a locator of its own under a log norm marks the locator's ticks as major ones and lays the log scale's minor
    /// marks between them, those on a major tick left out (matplotlib's colour bar takes the long axis' minor locator when it names
    /// only a major one).</summary>
    [Fact]
    public void ALogBarWithALocatorOfItsOwn_MarksItsTicks_AndTheMinorsBetween()
    {
        var data = new double[,] { { 1, 10 }, { 100, 1000 } };
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(data, s => s.Normalizer = new LogNormalizer())
                .WithColorBar(cb => cb with { Locator = new MatPlotLibNet.Rendering.TickLocators.FixedLocator([1.0, 10, 100, 1000]) })).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        int start = ctx.Calls.FindIndex(c => c is BeginGroupCall { CssClass: "colorbar" });
        int end = ctx.Calls.FindIndex(start, c => c is EndGroupCall);
        var marks = Marks(ctx.Calls.GetRange(start + 1, end - start - 1));

        Assert.Equal(4, marks.Count(m => Math.Abs(m.Width - 0.8 * Pt) < 1e-9));
        Assert.Equal(24, marks.Count(m => Math.Abs(m.Width - 0.6 * Pt) < 1e-9));
    }

    private static double[] LogMinors(double lo, double hi) =>
        MatPlotLibNet.Rendering.TickLocators.LogLocator.MinorsOver(new Range1D(lo, hi), [], 9, Theme.MatplotlibV2);

    /// <summary>A linear bar has no minor marks: every mark is a major one, one per label.</summary>
    [Fact]
    public void ALinearBar_HasNoMinorMarks()
    {
        var bar = BarCalls(Theme.MatplotlibV2, Ramp);

        Assert.All(Marks(bar), m => Assert.Equal(0.8 * Pt, m.Width, 9));
    }

    /// <summary>The library's own themes draw no tick mark on a bar and keep their labels 4 pixels from it.</summary>
    [Fact]
    public void UnderTheLibrarysOwnTheme_ABarHasNoMarks()
    {
        var bar = BarCalls(Theme.Default, Ramp);

        var frame = bar.Frame().Rect;
        Assert.Empty(Marks(bar));
        Assert.All(Labels(bar), label => Assert.Equal(frame.X + frame.Width + 4, label.At.X, 9));
    }

    private sealed class Tolerance(double eps) : IEqualityComparer<double>
    {
        public bool Equals(double a, double b) => Math.Abs(a - b) <= eps;
        public int GetHashCode(double value) => 0;
    }
}
