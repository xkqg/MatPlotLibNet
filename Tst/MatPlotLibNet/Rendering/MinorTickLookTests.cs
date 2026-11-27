// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The width and the pad of a minor tick where its author names none. matplotlib 3.11.2, printed under each style:
/// <c>xtick.minor.width</c> and <c>ytick.minor.width</c> are 0.6 points in the default style and 0.5 in the classic one,
/// <c>xtick.minor.pad</c> and <c>ytick.minor.pad</c> 3.4 and 4.0 (the major pad is 3.5 and 4.0), <c>xtick.minor.size</c> 2.0 in both.
/// A minor tick the library's own themes draw is 2 points long, 0.6 wide, with a pad of 3.5; one that names a width or a pad is drawn as
/// named, even when it names the number a new axis reads.</summary>
public class MinorTickLookTests
{
    private static readonly double Two = 2.0.PointsInPixels;

    private static TickConfig Drawn(Theme theme, Func<TickConfig, TickConfig>? change = null)
    {
        var minor = new Axis().MinorTicks;
        return (change?.Invoke(minor) ?? minor).DrawnUnder(theme, major: false);
    }

    [Fact]
    public void AMinorTickOfAnAxis_ReadsTheSameValuesAsItAlwaysDid()
    {
        var minor = new Axis().MinorTicks;

        Assert.Equal(2.0.PointsInPixels, minor.Length, 9);
        Assert.Equal(0.6.PointsInPixels, minor.Width, 9);
        Assert.Equal(3.5.PointsInPixels, minor.Pad, 9);
        Assert.Null(minor.GivenLength);   // nothing was given: the resting size is the axis', not the author's
        Assert.Null(minor.GivenWidth);
    }

    [Theory]
    [InlineData("v2", 0.6, 3.4)]
    [InlineData("classic", 0.5, 4.0)]
    [InlineData("default", 0.6, 3.5)] // the library's own themes: a minor tick keeps its own width and pad
    public void AMinorTick_IsTwoPointsLong_AndItsWidthAndPadAreThoseOfTheTheme(string theme, double widthPoints, double padPoints)
    {
        var minor = Drawn(theme.AsTheme());

        Assert.Equal(widthPoints.PointsInPixels, minor.Width, 9);
        Assert.Equal(padPoints.PointsInPixels, minor.Pad, 9);
        Assert.Equal(Two, minor.Length, 9);
    }

    [Fact]
    public void AMinorWidthAndPadThatAreNamed_AreDrawnAsNamed_EvenWhenTheyEqualTheOnesANewAxisReads()
    {
        var classic = Drawn(Theme.MatplotlibClassic, m => m with { Width = 0.6.PointsInPixels, Pad = 3.5.PointsInPixels });
        var v2 = Drawn(Theme.MatplotlibV2, m => m with { Width = 2.0, Pad = 3.5.PointsInPixels });

        Assert.Equal(0.6.PointsInPixels, classic.Width, 9);
        Assert.Equal(3.5.PointsInPixels, classic.Pad, 9);
        Assert.Equal(2.0, v2.Width, 9);
        Assert.Equal(3.5.PointsInPixels, v2.Pad, 9);
    }

    [Fact]
    public void TheMajorTicks_AreUntouchedByTheMinorValues()
    {
        var major = new Axis().MajorTicks.DrawnUnder(Theme.MatplotlibV2, major: true);

        Assert.Equal(0.8.PointsInPixels, major.Width, 9);
        Assert.Equal(3.5.PointsInPixels, major.Pad, 9);
    }

    // ── drawn ─────────────────────────────────────────────────────────────────────────────────────────────────

    private static Figure Built(Theme theme, Func<TickConfig, TickConfig>? named = null)
    {
        var figure = Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).WithMinorTicks()).Build();
        if (named is not null) figure.SubPlots[0].XAxis.MinorTicks = named(figure.SubPlots[0].XAxis.MinorTicks);
        return figure;
    }

    private static XDocument Doc(Theme theme, Func<TickConfig, TickConfig>? named = null) => Built(theme, named).ToSvg().Xml();

    /// <summary>The widths of the short vertical marks on the X axis' edge: the minor ticks, which are shorter than the major ones (the
    /// marks stand a little over their length: the stroke's caps).</summary>
    private static List<double> MinorWidths(XDocument doc)
    {
        return [.. doc.Each("line").Where(e => e.Attribute("stroke-width") is not null && e.Number("x1") == e.Number("x2"))
            .Where(e => Math.Abs(e.Number("y2") - e.Number("y1")) < 4.5)
            .Select(e => e.Number("stroke-width"))];
    }

    [Fact]
    public void TheMinorTicksOfAFigure_AreDrawnAsWideAsTheThemeSays()
    {
        var v2 = MinorWidths(Doc(Theme.MatplotlibV2));
        var classic = MinorWidths(Doc(Theme.MatplotlibClassic));

        Assert.NotEmpty(v2);
        Assert.All(v2, w => Assert.Equal(0.6.PointsInPixels, w, 2));
        Assert.NotEmpty(classic);
        Assert.All(classic, w => Assert.Equal(0.5.PointsInPixels, w, 2));
    }

    [Fact]
    public void AMinorWidthThatIsNamed_SurvivesTheJson_AndOneThatIsNot_StaysUnnamed()
    {
        string Json(Func<TickConfig, TickConfig>? named) => Built(Theme.MatplotlibClassic, named).ToJson();

        string unnamed = Json(null);
        string named = Json(m => m with { Width = 0.6.PointsInPixels });

        Assert.All(MinorWidths(ChartSerializerDefault.Current.FromJson(unnamed).ToSvg().Xml()), w => Assert.Equal(0.5.PointsInPixels, w, 2));
        Assert.All(MinorWidths(ChartSerializerDefault.Current.FromJson(named).ToSvg().Xml()), w => Assert.Equal(0.6.PointsInPixels, w, 2));
    }

}
