// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The outline of the markers of a line where its author names none. matplotlib 3.11.2, printed under each style:
/// <c>rcParams['lines.markeredgewidth']</c> is 1.0 points in the default style and 0.5 in the classic one, and
/// <c>Line2D.get_markeredgecolor()</c> for the default <c>'auto'</c> is the line's colour, except in classic mode
/// (<c>_internal.classic_mode</c>: "Bad hard-wired default"), where a filled marker (everything but <c>+</c> and <c>x</c>) gets <c>'k'</c>.
/// An unfilled marker (<c>+</c>, <c>x</c>) is drawn by its edge: in the edge's colour, markeredgewidth wide. The library drew a marker with
/// no outline unless its author named an edge colour, and a <c>+</c> an eighth of its size wide. A width and a colour that are named are
/// drawn as named, 1 included, and the legend's handle wears the plot's marker.</summary>
public class LineMarkerEdgeTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [2.0, 3.0, 2.5];

    private static readonly double OnePoint = 1.0.PointsInPixels;
    private static readonly double HalfPoint = 0.5.PointsInPixels;

    private static XDocument Doc(Theme theme, Action<LineSeries> configure, bool legend = false) =>
        Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot(X, Y, s => { s.Label = "m"; configure(s); });
            if (legend) ax.WithLegend();
        }).ToSvg().Xml();

    private static bool InLegend(XElement e) => e.Ancestors().Any(a => (string?)a.Attribute("class") == "legend");

    private static string Blue(Theme theme) => theme.CycleColors[0].ToHex();

    /// <summary>The markers of the plot or of the legend: the circles, which the line itself does not write.</summary>
    private static List<XElement> Circles(XDocument doc, bool inLegend) =>
        [.. doc.Each("circle").Where(e => InLegend(e) == inLegend)];

    /// <summary>The strokes of a plus marker: a horizontal and a vertical line through one point, in the colour given.</summary>
    private static List<XElement> Strokes(XDocument doc, string colour, bool inLegend) =>
        [.. doc.Each("line").Where(e => (string?)e.Attribute("stroke") == colour && InLegend(e) == inLegend
            && ((string?)e.Attribute("x1") == (string?)e.Attribute("x2") || (string?)e.Attribute("y1") == (string?)e.Attribute("y2")))];

    private static double Width(XElement e) => double.Parse((string?)e.Attribute("stroke-width") ?? "0", CultureInfo.InvariantCulture);

    // ── filled markers ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UnderV2_AFilledMarkerIsOutlinedInTheLinesColour_OnePointWide()
    {
        var circles = Circles(Doc(Theme.MatplotlibV2, s => s.Marker = MarkerStyle.Circle), inLegend: false);

        Assert.Equal(3, circles.Count);
        Assert.All(circles, c =>
        {
            Assert.Equal(Blue(Theme.MatplotlibV2), (string?)c.Attribute("fill"));
            Assert.Equal(Blue(Theme.MatplotlibV2), (string?)c.Attribute("stroke"));
            Assert.Equal(Math.Round(OnePoint, 2), Width(c), 2);
        });
    }

    [Fact]
    public void UnderClassic_AFilledMarkerIsOutlinedInBlack_HalfAPointWide_WhateverTheLinesColour()
    {
        var circles = Circles(Doc(Theme.MatplotlibClassic, s => s.Marker = MarkerStyle.Circle), inLegend: false);

        Assert.All(circles, c =>
        {
            Assert.Equal(Blue(Theme.MatplotlibClassic), (string?)c.Attribute("fill"));
            Assert.Equal("#000000", (string?)c.Attribute("stroke"));
            Assert.Equal(Math.Round(HalfPoint, 2), Width(c), 2);
        });
    }

    [Fact]
    public void UnderTheLibrarysThemes_AFilledMarkerHasNoOutline_AsBefore()
    {
        var circles = Circles(Doc(Theme.Default, s => s.Marker = MarkerStyle.Circle), inLegend: false);

        Assert.Equal(3, circles.Count);
        Assert.All(circles, c => Assert.Null(c.Attribute("stroke")));
    }

    // ── unfilled markers: drawn by their edge ─────────────────────────────────────────────────────────────────

    [Fact]
    public void APlus_IsDrawnInTheLinesColour_AsWideAsTheMarkerEdgeWidthOfItsTheme()
    {
        var v2 = Strokes(Doc(Theme.MatplotlibV2, s => s.Marker = MarkerStyle.Plus), Blue(Theme.MatplotlibV2), inLegend: false);
        var classic = Strokes(Doc(Theme.MatplotlibClassic, s => s.Marker = MarkerStyle.Plus), Blue(Theme.MatplotlibClassic), inLegend: false);

        // three markers of two strokes each, plus the line's own segments (which are neither horizontal nor vertical here)
        Assert.Equal(6, v2.Count);
        Assert.All(v2, line => Assert.Equal(Math.Round(OnePoint, 2), Width(line), 2));
        Assert.Equal(6, classic.Count);
        Assert.All(classic, line => Assert.Equal(Math.Round(HalfPoint, 2), Width(line), 2));
    }

    // ── what is named is drawn as named ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void AGivenEdgeWidth_IsDrawnAsGiven_EvenWhenItIsTheOneANewLineHolds()
    {
        var oneUnderClassic = Circles(Doc(Theme.MatplotlibClassic, s => { s.Marker = MarkerStyle.Circle; s.MarkerEdgeWidth = 1.0; }), inLegend: false);
        var threeUnderV2 = Circles(Doc(Theme.MatplotlibV2, s => { s.Marker = MarkerStyle.Circle; s.MarkerEdgeWidth = 3.0; }), inLegend: false);

        Assert.All(oneUnderClassic, c => Assert.Equal(1.0, Width(c), 2));
        Assert.All(oneUnderClassic, c => Assert.Equal("#000000", (string?)c.Attribute("stroke")));
        Assert.All(threeUnderV2, c => Assert.Equal(3.0, Width(c), 2));
        Assert.All(threeUnderV2, c => Assert.Equal(Blue(Theme.MatplotlibV2), (string?)c.Attribute("stroke")));
    }

    [Fact]
    public void AGivenEdgeWidthAlone_StaysWithoutAnOutlineUnderTheLibrarysThemes_AsItsDocumentationSays()
    {
        var circles = Circles(Doc(Theme.Default, s => { s.Marker = MarkerStyle.Circle; s.MarkerEdgeWidth = 3.0; }), inLegend: false);

        Assert.All(circles, c => Assert.Null(c.Attribute("stroke")));
    }

    [Fact]
    public void AGivenEdgeColour_WinsOverTheThemesAndTakesTheThemesWidth()
    {
        var v2 = Circles(Doc(Theme.MatplotlibV2, s => { s.Marker = MarkerStyle.Circle; s.MarkerEdgeColor = Colors.Red; }), inLegend: false);
        var library = Circles(Doc(Theme.Default, s => { s.Marker = MarkerStyle.Circle; s.MarkerEdgeColor = Colors.Red; }), inLegend: false);

        Assert.All(v2, c => Assert.Equal(Colors.Red.ToHex(), (string?)c.Attribute("stroke")));
        Assert.All(v2, c => Assert.Equal(Math.Round(OnePoint, 2), Width(c), 2));
        Assert.All(library, c => Assert.Equal(1.0, Width(c), 2));
    }

    [Fact]
    public void AnEdgeWidth_SurvivesTheJson_AndOneThatIsNot_StaysUnnamed()
    {
        string unnamed = Plt.Create().WithTheme(Theme.MatplotlibClassic).AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Marker = MarkerStyle.Circle)).Build().ToJson();
        string named = Plt.Create().WithTheme(Theme.MatplotlibClassic).AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => { s.Marker = MarkerStyle.Circle; s.MarkerEdgeWidth = 1.0; })).Build().ToJson();

        Assert.DoesNotContain("markerEdgeWidth", unnamed);
        Assert.Contains("\"markerEdgeWidth\":1", named);
        Assert.All(Circles(ChartSerializerDefault.Current.FromJson(unnamed).ToSvg().Xml(), false), c => Assert.Equal(Math.Round(HalfPoint, 2), Width(c), 2));
        Assert.All(Circles(ChartSerializerDefault.Current.FromJson(named).ToSvg().Xml(), false), c => Assert.Equal(1.0, Width(c), 2));
    }

    // ── the other series that marks its points, and the legend's handle ───────────────────────────────────────

    [Fact]
    public void AStepsMarkers_WearTheThemesOutlineToo()
    {
        var svg = Plt.Create().WithTheme(Theme.MatplotlibClassic).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Step(X, Y, s => s.Marker = MarkerStyle.Circle)).ToSvg().Xml();

        var circles = Circles(svg, inLegend: false);
        Assert.Equal(3, circles.Count);
        Assert.All(circles, c => Assert.Equal("#000000", (string?)c.Attribute("stroke")));
    }

    [Fact]
    public void TheLegendsHandle_WearsTheThemesOutlineToo()
    {
        var v2 = Circles(Doc(Theme.MatplotlibV2, s => s.Marker = MarkerStyle.Circle, legend: true), inLegend: true);
        var classic = Circles(Doc(Theme.MatplotlibClassic, s => s.Marker = MarkerStyle.Circle, legend: true), inLegend: true);
        var library = Circles(Doc(Theme.Default, s => s.Marker = MarkerStyle.Circle, legend: true), inLegend: true);

        Assert.Equal(Blue(Theme.MatplotlibV2), (string?)v2.Single().Attribute("stroke"));
        Assert.Equal(2, classic.Count);   // classic shows two markers in a line's handle (legend.numpoints 2)
        Assert.All(classic, marker => Assert.Equal("#000000", (string?)marker.Attribute("stroke")));
        Assert.Null(library.Single().Attribute("stroke"));
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("library")]
    public void TheLegendsHandle_WearsTheFaceAndTheOutlineTheAuthorNamed_AsThePlotDoes(string theme)
    {
        var chosen = theme.AsTheme();

        var doc = Doc(chosen, s =>
        {
            s.Marker = MarkerStyle.Circle;
            s.MarkerFaceColor = Colors.Red;
            s.MarkerEdgeColor = Colors.Green;
            s.MarkerEdgeWidth = 2.0;
        }, legend: true);

        foreach (bool inLegend in new[] { false, true })
        {
            var marker = Circles(doc, inLegend).First();
            Assert.Equal(Colors.Red.ToHex(), (string?)marker.Attribute("fill"));
            Assert.Equal(Colors.Green.ToHex(), (string?)marker.Attribute("stroke"));
            Assert.Equal(2.0, Width(marker), 2);
        }
    }

    [Fact]
    public void TheSetting_IsTheMatplotlibThemes_AndSurvivesTheThemeJson()
    {
        Assert.Equal(OnePoint, Theme.MatplotlibV2.Looks.Markers.LineEdge!.Value.EdgeWidth, 9);
        Assert.Null(Theme.MatplotlibV2.Looks.Markers.LineEdge!.Value.FilledEdge);
        Assert.Equal(HalfPoint, Theme.MatplotlibClassic.Looks.Markers.LineEdge!.Value.EdgeWidth, 9);
        Assert.Equal(Colors.Black, Theme.MatplotlibClassic.Looks.Markers.LineEdge!.Value.FilledEdge);
        Assert.Null(Theme.Default.Looks.Markers.LineEdge);
        Assert.Equal(Colors.Black, Theme.MatplotlibClassic.ThroughDto().Looks.Markers.LineEdge!.Value.FilledEdge);
        Assert.Null(Theme.Default.ThroughDto().Looks.Markers.LineEdge);
        Assert.Equal(OnePoint, Theme.MatplotlibV2.Cloned().Looks.Markers.LineEdge!.Value.EdgeWidth, 9);
    }
}
