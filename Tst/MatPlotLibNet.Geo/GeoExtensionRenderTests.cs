// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that the geo extension methods (Coastlines, Borders, Land, Ocean) add their series to the axes: they once built a
/// series and never attached it, so the picture was blank.</summary>
public class GeoExtensionRenderTests
{
    public static TheoryData<string, Func<AxesBuilder, IGeoProjection, AxesBuilder>> Verbs => new()
    {
        { "coastlines", (ax, projection) => ax.Coastlines(projection) },
        { "borders", (ax, projection) => ax.Borders(projection) },
        { "land", (ax, projection) => ax.Land(projection, Colors.Green) },
        { "ocean", (ax, projection) => ax.Ocean(projection, Colors.Blue) },
    };

    [Theory]
    [MemberData(nameof(Verbs))]
    public void AGeoVerb_AddsItsSeriesToTheAxes(string verb, Func<AxesBuilder, IGeoProjection, AxesBuilder> add)
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => add(ax.WithProjection(GeoProjection.Robinson), GeoProjection.Robinson))
            .Build();

        Assert.True(fig.SubPlots[0].Series.Count == 1, $"{verb} added {fig.SubPlots[0].Series.Count} series");
    }

    /// <summary>The picture of one <paramref name="series"/> on a Plate Carree map, 400 by 300.</summary>
    private static string Drawn(GeoPolygonSeries series) => Plt.Create()
        .WithSize(400, 300)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.WithProjection(GeoProjection.PlateCarree);
            ax.AddSeries(series);
        })
        .ToSvg();

    /// <summary>A feature of one <paramref name="type"/> and one ring.</summary>
    private static GeoFeature Feature(string type, params GeoPosition[] ring) =>
        new(new GeoGeometry { Type = type, PositionRings = [ring] }, new Dictionary<string, string>());

    [Fact]
    public void FullStack_AddsAllFourSeries()
    {
        var proj = GeoProjection.Robinson;
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(proj)
                .Ocean(proj, Colors.Blue)
                .Land(proj, Colors.Green)
                .Coastlines(proj, Colors.Black)
                .Borders(proj, Colors.Gray))
            .Build();

        Assert.Equal(4, fig.SubPlots[0].Series.Count);
    }

    [Fact]
    public void Coastlines_SvgContainsRenderedGeometry()
    {
        // Coastlines render as line segments — SVG must contain at least one <polyline>
        // (the GeoPolygonSeries draws LineString features via DrawLines → polyline).
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.Robinson)
                .Coastlines(GeoProjection.Robinson, Colors.Black))
            .ToSvg();

        // Must contain polyline OR path (depending on backend); not just chrome.
        // The blank-SVG bug rendered nothing past the title/background rect.
        Assert.True(
            svg.Contains("<polyline") || svg.Contains("<path"),
            "SVG should contain rendered geometry from coastlines");
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  Land renders countries as filled paths (a polygon with a hole is one path) — must contain at least one <path>.
        new DrawnToken("Land_SvgContainsRenderedPolygons", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.Robinson)
                .Land(GeoProjection.Robinson, Colors.Green))
            .ToSvg(), "<path"),
        //  Ocean with IsRawProjected = true uses raw projected coordinates instead of calling Projection.Forward. SVG must
        // contain a filled polygon.
        new DrawnToken("Ocean_SvgContainsFilledPolygon_RawProjectedArm", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.PlateCarree)
                .Ocean(GeoProjection.PlateCarree, Colors.Blue))
            .ToSvg(), "<path"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A ring too short to draw is skipped and the picture is still drawn: a polygon of two points, a line of one, draws what a
    /// series of no feature draws.</summary>
    [Theory]
    [InlineData("Polygon", 2)]
    [InlineData("LineString", 1)]
    public void ARingTooShortToDraw_IsSkipped(string type, int points)
    {
        var ring = Enumerable.Range(0, points).Select(i => new GeoPosition(10 * i, 20 * i)).ToArray();
        GeoPolygonSeries Of(List<GeoFeature> features) => new(features, GeoProjection.PlateCarree) { Color = Colors.Green, StrokeColor = Colors.Black };

        Assert.Equal(Drawn(Of([])), Drawn(Of([Feature(type, ring)])));
    }

    public static TheoryData<string, GeoPosition[], Color?, Color?> Lines => new()
    {
        // A point that does not project is skipped; the two that do still make a line.
        { "a point that does not project", [new(double.NaN, 0), new(10, 20), new(20, 30)], null, Colors.Black },
        // StrokeColor ?? Color ?? Colors.Black: the series colour when no stroke colour is given, black when neither is.
        { "the series colour", [new(10, 20), new(20, 30), new(30, 40)], Colors.Red, null },
        { "black", [new(10, 20), new(20, 30)], null, null },
    };

    /// <summary>A line feature is drawn as a line whichever colour it falls back to and when one of its points does not project.</summary>
    [Theory]
    [MemberData(nameof(Lines))]
    public void ALineFeature_IsDrawnAsALine(string line, GeoPosition[] ring, Color? color, Color? stroke)
    {
        string svg = Drawn(new GeoPolygonSeries([Feature("LineString", ring)], GeoProjection.PlateCarree) { Color = color, StrokeColor = stroke });

        Assert.True(svg.Contains("<polyline") || svg.Contains("<path") || svg.Contains("<line"), $"no line drawn for {line}");
    }

    /// <summary>A Best legend looks at the marks the chart drew without drawing it again: a map draws itself, and drawing it
    /// into the picture while the legend finds its place would write every polygon of it twice.</summary>
    [Fact]
    public void ABestLegend_OverAMap_DoesNotDrawTheMapASecondTime()
    {
        int Polygons(bool legend)
        {
            string svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
                .WithProjection(GeoProjection.Robinson)
                .Ocean(GeoProjection.Robinson, Colors.Blue)
                .Land(GeoProjection.Robinson, Colors.Green)
                .Plot([0.0, 1.0], [0.0, 1.0], line => line.Label = "route")
                .WithLegend(legend ? MatPlotLibNet.Models.LegendPosition.Best : MatPlotLibNet.Models.LegendPosition.UpperRight)).ToSvg();
            return svg.Elements("polygon").Count;
        }

        Assert.Equal(Polygons(legend: false), Polygons(legend: true));
    }
}
