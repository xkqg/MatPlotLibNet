// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Verifies <see cref="GeoPixelMapping"/>, where a map's rings land in the plot: the projection's bounds fill the
/// plot edge to edge, north up, and a position the projection cannot place is left out.</summary>
public class GeoPixelMappingTests
{
    private static readonly Rect Plot = new(10, 20, 200, 100);

    /// <summary>The mapping of an axes whose limits are the projection's bounds, as a map asks for them: the bounds onto the plot.</summary>
    private static GeoPixelMapping OnItsBounds(IGeoProjection projection, bool rawProjected)
    {
        var bounds = projection.Bounds;
        return GeoPixelMapping.For(projection, rawProjected, new DataTransform(bounds.XMin, bounds.XMax, bounds.YMin, bounds.YMax, Plot));
    }

    [Fact]
    public void TheCornersOfTheBounds_LandOnTheCornersOfThePlot_NorthUp()
    {
        var bounds = new PlateCarree().Bounds;
        var mapping = OnItsBounds(new PlateCarree(), rawProjected: true);

        var pixels = mapping.Ring([new GeoPosition(bounds.XMin, bounds.YMax), new GeoPosition(bounds.XMax, bounds.YMin)]);

        Assert.Equal([new Point(10, 20), new Point(210, 120)], pixels);
    }

    [Fact]
    public void ARingOfLongitudesAndLatitudes_GoesThroughTheProjection()
    {
        var projection = new PlateCarree();
        var mapping = OnItsBounds(projection, rawProjected: false);
        var (x, y) = projection.Forward(10, 20);
        var bounds = projection.Bounds;
        var expected = new Point(
            Plot.X + (x - bounds.XMin) / (bounds.XMax - bounds.XMin) * Plot.Width,
            Plot.Y + (1.0 - (y - bounds.YMin) / (bounds.YMax - bounds.YMin)) * Plot.Height);

        Assert.Equal([expected], mapping.Ring([new GeoPosition(20, 10)]));
    }

    [Fact]
    public void APositionTheProjectionCannotPlace_IsLeftOut()
    {
        var mapping = OnItsBounds(new PlateCarree(), rawProjected: true);

        Assert.Single(mapping.Ring([new GeoPosition(double.NaN, 0), new GeoPosition(0, double.NaN), new GeoPosition(0, 0)]));
    }

    [Fact]
    public void ARingOnAProjectionWithNoHorizon_IsOnePieceOfTheSamePixelsAsRing()
    {
        var mapping = OnItsBounds(new PlateCarree(), rawProjected: false);
        GeoPosition[] ring = [new(-10, -10), new(10, -10), new(10, 10), new(-10, 10), new(-10, -10)];

        var pieces = mapping.Pieces(ring, closed: true);

        Assert.Equal(1, pieces.Count);
        Assert.Equal(mapping.Ring(ring), pieces[0]);
    }

    [Fact]
    public void ARingThatIsAlreadyProjected_IsNeverCutAtAHorizon()
    {
        var globe = GeoProjection.OrthographicAt(0, 0);
        var mapping = OnItsBounds(globe, rawProjected: true);
        GeoPosition[] ring = [new(-50, -50), new(50, -50), new(50, 50), new(-50, 50), new(-50, -50)];

        var pieces = mapping.Pieces(ring, closed: true);

        Assert.Equal(1, pieces.Count);
        Assert.Equal(mapping.Ring(ring), pieces[0]);
    }

    [Fact]
    public void ARingOnAGlobeThatCrossesItsHorizon_KeepsOnlyWhatIsShown_AndNeverTheFarSide()
    {
        var globe = GeoProjection.OrthographicAt(0, 0);
        var mapping = OnItsBounds(globe, rawProjected: false);
        GeoPosition[] ring = [new(60, -10), new(120, -10), new(120, 10), new(60, 10), new(60, -10)];

        var pieces = mapping.Pieces(ring, closed: true);

        Assert.Equal(1, pieces.Count);
        // the far-side corners are gone, and the piece is closed along the edge of the globe: more points than the two near corners
        Assert.True(pieces[0].Count > 4);
        var centre = new Point(Plot.X + Plot.Width / 2, Plot.Y + Plot.Height / 2);
        double radius = Math.Min(Plot.Width, Plot.Height) / 2;
        Assert.All(Enumerable.Range(0, pieces[0].Count).Select(i => pieces[0][i]),
            p => Assert.True(double.Hypot((p.X - centre.X) / (Plot.Width / 2), (p.Y - centre.Y) / (Plot.Height / 2)) <= 1 + 1e-9));
    }

    [Fact]
    public void ALineThatLeavesAGlobe_IsOnePieceThatEndsOnItsEdge_AndOneThatComesBackStartsThere()
    {
        var globe = GeoProjection.OrthographicAt(0, 0);
        var mapping = OnItsBounds(globe, rawProjected: false);
        GeoPosition[] line = [new(60, 0), new(120, 0), new(120, 20), new(60, 20)];

        var pieces = mapping.Pieces(line, closed: false);

        Assert.Equal(2, pieces.Count);
    }

    /// <summary>A place lands where the transform of the axes puts it, to the last bit: the one rule that turns a coordinate into a
    /// pixel is the transform's, for the marks of every series and for a map.</summary>
    [Fact]
    public void APlace_LandsWhereTheTransformOfTheAxesPutsIt_ToTheLastBit()
    {
        var projection = new PlateCarree();
        var transform = new DataTransform(-30, 70, -10, 55, Plot);
        var mapping = GeoPixelMapping.For(projection, rawProjected: false, transform);
        var places = Enumerable.Range(0, 400).Select(i => new GeoPosition(-170 + i * 0.8537, -80 + i * 0.3891)).ToArray();

        var pixels = mapping.Ring(places);

        Assert.Equal(places.Length, pixels.Count);
        for (int i = 0; i < places.Length; i++)
        {
            var (x, y) = projection.Forward(places[i].Lat, places[i].Lon);
            Assert.Equal(transform.DataToPixel(x, y), pixels[i]);
        }
    }
}
