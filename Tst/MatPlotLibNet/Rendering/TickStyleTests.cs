// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>How the ticks of a 2-D axis are drawn under each theme. matplotlib 3.11.2, measured on a tick of each style:
/// the default style's ticks point out, 3.5 pt long, 0.8 pt wide, 3.5 pt from their labels, on the bottom and left edges only;
/// the classic style's point in (<c>xtick.direction 'in'</c>), 4 pt long, 0.5 pt wide, 4 pt from their labels, and stand on
/// the top and right edges too (<c>xtick.top</c>, <c>ytick.right</c>), without labels there. A label stands its pad past the
/// part of the mark outside the axes (<c>Tick.get_tick_padding</c>: <c>self._size * {'in': 0.0, 'inout': 0.5, 'out':
/// 1.0}[self._tickdir]</c>), and an in-out mark is the marker <c>'|'</c>, centred on the axis.</summary>
public sealed class TickStyleTests
{
    private static readonly double Pt = 100.0 / 72.0;

    [Fact]
    public void TheClassicTheme_TicksPointIn_FourPointsLong_HalfAPointWide_FourPointsFromTheirLabels()
    {
        var major = new TickConfig().DrawnUnder(Theme.MatplotlibClassic, major: true);
        var minor = new Axis().MinorTicks.DrawnUnder(Theme.MatplotlibClassic, major: false);

        Assert.Equal(TickDirection.In, major.Direction);
        Assert.Equal(4 * Pt, major.Length, 9);
        Assert.Equal(0.5 * Pt, major.Width, 9);
        Assert.Equal(4 * Pt, major.Pad, 9);
        Assert.Equal(TickDirection.In, minor.Direction);
        Assert.Equal(2.0.PointsInPixels, minor.Length, 9);
    }

    [Fact]
    public void V2AndTheLibrarysThemes_DrawTheTicksAsTheyAre()
    {
        var config = new TickConfig();

        Assert.Same(config, config.DrawnUnder(Theme.MatplotlibV2, major: true));
        Assert.Same(config, config.DrawnUnder(Theme.Default, major: true));
    }

    /// <summary>A direction, a length, a width or a pad the author gave wins over the theme's, the resting value included.</summary>
    [Fact]
    public void AGivenSetting_WinsOverTheTheme()
    {
        var given = new TickConfig { Direction = TickDirection.Out, Length = 3.5 * Pt }.DrawnUnder(Theme.MatplotlibClassic, major: true);

        Assert.Equal(TickDirection.Out, given.Direction);
        Assert.Equal(3.5 * Pt, given.Length, 9);
        Assert.Equal(0.5 * Pt, given.Width, 9);
    }

    [Theory]
    [InlineData(TickDirection.In, 3)]
    [InlineData(TickDirection.InOut, 8)]
    [InlineData(TickDirection.Out, 13)]
    public void ALabelStandsItsPadPastThePartOfTheMarkOutsideTheAxes(TickDirection direction, double band)
    {
        var config = new TickConfig { Direction = direction, Length = 10, Pad = 3 };

        Assert.Equal(band + 20, config.TickLabelBand(20), 9);
    }

    [Fact]
    public void AnInOutMark_IsCentredOnTheAxis()
    {
        var ctx = new RecordingRenderContext();

        ctx.DrawTickMark(50, 100, EdgeTerms.Bottom, TickMark.Of(new TickConfig { Length = 10, Width = 1, Direction = TickDirection.InOut }, Colors.Black, 0));

        var line = ctx.Of<DrawLineCall>().Single();
        Assert.Equal(95, line.P1.Y, 9);
        Assert.Equal(105, line.P2.Y, 9);
    }

    private readonly record struct Line(double X1, double Y1, double X2, double Y2);

    private static List<Line> Lines(string svg) =>
        [.. svg.Elements("line").Select(l => new Line(l.Number("x1"), l.Number("y1"), l.Number("x2"), l.Number("y2")))];

    private static string Svg(Theme theme) => Plt.Create().WithTheme(theme).WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1, 2], [0.0, 1, 2])).ToSvg();

    /// <summary>Under the classic theme the top and right edges carry tick marks as well, pointing into the plot, and no label;
    /// the bottom marks point into the plot too. Under v2 the top edge carries none.</summary>
    [Fact]
    public void TheClassicTheme_MarksTheTopAndRightEdges_IntoThePlot_WithoutLabels()
    {
        string svg = Svg(Theme.MatplotlibClassic);
        var lines = Lines(svg);
        var horizontal = lines.Where(l => Math.Abs(l.Y1 - l.Y2) < 1e-9).OrderByDescending(l => Math.Abs(l.X2 - l.X1)).ToList();
        double top = horizontal.Take(2).Min(l => l.Y1), bottom = horizontal.Take(2).Max(l => l.Y1);
        var vertical = lines.Where(l => Math.Abs(l.X1 - l.X2) < 1e-9 && Math.Abs(l.Y2 - l.Y1) < 10).ToList();

        var topMarks = vertical.Where(l => Math.Abs(Math.Min(l.Y1, l.Y2) - top) < 1.5).ToList();
        var bottomMarks = vertical.Where(l => Math.Abs(Math.Max(l.Y1, l.Y2) - bottom) < 1.5).ToList();

        Assert.NotEmpty(topMarks);
        Assert.All(topMarks, m => Assert.True(Math.Max(m.Y1, m.Y2) > top + 4, "a top mark points down into the plot"));
        Assert.NotEmpty(bottomMarks);
        Assert.All(bottomMarks, m => Assert.True(Math.Min(m.Y1, m.Y2) < bottom - 4, "a bottom mark points up into the plot"));
        Assert.DoesNotContain(svg.Each("text"), t => t.Number("y") < top && Regex.IsMatch(t.Value, "^[0-9.]+$"));

        var v2Lines = Lines(Svg(Theme.MatplotlibV2));
        double v2Top = v2Lines.Where(l => Math.Abs(l.Y1 - l.Y2) < 1e-9).OrderByDescending(l => Math.Abs(l.X2 - l.X1)).Take(2).Min(l => l.Y1);
        Assert.DoesNotContain(v2Lines, l => Math.Abs(l.X1 - l.X2) < 1e-9 && Math.Abs(l.Y2 - l.Y1) < 10
            && Math.Abs(Math.Min(l.Y1, l.Y2) - v2Top) < 1.5);
    }

    /// <summary>The theme's tick style survives a theme derived from it and the theme's JSON; a direction an author gave, the
    /// resting one included, survives the figure's JSON, so it still wins over the theme when read back.</summary>
    [Fact]
    public void TheStyle_SurvivesADerivedThemeAndTheJson_AndAGivenDirectionSurvivesTheFiguresJson()
    {
        var derived = new ThemeBuilder(Theme.MatplotlibClassic).Build();
        var read = ThemeDto.From(Theme.MatplotlibClassic).ToTheme();
        Assert.Equal(TickDirection.In, new TickConfig().DrawnUnder(derived, major: true).Direction);
        Assert.Equal(TickDirection.In, new TickConfig().DrawnUnder(read, major: true).Direction);
        Assert.True(read.Looks.Ticks.OnAllSides);

        var figure = Plt.Create().WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [0.0, 1])).Build();
        figure.SubPlots[0].XAxis.MajorTicks = new TickConfig { Direction = TickDirection.Out };
        var back = figure.RoundTripped();

        Assert.Equal(TickDirection.Out, back.SubPlots[0].XAxis.MajorTicks.DrawnUnder(Theme.MatplotlibClassic, major: true).Direction);
        Assert.Equal(TickDirection.In, back.SubPlots[0].YAxis.MajorTicks.DrawnUnder(Theme.MatplotlibClassic, major: true).Direction);
    }
}
