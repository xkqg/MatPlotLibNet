// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The land of a globe is cut at its horizon as cartopy cuts it. A country that crosses the horizon used to be drawn from
/// the vertices on the near side only, closed by a straight edge from the last to the first (Russia and the whole of Asia bridged by
/// one line from the top of the globe to its right edge); the part of it on the far side is now left out and the polygon follows the
/// edge of the globe. The areas are cartopy 0.26's, read with python from the countries this package ships:
/// <code>
/// tgt = ccrs.Orthographic(central_longitude=lon0, central_latitude=lat0)
/// sum(tgt.project_geometry(shapely.geometry.shape(feature["geometry"]), ccrs.PlateCarree()).area for feature in countries) / 6378137.0**2
/// </code></summary>
public class GeoGlobeHorizonTests
{
    private const double Radius = 180 / Math.PI;

    /// <summary>Latitude and longitude of the centre, the area cartopy shows of all the countries, in units of the globe's radius
    /// squared (the whole disc is pi).</summary>
    public static TheoryData<double, double, double> Globes => new()
    {
        { 45, -30, 1.427287 },
        { 0, 0, 1.175833 },
        { 20, 10, 1.388945 },
        { -30, 120, 0.629505 },
    };

    /// <summary>The area of the polygons of <paramref name="countries"/> as the map draws them on a plot that is the globe's square,
    /// one pixel to a degree of arc, in units of the globe's radius squared. The plot is upside down against the plane, so an
    /// exterior winds clockwise and a hole counter-clockwise.</summary>
    private static double ShownArea(IEnumerable<GeoFeature> countries, Orthographic globe)
    {
        var mapping = GeoPixelMapping.For(globe, rawProjected: false, new DataTransform(globe.Bounds.XMin, globe.Bounds.XMax, globe.Bounds.YMin, globe.Bounds.YMax, new Rect(0, 0, 2 * Radius, 2 * Radius)));
        double total = 0;
        foreach (var country in countries)
        {
            foreach (var polygon in country.Geometry.Polygons)
            {
                double signed = PiecesArea(mapping, polygon.Exterior);
                foreach (var hole in polygon.Holes) signed += PiecesArea(mapping, hole);
                total += -signed / (Radius * Radius);
            }
        }

        return total;
    }

    private static double PiecesArea(GeoPixelMapping mapping, IReadOnlyList<GeoPosition> ring)
    {
        var pieces = mapping.Pieces(ring, closed: true);
        double twice = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            if (piece.Count < 3) continue;
            for (int k = 0; k < piece.Count; k++)
            {
                var p = piece[k];
                var q = piece[(k + 1) % piece.Count];
                twice += p.X * q.Y - q.X * p.Y;
            }
        }

        return twice / 2;
    }

    [Theory]
    [MemberData(nameof(Globes))]
    public void TheLandOfAGlobe_IsTheAreaCartopyShowsOfIt(double latitude, double longitude, double cartopyArea)
    {
        double shown = ShownArea(NaturalEarth110m.Countries(), new Orthographic(latitude, longitude));

        Assert.Equal(cartopyArea, shown, cartopyArea * 0.003);
    }

    [Theory]
    [InlineData("RUS", 0.188117)]
    [InlineData("CAN", 0.180104)]
    [InlineData("USA", 0.142491)]
    [InlineData("GRL", 0.047040)]
    [InlineData("BRA", 0.104708)]
    [InlineData("KAZ", 0.031240)]
    [InlineData("DZA", 0.048769)]
    [InlineData("FRA", 0.014154)]
    [InlineData("MNG", 0.007083)]
    public void ACountryThatCrossesTheHorizon_IsTheAreaCartopyShowsOfIt(string code, double cartopyArea)
    {
        var country = NaturalEarth110m.Countries().Single(c => c.Properties["ADM0_A3"] == code);

        double shown = ShownArea([country], new Orthographic(45, -30));

        Assert.Equal(cartopyArea, shown, cartopyArea * 0.01);
    }

    [Fact]
    public void AntarcticaOnTheFarSide_IsNotDrawn()
    {
        var antarctica = NaturalEarth110m.Countries().Single(c => c.Properties["ADM0_A3"] == "ATA");

        Assert.Equal(0, ShownArea([antarctica], new Orthographic(45, -30)), 9);
    }

    [Fact]
    public void TheLandOfTheGlobeSample_IsTheAreaCartopyShowsOfIt_AndNeverBeyondTheGlobe()
    {
        var globe = GeoProjection.OrthographicAt(45, -30);
        string svg = Plt.Create().WithSize(600, 600).AddSubPlot(1, 1, 1, ax => ax.WithProjection(globe)
            .Ocean(globe, Color.FromHex("#87CEEB")).Land(globe, Color.FromHex("#90EE90"))).Build().ToSvg();
        var (left, right, top, bottom) = MapSvg.Frame(svg);
        double cx = (left + right) / 2, cy = (top + bottom) / 2, radius = (right - left) / 2;
        double area = 0;
        double farthest = 0;
        foreach (var path in svg.Elements("path").Where(element => element["fill"] == "#90EE90" && element["d"] is not null))
        {
            // each subpath is a ring: an exterior and a hole wind opposite ways, so the signed areas of a path add up to its area
            double signed = 0;
            foreach (var points in path.Subpaths())
            {
                for (int i = 0; i < points.Length; i++)
                {
                    var (p, q) = (points[i], points[(i + 1) % points.Length]);
                    signed += p.X * q.Y - q.X * p.Y;
                    farthest = Math.Max(farthest, double.Hypot(p.X - cx, p.Y - cy) / radius);
                }
            }

            area += Math.Abs(signed) / 2 / (radius * radius);
        }

        // cartopy 1.427287; the picture before the cut, which drew a chord across the globe wherever land met the horizon, 1.4102
        Assert.Equal(1.427287, area, 1.427287 * 0.005);
        Assert.True(farthest <= 1.0 + 0.1 / radius, $"land is drawn beyond the globe: {farthest} of its radius");
    }
}
