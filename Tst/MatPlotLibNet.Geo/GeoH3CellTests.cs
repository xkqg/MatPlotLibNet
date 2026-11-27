// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The cells of Uber's H3 index on a map: <c>H3Cells</c> colours the cells it is given by their index, <c>H3Bins</c> counts
/// places into the cells of one resolution. Each cell is a region of a <see cref="GeoPolygonSeries"/> with its index in the property
/// <c>h3</c>, cut at the antimeridian and closed along a pole it goes round. The numbers are those of the H3 C library (h3-py 4.5.0):
/// <c>latlng_to_cell(37.7752702151959, -122.418307270836, 9)</c> is <c>8928308280fffff</c>, the example of the H3 documentation.</summary>
[Collection("ChartSerializerGlobalState")]
public class GeoH3CellTests
{
    private const string SanFrancisco9 = "8928308280fffff";
    private const double SanFranciscoLat = 37.7752702151959, SanFranciscoLon = -122.418307270836;

    private static Figure Map(Action<AxesBuilder> layers, IGeoProjection? projection = null) =>
        Plt.Create().WithSize(720, 360).AddSubPlot(1, 1, 1, ax =>
        {
            ax.WithProjection(projection ?? GeoProjection.PlateCarree);
            layers(ax);
        }).Build();

    private static GeoPolygonSeries Cells(Figure figure) => Assert.IsType<GeoPolygonSeries>(Assert.Single(figure.SubPlots[0].Series));

    // ---- a cell by its index ----------------------------------------------------------------------------------------------------

    [Fact]
    public void ACellIsItsBoundary_AsTheH3LibraryDrawsIt()
    {
        // h3.cell_to_boundary('8928308280fffff'), (lat, lng):
        (double Lat, double Lon)[] boundary =
        [
            (37.775197782893386, -122.41719971841658), (37.77688044840227, -122.41612835779269), (37.77838500493093, -122.41738797617619),
            (37.77820687262238, -122.41971895414808), (37.776524206993216, -122.42079024541879), (37.77501967379262, -122.41953062807342),
        ];

        var cell = Assert.Single(Cells(Map(ax => ax.H3Cells(new Dictionary<string, double> { [SanFrancisco9] = 1 }))).Features);

        Assert.Equal("Polygon", cell.Geometry.Type);
        Assert.Equal(SanFrancisco9, cell.Properties["h3"]);
        var ring = Assert.Single(cell.Geometry.PositionRings);
        Assert.Equal(7, ring.Count);
        Assert.Equal(ring[0], ring[^1]);
        foreach (var (lat, lon) in boundary)
            Assert.Contains(ring, p => Math.Abs(p.Lat - lat) < 1e-9 && Math.Abs(p.Lon - lon) < 1e-9);
    }

    [Fact]
    public void EachCellHasItsValue_InTheOrderGiven()
    {
        var series = Cells(Map(ax => ax.H3Cells(new Dictionary<string, double> { ["85196953fffffff"] = 4, ["85283083fffffff"] = 2 })));

        Assert.Equal(["85196953fffffff", "85283083fffffff"], series.Features.Select(f => f.Properties["h3"]));
        Assert.Equal([4.0, 2.0], series.Values!);
        Assert.True(series.ProvidesColorBar);
    }

    [Fact]
    public void AnIndexInCapitals_IsTheSameCell_WrittenAsH3WritesIt()
    {
        var series = Cells(Map(ax => ax.H3Cells(new Dictionary<string, double> { ["8928308280FFFFF"] = 1 })));

        Assert.Equal(SanFrancisco9, Assert.Single(series.Features).Properties["h3"]);
    }

    [Fact]
    public void TheColourMapAndTheCallbackSetTheSeries()
    {
        var series = Cells(Map(ax => ax.H3Cells(new Dictionary<string, double> { [SanFrancisco9] = 1 }, MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma,
            s => s.Label = "trips")));

        Assert.Same(MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma, series.ColorMap);
        Assert.Equal("trips", series.Label);
        Assert.Null(series.StrokeColor);
        Assert.Equal(0.3, series.StrokeWidth);
    }

    [Theory]
    [InlineData("")]
    [InlineData("zz")]
    [InlineData("0")]
    [InlineData("8928308280fffff0")]
    public void AnIndexThatIsNoCell_IsRefused_AndTheAxesIsLeftAsItWas(string index)
    {
        var figure = Map(ax => Assert.Throws<ArgumentException>(() =>
            ax.H3Cells(new Dictionary<string, double> { [SanFrancisco9] = 1, [index] = 2 })));

        Assert.Empty(figure.SubPlots[0].Series);
    }

    [Fact]
    public void NoProjection_IsRefused()
    {
        Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Throws<InvalidOperationException>(() => ax.H3Cells(new Dictionary<string, double> { [SanFrancisco9] = 1 }));
            Assert.Throws<InvalidOperationException>(() => ax.H3Bins([0.0], [0.0], 3));
        });
    }

    // ---- places counted into cells ------------------------------------------------------------------------------------------------

    [Fact]
    public void APlace_FallsInTheCellTheH3LibraryNames()
    {
        var series = Cells(Map(ax => ax.H3Bins([SanFranciscoLat], [SanFranciscoLon], 9)));

        Assert.Equal(SanFrancisco9, Assert.Single(series.Features).Properties["h3"]);
        Assert.Equal([1.0], series.Values!);
    }

    [Fact]
    public void ThePlacesOfACell_AreCounted_InTheOrderTheCellsAreFirstMet()
    {
        // h3.latlng_to_cell(52.37, 4.9, 5) is 85196953fffffff; the San Francisco point at 5 is 85283083fffffff.
        double[] lat = [SanFranciscoLat, 52.37, SanFranciscoLat + 0.001, 52.371, SanFranciscoLat - 0.001, double.NaN, 10];
        double[] lon = [SanFranciscoLon, 4.9, SanFranciscoLon, 4.9, SanFranciscoLon + 0.001, 4.9, double.PositiveInfinity];

        var series = Cells(Map(ax => ax.H3Bins(lat, lon, 5)));

        Assert.Equal(["85283083fffffff", "85196953fffffff"], series.Features.Select(f => f.Properties["h3"]));
        Assert.Equal([3.0, 2.0], series.Values!);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    public void AResolutionH3DoesNotHave_IsRefused_AndTheAxesIsLeftAsItWas(int resolution)
    {
        var figure = Map(ax => Assert.Throws<ArgumentOutOfRangeException>(() => ax.H3Bins([0.0], [0.0], resolution)));

        Assert.Empty(figure.SubPlots[0].Series);
    }

    [Fact]
    public void PlacesAndLongitudesThatDoNotPair_AreRefused_AndTheAxesIsLeftAsItWas()
    {
        var figure = Map(ax => Assert.Throws<ArgumentException>(() => ax.H3Bins([0.0, 1.0], [0.0], 3)));

        Assert.Empty(figure.SubPlots[0].Series);
    }

    [Fact]
    public void NoPlaceAtAll_IsAMapOfNoCells()
    {
        var series = Cells(Map(ax => ax.H3Bins([double.NaN], [0.0], 3)));

        Assert.Empty(series.Features);
        Assert.Empty(series.Values!);
    }

    // ---- a cell across the antimeridian, a cell round a pole -----------------------------------------------------------------------

    [Fact]
    public void ACellAcrossTheAntimeridian_IsTwoPolygons_EachReachingTheMeridian()
    {
        // h3.latlng_to_cell(0, 180, 2) is 827eb7fffffffff: three vertices at 179.x, three at -178.x.
        var cell = Assert.Single(Cells(Map(ax => ax.H3Cells(new Dictionary<string, double> { ["827eb7fffffffff"] = 1 }))).Features);

        Assert.Equal("MultiPolygon", cell.Geometry.Type);
        Assert.Equal(2, cell.Geometry.Polygons.Count);
        Assert.Contains(cell.Geometry.Polygons, p => p.Exterior.Any(q => q.Lon == -180) && p.Exterior.All(q => q.Lon < 0));
        Assert.Contains(cell.Geometry.Polygons, p => p.Exterior.Any(q => q.Lon == 180) && p.Exterior.All(q => q.Lon > 0));
    }

    [Theory]
    [InlineData(90.0, "81033ffffffffff")]
    [InlineData(-90.0, "81f2bffffffffff")]
    public void ACellRoundAPole_IsOnePolygon_ClosedAlongThePole(double pole, string index)
    {
        var cell = Assert.Single(Cells(Map(ax => ax.H3Bins([pole], [0.0], 1))).Features);

        Assert.Equal(index, cell.Properties["h3"]);
        Assert.Equal("Polygon", cell.Geometry.Type);
        var ring = Assert.Single(cell.Geometry.PositionRings);
        Assert.Contains(new GeoPosition(180, pole), ring);
        Assert.Contains(new GeoPosition(-180, pole), ring);
    }

    // ---- drawn where the cell is, and back from JSON --------------------------------------------------------------------------------

    [Fact]
    public void ACellIsDrawnRoundThePixelOfItsCentre()
    {
        // 82196ffffffffff, Amsterdam's cell at resolution 2 (h3.cell_to_boundary): its six vertices average 52.9266 north,
        // 4.4386 east, and it is about 4.7 degrees across, some 9 pixels on this map.
        string svg = Plt.Create().WithSize(720, 360).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree)
            .Ocean(Colors.Blue)
            .H3Cells(new Dictionary<string, double> { ["82196ffffffffff"] = 1 })).ToSvg();

        var paths = svg.Elements("path").Where(path => path["d"] is not null && path["fill"]?.StartsWith('#') == true).Select(path => path.PathPoints()).ToArray();
        // The ocean is the first path and the legend frame the last; the cell stands between them. Its ring is closed, as
        // GeoJSON writes one: six vertices and the first again.
        var cell = paths[1];
        var (cx, cy) = MapSvg.PixelOf(svg, 4.438649114910269, 52.92664648821562);

        Assert.Equal(7, cell.Length);
        Assert.Equal(cell[0], cell[^1]);
        Assert.InRange(cell.Take(6).Average(p => p.X), cx - 0.2, cx + 0.2);
        Assert.InRange(cell.Take(6).Average(p => p.Y), cy - 0.2, cy + 0.2);
    }

    [Theory]
    [InlineData("cells")]
    [InlineData("bins")]
    public void AMapOfCells_ComesBackFromJson_AsTheSamePicture_WithItsIndexes(string verb)
    {
        var figure = Map(ax =>
        {
            ax.Ocean(Colors.Blue);
            if (verb == "cells")
                ax.H3Cells(new Dictionary<string, double> { [SanFrancisco9] = 1, ["827eb7fffffffff"] = 3, ["81033ffffffffff"] = 2 });
            else
                ax.H3Bins([SanFranciscoLat, 0, 90, 0], [SanFranciscoLon, 180, 0, 180], 2);
        }, GeoProjection.Robinson);

        var back = figure.RoundTripped();

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Equal(
            Cells2(figure).Features.Select(f => f.Properties["h3"]),
            Cells2(back).Features.Select(f => f.Properties["h3"]));
    }

    private static GeoPolygonSeries Cells2(Figure figure) => figure.SubPlots[0].Series.OfType<GeoPolygonSeries>().Last();

    [Fact]
    public void TheResolutionsAreH3s_AndTheIndexIsWrittenUnderH3()
    {
        Assert.Equal(0, GeoAxesExtensions.H3CoarsestResolution);
        Assert.Equal(15, GeoAxesExtensions.H3FinestResolution);
        Assert.Equal("h3", GeoAxesExtensions.H3IndexProperty);
    }
}
