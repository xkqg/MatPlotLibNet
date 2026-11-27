// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Data on a map: points, bubbles, hexagon bins, flows, a non-contiguous cartogram and a Dorling map. Each is a verb
/// that projects the data with the axes' projection and draws it in the map's coordinates, over the series the library already
/// has (a scatter, a hexagon binning, lines) or, for regions resized by a value, over a map series that holds the resized
/// regions. A position the projection cannot place is skipped.</summary>
[Collection("ChartSerializerGlobalState")]
public class GeoMapLayerTests
{
    private static string Map(Action<AxesBuilder> layers, Theme? theme = null) =>
        (theme is null ? Plt.Create() : Plt.Create().WithTheme(theme)).WithSize(720, 360).AddSubPlot(1, 1, 1, ax =>
        {
            ax.WithProjection(GeoProjection.PlateCarree).Ocean(Colors.Blue);
            layers(ax);
        }).ToSvg();

    private static Figure Build(Action<AxesBuilder> layers, IGeoProjection? projection = null) =>
        Plt.Create().WithSize(720, 360).AddSubPlot(1, 1, 1, ax =>
        {
            ax.WithProjection(projection ?? GeoProjection.PlateCarree);
            layers(ax);
        }).Build();

    // ---- points ------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Points_LandOnTheirPixels_TheCentreAtTheCentreOfTheFrame()
    {
        string svg = Map(ax => ax.Points([0.0, 90.0], [0.0, -180.0]));

        var circles = MapSvg.Circles(svg);
        var centre = MapSvg.PixelOf(svg, 0, 0);
        var topLeft = MapSvg.PixelOf(svg, -180, 90);
        Assert.Equal(2, circles.Length);
        Assert.Equal(centre.X, circles[0].X, MapSvg.WrittenPrecision);
        Assert.Equal(centre.Y, circles[0].Y, MapSvg.WrittenPrecision);
        Assert.Equal(topLeft.X, circles[1].X, MapSvg.WrittenPrecision);
        Assert.Equal(topLeft.Y, circles[1].Y, MapSvg.WrittenPrecision);
    }

    [Fact]
    public void APointTheProjectionCannotPlace_IsSkipped()
    {
        // The far side of the globe has no place on an orthographic map centred on (0, 0).
        var figure = Build(ax => ax.Points([0.0, 0.0], [10.0, 180.0]), GeoProjection.Orthographic);

        var points = Assert.IsType<ScatterSeries>(Assert.Single(figure.SubPlots[0].Series));

        Assert.Single(points.XData);
    }

    [Fact]
    public void Points_TakeTheirCoordinatesFromTheProjection_NotTheDegrees()
    {
        var figure = Build(ax => ax.Points([45.0], [10.0]), GeoProjection.Robinson);

        var points = (ScatterSeries)figure.SubPlots[0].Series[0];
        var projected = GeoProjection.Robinson.Forward(45, 10);

        Assert.Equal(projected.X, points.XData[0]);
        Assert.Equal(projected.Y, points.YData[0]);
    }

    [Fact]
    public void Points_AreConfiguredLikeAnyScatter()
    {
        var figure = Build(ax => ax.Points([0.0], [0.0], s => { s.Color = Colors.Red; s.Label = "cities"; }));

        var points = (ScatterSeries)figure.SubPlots[0].Series[0];

        Assert.Equal(Colors.Red, points.Color);
        Assert.Equal("cities", points.Label);
    }

    [Fact]
    public void ALayerWhoseArraysDoNotPair_IsRefused_AndLeavesTheAxesAsItWas()
    {
        var ex = Assert.Throws<ArgumentException>(() => Build(ax => ax.Points([0.0, 1.0], [0.0])));
        Assert.Equal(2.RefusalAgainst(1), ex.Message);

        var figure = Build(ax =>
        {
            try { ax.Points([0.0, 1.0], [0.0]); } catch (ArgumentException) { }
        });
        Assert.Empty(figure.SubPlots[0].Series);
    }

    [Fact]
    public void ALayerOnAxesWithNoProjection_IsRefused() =>
        Assert.Throws<InvalidOperationException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Points([0.0], [0.0])).Build());

    // ---- bubbles -----------------------------------------------------------------------------------------------------------

    [Fact]
    public void ABubbleHasTheAreaOfItsValue_TheLargestValueTheDefaultArea()
    {
        string svg = Map(ax => ax.Bubbles([0.0, 10.0, 20.0], [0.0, 10.0, 20.0], [1.0, 2.0, 4.0]));

        var circles = MapSvg.Circles(svg);
        // a scatter's size s is the marker's area in points squared as matplotlib reads it: sqrt(s) points across, so 20 points
        // across (a radius of 10 points) for 400, drawn at 100 dpi
        double expectedLargest = 10 * (100.0 / 72.0);
        Assert.Equal(3, circles.Length);
        Assert.Equal(expectedLargest, circles[2].R, MapSvg.WrittenPrecision);
        // a quarter of the area is half the radius, half the area the radius over the square root of two
        Assert.Equal(expectedLargest / 2, circles[0].R, MapSvg.WrittenPrecision);
        Assert.Equal(expectedLargest / Math.Sqrt(2), circles[1].R, MapSvg.WrittenPrecision);
    }

    [Fact]
    public void ABubbleOfNoValue_IsLeftOut()
    {
        var figure = Build(ax => ax.Bubbles([0.0, 1.0, 2.0, 3.0], [0.0, 1.0, 2.0, 3.0], [3.0, 0.0, double.NaN, 6.0]));

        var bubbles = (ScatterSeries)figure.SubPlots[0].Series[0];

        Assert.Equal(2, bubbles.XData.Length);
        Assert.Equal(2, bubbles.Sizes!.Length);
    }

    [Fact]
    public void BubblesOfNoValueAtAll_AreAnEmptyScatter()
    {
        var figure = Build(ax => ax.Bubbles([0.0, 1.0], [0.0, 1.0], [0.0, double.NaN]));

        var bubbles = (ScatterSeries)Assert.Single(figure.SubPlots[0].Series);

        Assert.Empty(bubbles.XData);
        Assert.Empty(bubbles.Sizes!);
    }

    [Fact]
    public void TheSizesOfTheBubbles_CanBeRescaledByTheCallback()
    {
        var figure = Build(ax => ax.Bubbles([0.0, 1.0], [0.0, 1.0], [5.0, 10.0], s => s.Sizes = [.. s.Sizes!.Select(area => area / 4)]));

        var bubbles = (ScatterSeries)figure.SubPlots[0].Series[0];

        Assert.Equal([50.0, 100.0], bubbles.Sizes!);
    }

    // ---- hexagon bins ------------------------------------------------------------------------------------------------------

    [Fact]
    public void HexagonBins_AreBinnedInProjectedCoordinates()
    {
        var figure = Build(ax => ax.HexBins([10.0, 20.0, 30.0], [10.0, 20.0, 30.0]), GeoProjection.Mercator);

        var bins = Assert.IsType<HexbinSeries>(Assert.Single(figure.SubPlots[0].Series));

        Assert.Equal(GeoProjection.Mercator.Forward(10, 10).X, bins.X[0]);
        Assert.Equal(GeoProjection.Mercator.Forward(30, 30).Y, bins.Y[2]);
    }

    // ---- flows -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void AFlow_StartsAndEndsAtTheProjectedPixelsOfItsEnds()
    {
        string svg = Map(ax => ax.Flows([new GeoFlow(new GeoCoordinate(52, 5), new GeoCoordinate(-33, 151), 3)]));

        var line = Assert.Single(MapSvg.Polylines(svg), p => p.Points.Length > 10);
        var from = MapSvg.PixelOf(svg, 5, 52);
        var to = MapSvg.PixelOf(svg, 151, -33);
        Assert.Equal(from.X, line.Points[0].X, MapSvg.WrittenPrecision);
        Assert.Equal(from.Y, line.Points[0].Y, MapSvg.WrittenPrecision);
        Assert.Equal(to.X, line.Points[^1].X, MapSvg.WrittenPrecision);
        Assert.Equal(to.Y, line.Points[^1].Y, MapSvg.WrittenPrecision);
    }

    [Fact]
    public void AFlow_FollowsTheGreatCircle_NotTheStraightLineOfTheMap()
    {
        // Amsterdam to Seattle runs far north of the straight line on a PlateCarree map: the great circle reaches about 71 N.
        string svg = Map(ax => ax.Flows([new GeoFlow(new GeoCoordinate(52.4, 4.9), new GeoCoordinate(47.6, -122.3), 1)]));

        var line = Assert.Single(MapSvg.Polylines(svg), p => p.Points.Length > 10);
        double topOfTheLine = line.Points.Min(p => p.Y);
        double straightLine = MapSvg.PixelOf(svg, 0, 50).Y;
        Assert.True(topOfTheLine < straightLine - 15, "the flow bends north of the straight line");
    }

    [Fact]
    public void AFlowAcrossTheAntimeridian_IsSplitInTwo_AndNoSegmentCrossesTheMap()
    {
        string svg = Map(ax => ax.Flows([new GeoFlow(new GeoCoordinate(35.7, 139.7), new GeoCoordinate(34.0, -118.2), 1)]));

        var lines = MapSvg.Polylines(svg).Where(p => p.Points.Length > 5).ToArray();
        var (left, right, _, _) = MapSvg.Frame(svg);
        Assert.Equal(2, lines.Length);
        foreach (var line in lines)
            for (int i = 1; i < line.Points.Length; i++)
                Assert.True(Math.Abs(line.Points[i].X - line.Points[i - 1].X) < (right - left) / 2);
    }

    [Fact]
    public void AFlowWithOneEndTheProjectionCannotPlace_IsSkippedWhole()
    {
        var figure = Build(ax => ax.Flows([new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(0, 180), 1)]), GeoProjection.Orthographic);

        Assert.Empty(figure.SubPlots[0].Series);
    }

    [Fact]
    public void TheWidthOfAFlow_IsInProportionToItsQuantity()
    {
        string svg = Map(ax => ax.Flows(
        [
            new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(10, 40), 1),
            new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(-10, -40), 2),
        ], maxWidth: 6));

        var lines = MapSvg.Polylines(svg).Where(p => p.Points.Length > 5).ToArray();
        Assert.Equal(2, lines.Length);
        Assert.Equal(3.0, lines[0].Element.Number("stroke-width"), MapSvg.WrittenPrecision);
        Assert.Equal(6.0, lines[1].Element.Number("stroke-width"), MapSvg.WrittenPrecision);
    }

    [Fact]
    public void AFlowOfNoQuantity_IsLeftOut()
    {
        var figure = Build(ax => ax.Flows(
        [
            new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(10, 40), 0),
            new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(10, 40), double.NaN),
            new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(10, 40), 3),
        ]));

        Assert.Single(figure.SubPlots[0].Series);
    }

    private static string Stroke(SvgElement line) => line["stroke"]!.ToUpperInvariant();

    private static string[] PointFills(string svg) =>
        [.. svg.Elements("circle").Select(circle => circle["fill"]).OfType<string>().Where(fill => fill.StartsWith('#')).Select(fill => fill.ToUpperInvariant())];

    /// <summary>The flows are lines drawn as <c>ax.plot</c> draws them and the places a scatter, so under the matplotlib themes the
    /// flows of one layer share the lines cycle's next colour, the places that follow take the patch cycle's first, and the next
    /// layer of flows the lines cycle's next. matplotlib 3.11.2, style 'default':
    /// <c>a = ax.plot(...)[0]; ax.plot(..., color=a.get_color()); s = ax.scatter(...); c = ax.plot(...)[0]</c> prints
    /// <c>#1f77b4 #1f77b4 #1f77b4 #ff7f0e</c> (plot and scatter ask two cycles, axes/_base.py <c>_get_lines</c> and
    /// <c>_get_patches_for_fill</c>).</summary>
    [Fact]
    public void TheFlowsOfOneLayer_ShareOneColour_ThePlacesTakeThePatchCycle_TheNextFlowsTheNextLineColour()
    {
        string svg = Map(ax => ax.Flows(
        [
            new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(10, 40), 1),
            new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(-10, -40), 2),
        ]).Points([0.0], [0.0]).Flows([new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(20, 60), 1)]));

        var lines = MapSvg.Polylines(svg).Where(p => p.Points.Length > 5).ToArray();
        Assert.Equal(3, lines.Length);
        Assert.Equal("#1F77B4", Stroke(lines[0].Element));
        Assert.Equal("#1F77B4", Stroke(lines[1].Element));
        Assert.Equal("#FF7F0E", Stroke(lines[2].Element));
        Assert.Equal(["#1F77B4"], PointFills(svg));
    }

    /// <summary>The library's own themes keep one cycle: the flows of one layer share a colour and the places that follow take
    /// the next.</summary>
    [Fact]
    public void TheFlowsOfOneLayer_ShareOneColour_TheNextSeriesTakesTheNext_UnderTheLibrarysOwnTheme()
    {
        string svg = Map(ax => ax.Flows(
        [
            new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(10, 40), 1),
            new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(-10, -40), 2),
        ]).Points([0.0], [0.0]), Theme.Default);

        var lines = MapSvg.Polylines(svg).Where(p => p.Points.Length > 5).ToArray();
        Assert.Equal(2, lines.Length);
        Assert.Equal(Stroke(lines[0].Element), Stroke(lines[1].Element));
        var fills = PointFills(svg);
        Assert.Single(fills);
        Assert.NotEqual(Stroke(lines[0].Element), fills[0]);
    }

    [Fact]
    public void AColourGivenToTheFlows_IsTheColourOfEveryFlow()
    {
        var figure = Build(ax => ax.Flows(
        [
            new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(10, 40), 1),
            new GeoFlow(new GeoCoordinate(0, 0), new GeoCoordinate(-10, -40), 2),
        ], Colors.Red));

        var lines = figure.SubPlots[0].Series.OfType<LineSeries>().ToArray();

        Assert.Equal(2, lines.Length);
        Assert.All(lines, line => Assert.Equal(Colors.Red, line.Color));
    }

    // ---- regions resized by a value ----------------------------------------------------------------------------------------

    /// <summary>The area of a projected polygon: its exterior less its holes (shoelace, in the plane of the map).</summary>
    private static double Area(GeoPolygon polygon)
    {
        static double Ring(IReadOnlyList<GeoPosition> ring)
        {
            double sum = 0;
            for (int i = 0; i < ring.Count; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Count];
                sum += a.Lon * b.Lat - b.Lon * a.Lat;
            }

            return Math.Abs(sum / 2);
        }

        return Ring(polygon.Exterior) - polygon.Holes.Sum(Ring);
    }

    private static double AreaOf(GeoFeature feature) => feature.Geometry.Polygons.Sum(Area);

    /// <summary>The centroid of a region, worked out by a different road than the library's: each ring is cut into the triangles that fan out
    /// of its first corner and the centroid is the mean of their centres weighed by their signed areas, a hole weighing against it.</summary>
    private static (double X, double Y) CentroidOf(GeoFeature feature)
    {
        double area = 0, cx = 0, cy = 0;
        foreach (var polygon in feature.Geometry.Polygons)
        {
            foreach (var (ring, sign) in new[] { (polygon.Exterior, 1.0) }.Concat(polygon.Holes.Select(h => (h, -1.0))))
            {
                double signed = 0, mx = 0, my = 0;
                for (int i = 1; i + 1 < ring.Count; i++)
                {
                    double triangle = ((ring[i].Lon - ring[0].Lon) * (ring[i + 1].Lat - ring[0].Lat) - (ring[i + 1].Lon - ring[0].Lon) * (ring[i].Lat - ring[0].Lat)) / 2;
                    signed += triangle;
                    mx += triangle * (ring[0].Lon + ring[i].Lon + ring[i + 1].Lon) / 3;
                    my += triangle * (ring[0].Lat + ring[i].Lat + ring[i + 1].Lat) / 3;
                }

                double weight = sign * Math.Abs(signed);
                area += weight;
                cx += weight * (mx / signed);
                cy += weight * (my / signed);
            }
        }

        return (cx / area, cy / area);
    }

    private static GeoFeature Country(string code) => NaturalEarth110m.Countries().Single(f => f.Properties["ADM0_A3"] == code);

    [Fact]
    public void ACartogramRegion_HasAnAreaInProportionToItsValue()
    {
        var figure = Build(ax => ax.Cartogram(new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 2, ["ESP"] = 4 }));

        var series = Assert.IsType<GeoPolygonSeries>(Assert.Single(figure.SubPlots[0].Series));
        var areas = series.Features.ToDictionary(f => f.Properties["ADM0_A3"], AreaOf);

        Assert.Equal(3, series.Features.Count);
        Assert.Equal(2.0, areas["DEU"] / areas["FRA"], 6);
        Assert.Equal(4.0, areas["ESP"] / areas["FRA"], 6);
        Assert.True(series.IsRawProjected);
    }

    [Fact]
    public void ACartogramRegion_KeepsItsCentroid()
    {
        var figure = Build(ax => ax.Cartogram(new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 2 }));

        var series = (GeoPolygonSeries)figure.SubPlots[0].Series[0];

        foreach (var resized in series.Features)
        {
            var original = CentroidOf(Country(resized.Properties["ADM0_A3"]));
            var now = CentroidOf(resized);
            Assert.Equal(original.X, now.X, 6);
            Assert.Equal(original.Y, now.Y, 6);
        }
    }

    [Fact]
    public void TheTotalAreaOfACartogram_IsTheTotalAreaOfTheRegionsItDraws()
    {
        var figure = Build(ax => ax.Cartogram(new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 3 }));

        var series = (GeoPolygonSeries)figure.SubPlots[0].Series[0];

        Assert.Equal(AreaOf(Country("FRA")) + AreaOf(Country("DEU")), series.Features.Sum(AreaOf), 6);
    }

    [Fact]
    public void ARegionOfNoValue_IsLeftOutOfACartogram()
    {
        var figure = Build(ax => ax.Cartogram(new Dictionary<string, double> { ["FRA"] = 0, ["DEU"] = -1, ["ESP"] = 4, ["ITA"] = double.NaN }));

        var series = (GeoPolygonSeries)figure.SubPlots[0].Series[0];

        Assert.Equal("ESP", Assert.Single(series.Features).Properties["ADM0_A3"]);
    }

    [Fact]
    public void ACartogramOfNoRegionAtAll_IsAnEmptyMap()
    {
        var figure = Build(ax => ax.Cartogram(new Dictionary<string, double>()));

        var series = Assert.IsType<GeoPolygonSeries>(Assert.Single(figure.SubPlots[0].Series));

        Assert.Empty(series.Features);
    }

    [Fact]
    public void ACartogram_KeepsTheHolesOfItsRegions()
    {
        var figure = Build(ax => ax.Cartogram(new Dictionary<string, double> { ["ZAF"] = 2 }));

        var series = (GeoPolygonSeries)figure.SubPlots[0].Series[0];

        Assert.Single(Assert.Single(Assert.Single(series.Features).Geometry.Polygons).Holes);
    }

    /// <summary>A cartogram, the plain one or the contiguous one, comes back from JSON as the picture that was written.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ACartogram_ComesBackFromJson_AsTheSamePicture(bool contiguous)
    {
        var values = new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 2, ["ZAF"] = 3 };
        var figure = Build(ax =>
        {
            var ocean = ax.Ocean(Colors.Blue);
            if (contiguous) ocean.ContiguousCartogram(values);
            else ocean.Cartogram(values);
        });

        Assert.Equal(figure.ToSvg(), figure.RoundTripped().ToSvg());
    }

    // ---- contiguous cartogram -----------------------------------------------------------------------------------------------

    private static Dictionary<string, double> PopulationOf() =>
        NaturalEarth110m.Countries().Where(c => c.NumberOf("POP_EST") > 0)
            .ToDictionary(c => c.Properties["ADM0_A3"], c => c.NumberOf("POP_EST"));

    private static GeoPolygonSeries ContiguousOf(IReadOnlyDictionary<string, double> valueOf, IGeoProjection? projection = null) =>
        (GeoPolygonSeries)Build(ax => ax.ContiguousCartogram(valueOf), projection).SubPlots[0].Series.Single();

    [Fact]
    public void AContiguousCartogram_DrawsEveryCountry_InTheCoordinatesOfTheMap()
    {
        var series = ContiguousOf(new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 2 });

        Assert.Equal(NaturalEarth110m.Countries().Count, series.Features.Count);
        Assert.True(series.IsRawProjected);
        Assert.Equal("Contiguous cartogram", series.Label);
    }

    /// <summary>The world's countries resized by their population, made once for the tests that read it.</summary>
    private static readonly Lazy<GeoPolygonSeries> PopulationMap = new(() => ContiguousOf(PopulationOf()));

    [Fact]
    public void AContiguousCartogramCountry_HasAnAreaInProportionToItsValue()
    {
        var population = PopulationOf();

        // The countries with at least 0.5 % of the world's population, which no outline rule raises: each ends within 1 % of the
        // area its population asks for at the scale of the map they make together. Measured: 147 of the 149 countries the rule
        // leaves alone are within 1 %; Mongolia (6.8 %) and Kosovo (5.8 %) cover a few cells of the lattice.
        double world = population.Values.Sum();
        var large = PopulationMap.Value.Features.Where(f => population.GetValueOrDefault(f.Properties["ADM0_A3"]) >= 0.005 * world).ToList();
        double perPerson = large.Sum(AreaOf) / large.Sum(f => population[f.Properties["ADM0_A3"]]);
        var errors = large.Select(f => (Code: f.Properties["ADM0_A3"], Error: Math.Abs(AreaOf(f) / (perPerson * population[f.Properties["ADM0_A3"]]) - 1)))
            .OrderByDescending(e => e.Error).ToList();

        Assert.True(large.Count >= 30, $"{large.Count} countries");
        Assert.True(errors[0].Error <= 0.01, string.Join(", ", errors.Take(5).Select(e => $"{e.Code} {e.Error:P2}")));
    }

    [Fact]
    public void TheCountriesOfAContiguousCartogram_StillMeetAtTheirBorders()
    {
        var series = PopulationMap.Value;

        var france = series.Features.Single(f => f.Properties["ADM0_A3"] == "FRA").Geometry.PositionRings.SelectMany(r => r).ToHashSet();
        var spain = series.Features.Single(f => f.Properties["ADM0_A3"] == "ESP").Geometry.PositionRings.SelectMany(r => r).ToHashSet();

        Assert.True(france.Count(spain.Contains) >= 2, "France and Spain share no border");
    }

    [Fact]
    public void ACountryOfNoValue_KeepsItsAreaInAContiguousCartogram()
    {
        var series = ContiguousOf(new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 4, ["ESP"] = 2 });

        var brazil = series.Features.Single(f => f.Properties["ADM0_A3"] == "BRA");
        Assert.Equal(1.0, AreaOf(brazil) / AreaOf(Country("BRA")), 0.01);
        var area = series.Features.Where(f => f.Properties["ADM0_A3"] is "FRA" or "DEU")
            .ToDictionary(f => f.Properties["ADM0_A3"], AreaOf);
        Assert.Equal(4.0, area["DEU"] / area["FRA"], 0.04);
    }

    [Fact]
    public void AContiguousCartogramOfNoValueAtAll_IsTheMapAsItIs()
    {
        var series = ContiguousOf(new Dictionary<string, double>());

        foreach (var country in series.Features)
            Assert.Equal(AreaOf(Country(country.Properties["ADM0_A3"])), AreaOf(country), 9);
    }

    [Fact]
    public void AContiguousCartogram_BeforeTheAxesHasAProjection_IsRefused()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ContiguousCartogram(new Dictionary<string, double> { ["FRA"] = 1 })).Build());

        Assert.Contains("WithProjection", ex.Message);
    }

    // ---- Dorling -----------------------------------------------------------------------------------------------------------

    [Fact]
    public void ADorlingCircle_StandsAtTheCentroidOfItsRegion_WithTheAreaOfItsValue()
    {
        string svg = Map(ax => ax.Dorling(new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 4 }));

        var circles = MapSvg.Circles(svg);
        var france = CentroidOf(Country("FRA"));
        var germany = CentroidOf(Country("DEU"));
        var onFrance = MapSvg.PixelOf(svg, france.X, france.Y);
        var onGermany = MapSvg.PixelOf(svg, germany.X, germany.Y);
        Assert.Equal(2, circles.Length);
        Assert.Contains(circles, c => Math.Abs(c.X - onFrance.X) <= MapSvg.WrittenPrecision && Math.Abs(c.Y - onFrance.Y) <= MapSvg.WrittenPrecision);
        Assert.Contains(circles, c => Math.Abs(c.X - onGermany.X) <= MapSvg.WrittenPrecision && Math.Abs(c.Y - onGermany.Y) <= MapSvg.WrittenPrecision);
        // four times the area is twice the radius
        Assert.Equal(circles.Max(c => c.R) / 2, circles.Min(c => c.R), MapSvg.WrittenPrecision);
    }

    // ---- a legend that looks for room does not redraw the layer -----------------------------------------------------------

    public static TheoryData<string> Layers => ["points", "bubbles", "hexbins", "flows", "cartogram", "dorling"];

    private static void AddLayer(AxesBuilder ax, string layer)
    {
        switch (layer)
        {
            case "points": ax.Points([10.0, 20.0, 30.0], [10.0, 20.0, 30.0], s => s.Label = "points"); break;
            case "bubbles": ax.Bubbles([10.0, 20.0, 30.0], [10.0, 20.0, 30.0], [1.0, 2.0, 3.0], s => s.Label = "bubbles"); break;
            case "hexbins": ax.HexBins([10.0, 20.0, 30.0, 12.0], [10.0, 20.0, 30.0, 11.0], s => s.Label = "bins"); break;
            case "flows": ax.Flows([new GeoFlow(new GeoCoordinate(52, 5), new GeoCoordinate(-33, 151), 3)], configure: s => s.Label = "flow"); break;
            case "cartogram": ax.Cartogram(new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 2 }, s => s.Label = "cartogram"); break;
            default: ax.Dorling(new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 2 }, s => s.Label = "dorling"); break;
        }
    }

    private static int Marks(string svg) =>
        svg.AllElements().Count(element => element.Name is "circle" or "polygon" or "polyline" or "path");

    [Theory]
    [MemberData(nameof(Layers))]
    public void ALegendThatLooksForRoom_DrawsTheLayerOnce(string layer)
    {
        string Svg(LegendPosition position) =>
            Plt.Create().WithSize(720, 360).AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithProjection(GeoProjection.PlateCarree).Ocean(Colors.Blue);
                AddLayer(ax, layer);
                ax.WithLegend(position);
            }).ToSvg();

        Assert.Equal(Marks(Svg(LegendPosition.UpperLeft)), Marks(Svg(LegendPosition.Best)));
    }
}
