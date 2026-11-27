// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Verifies the owners the projections share: the numbers a projection is made with (<see cref="ProjectionParameterSet"/>),
/// the centre of an azimuthal projection (<see cref="CentredFrame"/>, <see cref="CentredPlace"/>), a point off the domain
/// (<see cref="ProjectedPointExtensions"/>), the outline traced once (<see cref="ProjectionOutlineExtensions"/>) and the widths of a
/// map's lines (<see cref="Geo.GeoAxesExtensions.CoastlineWidth"/> and the two beside it).</summary>
public class GeoProjectionOwnersTests
{
    [Fact]
    public void TheNumbersOfACone_AreNamedInTheOrderItsMapWritesThem()
    {
        var numbers = ProjectionParameterSet.Conic(29.5, 45.5, -96, 37.5);

        Assert.Equal(new[] { "lat_1", "lat_2", "lon_0", "lat_0" }, numbers.Numbers.Keys);
        Assert.Equal((29.5, 45.5, -96.0, 37.5), (numbers.Lat1, numbers.Lat2, numbers.Lon0, numbers.Lat0));
    }

    [Fact]
    public void TheNumbersOfACentreAndOfAMeridian_AreTheirPROJNames()
    {
        Assert.Equal(new[] { "lat_0", "lon_0" }, ProjectionParameterSet.Centred(10, 20).Numbers.Keys);
        Assert.Equal((10.0, 20.0), (ProjectionParameterSet.Centred(10, 20).Lat0, ProjectionParameterSet.Centred(10, 20).Lon0));
        Assert.Equal(new[] { "lon_0" }, ProjectionParameterSet.Meridian(15).Numbers.Keys);
        Assert.Equal(15.0, ProjectionParameterSet.Meridian(15).Lon0);
    }

    /// <summary>A number given takes its place, a number left out keeps its value, and the order stays the projection's; the set it
    /// was made from is left as it was.</summary>
    [Fact]
    public void WithGivenNumbers_ANumberLeftOutKeepsItsValue_AndTheOrderStays()
    {
        var numbers = ProjectionParameterSet.Conic(29.5, 45.5, -96, 37.5);

        var made = numbers.With(new Dictionary<string, double> { ["lat_0"] = 40, ["lat_1"] = 30 });

        Assert.Equal(new[] { "lat_1", "lat_2", "lon_0", "lat_0" }, made.Numbers.Keys);
        Assert.Equal(new double[] { 30, 45.5, -96, 40 }, made.Numbers.Values);
        Assert.Equal(37.5, numbers.Lat0);
    }

    /// <summary>The centre is the sine and the cosine of its latitude and its longitude in radians, and a place seen from it is what an
    /// azimuthal projection's formula reads, to the last bit.</summary>
    [Theory]
    [InlineData(90, 0, 10, 20)]
    [InlineData(40, 10, -33.9, 151.2)]
    [InlineData(-60, 20, 48.85, 2.35)]
    public void APlaceSeenFromTheCentre_IsTheAzimuthalFormula_ToTheLastBit(double lat0, double lon0, double lat, double lon)
    {
        var frame = CentredFrame.At(lat0, lon0);
        double phi0 = lat0.ToRadians(), lam0 = lon0.ToRadians(), phi = lat.ToRadians(), lam = lon.ToRadians();
        double sinPhi0 = Math.Sin(phi0), cosPhi0 = Math.Cos(phi0), sinPhi = Math.Sin(phi), cosPhi = Math.Cos(phi);
        const double k = 1.7;

        var place = frame.Of(lat, lon);

        Assert.Equal(new CentredFrame(sinPhi0, cosPhi0, lam0), frame);
        Assert.Equal(sinPhi0 * sinPhi + cosPhi0 * cosPhi * Math.Cos(lam - lam0), place.CosDistance);
        Assert.Equal(1 + sinPhi0 * sinPhi + cosPhi0 * cosPhi * Math.Cos(lam - lam0), place.CosDistanceAfter(1));
        Assert.Equal(k * cosPhi * Math.Sin(lam - lam0), place.EastBy(k));
        Assert.Equal(k * (cosPhi0 * sinPhi - sinPhi0 * cosPhi * Math.Cos(lam - lam0)), place.NorthBy(k));
    }

    [Fact]
    public void APointOffTheDomain_IsBothCoordinatesMissing_AndEitherMissingIsOff()
    {
        Assert.True(double.IsNaN(ProjectedPoint.OffDomain.X) && double.IsNaN(ProjectedPoint.OffDomain.Y));
        Assert.True(ProjectedPoint.OffDomain.IsOffDomain);
        Assert.True(new ProjectedPoint(double.NaN, 1).IsOffDomain);
        Assert.True(new ProjectedPoint(1, double.NaN).IsOffDomain);
        Assert.False(new ProjectedPoint(1, 2).IsOffDomain);
        Assert.False(new ProjectedPoint(double.PositiveInfinity, 2).IsOffDomain);
    }

    [Fact]
    public void TheStepBetweenTwoProjectedPoints_IsTheDifferenceAlongEachAxis()
    {
        var step = new ProjectedPoint(1, 2).StepTo(new ProjectedPoint(4, 6));

        Assert.Equal(new Point(3, 4), step);
        Assert.Equal(5, step.Length);
    }

    /// <summary>A projection's outline is traced once and kept for that projection: the same list every time it is asked for, and
    /// another projection of the same kind traces its own.</summary>
    [Fact]
    public void TheOutline_IsTracedOnceForEachProjection()
    {
        var first = new CountingOutline();
        var second = new CountingOutline();

        var outline = first.Outline();

        Assert.Same(outline, first.Outline());
        Assert.Equal(1, first.Traces);
        Assert.NotSame(outline, second.Outline());
        Assert.Equal(1, second.Traces);
    }

    /// <summary>A projection with no outline of its own has the rectangle of its bounds, made at each ask.</summary>
    [Fact]
    public void AProjectionWithNoOutline_HasTheRectangleOfItsBounds()
    {
        var bounds = new PlateCarree().Bounds;

        Assert.Equal(bounds.Rectangle(), new PlateCarree().Outline());
    }

    [Fact]
    public void ASymmetricBox_ReachesAsFarToEachSide()
    {
        Assert.Equal(new GeoBounds(-3, 3, -2, 2), GeoBounds.Symmetric(3, 2));
    }

    /// <summary>The world edge of a projection is as far along X as the antimeridian stands on the equator and as far along Y as the
    /// pole on the central meridian, each to both sides.</summary>
    [Fact]
    public void TheWorldEdge_IsTheAntimeridianOnTheEquator_AndThePoleOnTheCentralMeridian()
    {
        var robinson = new Robinson();
        double edge = robinson.Forward(0, 180).X, pole = robinson.Forward(90, 0).Y;

        Assert.Equal(new GeoBounds(-edge, edge, -pole, pole), robinson.WorldEdgeBounds());
    }

    /// <summary>The box round a projected boundary is the least and the greatest of its points, a place off the domain left out.</summary>
    [Fact]
    public void TheBoxRoundABoundary_LeavesOutAPlaceOffTheDomain()
    {
        var globe = new Orthographic();
        GeoCoordinate[] boundary = [new(10, 20), new(-30, -40), new(0, 180)];
        var (x1, y1) = globe.Forward(10, 20);
        var (x2, y2) = globe.Forward(-30, -40);

        Assert.True(globe.Forward(0, 180).IsOffDomain);
        Assert.Equal(new GeoBounds(Math.Min(x1, x2), Math.Max(x1, x2), Math.Min(y1, y2), Math.Max(y1, y2)), globe.BoxRound(boundary));
    }

    /// <summary>The bounds each projection had before they were worked out in one place, to the last bit: the world edge of the
    /// cylindrical and pseudo-cylindrical ones (the Mercator's pole where its latitude is clamped), half and a quarter turn of arc for
    /// the transverse Mercator, the antipode's circle for the azimuthal equidistant, the globe's radius for the orthographic.</summary>
    public static TheoryData<string, double, double> BoundsAsTheyWere => new()
    {
        { "PlateCarree", 180, 90 },
        { "Sinusoidal", 180, 90 },
        { "TransverseMercator", 180, 90 },
        { "AzimuthalEquidistant", 180, 180 },
        { "Orthographic", 180 / Math.PI, 180 / Math.PI },
    };

    [Theory]
    [MemberData(nameof(BoundsAsTheyWere))]
    public void TheBoundsOfAProjection_AreWhatTheyWere_ToTheLastBit(string name, double halfWidth, double halfHeight)
    {
        var projection = GeoProjectionExtensions.Make(name, new Dictionary<string, double>())!;

        Assert.Equal(new GeoBounds(-halfWidth, halfWidth, -halfHeight, halfHeight), projection.Bounds);
    }

    /// <summary>The pseudo-cylindrical ones are their world edge, and the Mercator's pole stands at the latitude it is clamped to.</summary>
    [Fact]
    public void TheWorldEdgeProjections_AreTheirWorldEdge_AndTheMercatorsPoleIsItsClampedLatitude()
    {
        IGeoProjection[] edged = [new Robinson(), new Mollweide(), new EqualEarth(), new NaturalEarthProjection()];
        foreach (var projection in edged)
        {
            var (xMax, _) = projection.Forward(0, 180);
            var (_, yMax) = projection.Forward(90, 0);
            Assert.Equal(new GeoBounds(-xMax, xMax, -yMax, yMax), projection.Bounds);
        }

        var mercator = new Mercator();
        var (_, top) = mercator.Forward(85.051129, 0);
        Assert.Equal(new GeoBounds(-180, 180, -top, top), mercator.Bounds);
    }

    [Fact]
    public void TheVerbsGivenNoWidth_DrawTheMapsLinesAtTheirWidths()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithProjection(GeoProjection.PlateCarree)
            .Coastlines().Borders().Choropleth(new Dictionary<string, double> { ["FRA"] = 1 })).Build();
        var series = figure.SubPlots[0].Series.OfType<GeoPolygonSeries>().ToDictionary(s => s.Label!);

        Assert.Equal(GeoAxesExtensions.CoastlineWidth, series["Coastlines"].StrokeWidth);
        Assert.Equal(GeoAxesExtensions.BorderWidth, series["Borders"].StrokeWidth);
        Assert.Equal(GeoAxesExtensions.RegionEdgeWidth, series["Choropleth"].StrokeWidth);
        Assert.Equal((0.5, 0.3, 0.3), (GeoAxesExtensions.CoastlineWidth, GeoAxesExtensions.BorderWidth, GeoAxesExtensions.RegionEdgeWidth));
    }

    /// <summary>A projection that counts how often its outline is traced.</summary>
    private sealed class CountingOutline() : TestGeoProjection(new GeoBounds(-1, 1, -1, 1)), IProjectionOutline
    {
        internal int Traces { get; private set; }

        IReadOnlyList<GeoPosition> IProjectionOutline.TraceOutline()
        {
            Traces++;
            return Bounds.Rectangle();
        }
    }
}
