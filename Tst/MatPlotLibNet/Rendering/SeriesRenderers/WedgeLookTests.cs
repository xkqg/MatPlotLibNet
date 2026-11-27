// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Where the wedges of a pie or a donut start, which way they follow each other and how a pie's name is aligned beside its wedge
/// are values of the theme's wedge look (<see cref="WedgeLook"/>) wherever the author gave none. Under the matplotlib themes they are
/// matplotlib 3.11.2's, read with python: <c>Axes.pie(..., startangle=0, counterclock=True)</c> (a donut is a <c>pie</c> with a wedge
/// width, so it follows the same rule) and <c>Axes.pie_label</c>'s <c>label_alignment_h = 'left' if xt &gt; 0 else 'right'</c>; printed:
/// <c>[(w.theta1, w.theta2) for w in ax.pie([35, 25, 20, 15, 5])[0]]</c> = (0, 126), (126, 216), (216, 288), (288, 342), (342, 360).
/// The library's own themes keep theirs: from the top, clockwise, a name centred above or below its wedge.</summary>
public class WedgeLookTests
{
    [Theory]
    [InlineData("MatplotlibV2", 0, true)]
    [InlineData("MatplotlibClassic", 0, true)]
    [InlineData("Default", 90, false)]
    [InlineData("Dark", 90, false)]
    public void AWedgeChartGivenNoStartOrDirection_TakesItsThemes(string themeName, double start, bool counterClockwise)
    {
        var theme = themeName.AsTheme();
        WedgeSeries[] charts = [new PieSeries([1.0, 2.0]), new DonutSeries([1.0, 2.0])];

        Assert.All(charts, chart => Assert.Equal(start, chart.StartAngleUnder(theme)));
        Assert.All(charts, chart => Assert.Equal(counterClockwise, chart.CounterClockwiseUnder(theme)));
    }

    [Theory]
    [InlineData("MatplotlibV2")]
    [InlineData("Default")]
    public void AGivenStartAndDirection_AreDrawnAsGiven_UnderEveryTheme(string themeName)
    {
        var theme = themeName.AsTheme();
        var pie = new PieSeries([1.0, 2.0]) { StartAngle = 90, CounterClockwise = false };

        Assert.Equal(90, pie.StartAngleUnder(theme));
        Assert.False(pie.CounterClockwiseUnder(theme));
    }

    [Fact]
    public void TheJson_WritesWhatWasGiven_AndAnUngivenStartStaysUngivenThroughIt()
    {
        var resting = new PieSeries([1.0, 2.0]);
        var givenAtTheLibrarysRest = new PieSeries([1.0, 2.0]) { StartAngle = 90, CounterClockwise = false };

        Assert.Null(resting.ToSeriesDto().StartAngle);
        Assert.Null(resting.ToSeriesDto().CounterClockwise);
        Assert.Equal(90, givenAtTheLibrarysRest.ToSeriesDto().StartAngle);
        Assert.Equal(false, givenAtTheLibrarysRest.ToSeriesDto().CounterClockwise);

        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Pie([1.0, 2.0])).Build();
        var back = (PieSeries)figure.RoundTripped().SubPlots[0].Series[0];
        Assert.Null(back.ToSeriesDto().StartAngle);
        Assert.Equal(0, back.StartAngleUnder(Theme.MatplotlibV2));
    }

    /// <summary>The second of four equal wedges: from 90 to 180 degrees counter-clockwise under the matplotlib themes (up and to the
    /// left), from 0 to -90 clockwise under the library's (down and to the right).</summary>
    [Theory]
    [InlineData("MatplotlibV2", -1, -1)]
    [InlineData("MatplotlibClassic", -1, -1)]
    [InlineData("Default", 1, 1)]
    public void TheSecondOfFourEqualWedges_StandsWhereItsThemeLaysIt(string themeName, int sideX, int sideY)
    {
        var figure = Plt.Create().WithTheme(themeName.AsTheme()).WithSize(600, 600)
            .AddSubPlot(1, 1, 1, ax => ax.Pie([1.0, 1, 1, 1])).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);

        var wedges = ctx.Of<DrawPathCall>().Select(c => c.Segments).ToList();
        var points = Points(wedges[1]);
        var centre = Centre(wedges);

        Assert.Equal(sideX, Math.Sign(points.Average(p => p.X) - centre.X));
        Assert.Equal(sideY, Math.Sign(points.Average(p => p.Y) - centre.Y));
    }

    /// <summary>A wedge whose middle stands at 87 degrees: its name is a little right of the top of the pie, where matplotlib starts
    /// the text there (<c>'left' if xt &gt; 0</c>) and the library's own themes centre it.</summary>
    [Theory]
    [InlineData("MatplotlibV2", "start")]
    [InlineData("Default", "middle")]
    public void ANameJustRightOfTheTop_IsAlignedByItsTheme(string themeName, string anchor)
    {
        // A share of 87/180 laid counter-clockwise from 0 has its middle at 87 degrees.
        var svg = Plt.Create().WithTheme(themeName.AsTheme()).WithSize(600, 600)
            .AddSubPlot(1, 1, 1, ax => ax.Pie([87.0, 93.0], ["near the top", "rest"], s => { s.StartAngle = 0; s.CounterClockwise = true; })
                .WithLegend(legend => legend with { Visible = false }))
            .Build().ToSvg();

        var label = svg.Each("text").Single(e => e.Value == "near the top");

        Assert.Equal(anchor, (string?)label.Attribute("text-anchor") ?? "start");
    }

    [Theory]
    [InlineData("MatplotlibV2")]
    [InlineData("MatplotlibClassic")]
    [InlineData("Default")]
    public void TheLook_SurvivesTheThemesJson(string themeName)
    {
        var theme = themeName.AsTheme();

        Assert.Same(theme.Looks.Wedges, theme.ThroughDto().Looks.Wedges);
    }

    private static List<Point> Points(PathSegment[] segments) => [.. segments.SelectMany(segment => segment switch
    {
        MoveToSegment move => [move.Point],
        LineToSegment line => [line.Point],
        _ => Array.Empty<Point>(),
    })];

    /// <summary>The centre of the pie: the point every wedge of a pie that reaches the centre starts its outline at.</summary>
    private static Point Centre(List<PathSegment[]> wedges) => wedges.Select(Points)
        .Aggregate((common, next) => [.. common.Where(p => next.Any(q => Math.Abs(p.X - q.X) < 1e-6 && Math.Abs(p.Y - q.Y) < 1e-6))])
        .First();
}
